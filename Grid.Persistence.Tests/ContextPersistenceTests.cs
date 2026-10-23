using Grid.Communication;
using Grid.Core;
using Grid.Simulation;
using Grid.Simulation.Editing;
using Microsoft.Data.Sqlite;
using Xunit;
namespace Grid.Persistence.Tests;

public sealed class ContextPersistenceTests : IDisposable
{
    private sealed class TestAuthority(Guid world) : IProgramEditAuthorizer
    {
        public EditSessionReference Session { get; } = new(Guid.NewGuid(), 0);
        public IProgramEditLease? Acquire(EditSessionReference session, Guid target) => target == world && session == Session ? new Lease(new(Guid.NewGuid(), session.SessionId, session.Generation, world, true, null, 1)) : null;
        private sealed class Lease(ProgramEditIdentity identity) : IProgramEditLease
        { public ProgramEditIdentity Identity => identity; public void Dispose() { } }
    }
    private readonly string directory = Path.Combine(Path.GetTempPath(), "GridContextStore-" + Guid.NewGuid().ToString("N"));
    private readonly GridSimulationEngine engine;
    private readonly TestAuthority authority;
    private readonly WorldInfo info;
    private string PathName => Path.Combine(directory, "world.db");
    public ContextPersistenceTests()
    {
        Directory.CreateDirectory(directory); var world = GridWorldFactory.CreateGenesis();
        authority = new(world.InstanceId); engine = new(world, editAuthorizer: authority); engine.BindLocalOwner("test-owner");
        info = new(world.Id, "Context", DateTimeOffset.UtcNow);
    }
    public void Dispose() => Directory.Delete(directory, true);
    private ContextDocument Document(string body, Guid? agent = null) => new(Guid.NewGuid(), "Project", Path.Combine(directory, "not-reopened.md"), new string('A', 64), 100, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "utf8", body, agent);
    private void Sql(string sql)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = PathName, Pooling = false }.ToString());
        connection.Open(); using var command = connection.CreateCommand(); command.CommandText = sql; command.ExecuteNonQuery();
    }
    [Fact]
    public void VoiceProfilesAndMicrophoneMessagesSurviveSaveRestoreAndRecovery()
    {
        var agent = engine.CreateRunningCoordinator(); engine.RegisterAgentIdentity(agent.Id);
        var profile = new VoiceProfile(agent.Id, "Offline Test Voice", -1, 75); engine.SetVoiceProfile(authority.Session, profile);
        var conversation = engine.StartConversation(engine.LocalOperator, agent);
        engine.Talk(conversation, engine.LocalOperator, "help", origin: MessageOrigin.Microphone);
        using (var store = new WorldStore(PathName)) store.Create(info, engine.GetSnapshot());
        using var reopened = new WorldStore(PathName); var restored = GridSimulationEngine.Restore(reopened.Load().State);
        Assert.Equal(profile, Assert.Single(restored.GetSnapshot().Communications.VoiceProfiles));
        Assert.Equal(MessageOrigin.Microphone, restored.GetConversation(conversation).Messages[0].Origin);
        restored.EndConversation(conversation, restored.LocalOperator);
        Assert.Equal(profile, Assert.Single(restored.GetSnapshot().Communications.VoiceProfiles));
        File.WriteAllText(PathName, "damaged"); reopened.Recover();
        Assert.Equal(profile, Assert.Single(reopened.Load().State.Communications.VoiceProfiles));
    }
    [Fact]
    public void OldCommunicationJsonDefaultsToNoVoicesAndCorruptProfilesAreRejected()
    {
        var legacy = System.Text.Json.JsonSerializer.Deserialize<CommunicationState>("{\"Conversations\":[]}")!;
        Assert.Empty(legacy.VoiceProfiles); CommunicationState.Validate(legacy, 0);
        var bad = legacy with { VoiceProfiles = [new(Guid.NewGuid(), "test", 99)] };
        Assert.Throws<InvalidDataException>(() => CommunicationState.Validate(bad, 0));
    }
    [Fact]
    public void PersistentContextAndGrantsSurviveRestartRecoveryWhileTemporaryDoesNot()
    {
        var agent = engine.CreateRunningCoordinator(); engine.RegisterAgentIdentity(agent.Id);
        var persistent = Document("persisted-body", agent.Id); var temporary = Document("temporary-body", agent.Id);
        engine.AddContext(authority.Session, persistent, true); engine.AddContext(authority.Session, temporary, false);
        using (var store = new WorldStore(PathName)) store.Create(info, engine.GetSnapshot());
        using var reopened = new WorldStore(PathName);
        var restored = GridSimulationEngine.Restore(reopened.Load().State);
        Assert.Equal(persistent, Assert.Single(restored.ContextFor(agent.Id, Guid.NewGuid())));
        Assert.DoesNotContain("temporary-body", File.ReadAllText(PathName));
        Assert.DoesNotContain("temporary-body", File.ReadAllText(reopened.BackupPath));
        File.WriteAllText(PathName, "damaged"); reopened.Recover();
        Assert.Equal(persistent, Assert.Single(reopened.Load().State.Context.Documents));
    }
    [Fact]
    public void RevocationAndRemovalPersistWithoutReopeningSource()
    {
        var agent = engine.CreateRunningCoordinator(); engine.RegisterAgentIdentity(agent.Id); var document = Document("stored", agent.Id);
        engine.AddContext(authority.Session, document, true);
        using var store = new WorldStore(PathName); store.Create(info, engine.GetSnapshot());
        engine.ChangeContext(authority.Session, document.Id, false); store.Save(info, engine.GetSnapshot());
        Assert.Null(Assert.Single(store.Load().State.Context.Documents).GrantedAgentId);
        engine.ChangeContext(authority.Session, document.Id, true); store.Save(info, engine.GetSnapshot());
        Assert.Empty(store.Load().State.Context.Documents);
    }
    [Fact]
    public void VersionTwelveMigratesToEmptyContextWithoutChangingWorld()
    {
        using var store = new WorldStore(PathName); store.Create(info, engine.GetSnapshot());
        Sql("DROP TABLE WorldTasks; DROP TABLE WorldContext; DELETE FROM SchemaMigrations WHERE Version>=13; PRAGMA user_version=12;");
        var loaded = store.Load(); Assert.Equal(info.Id, loaded.Info.Id); Assert.Empty(loaded.State.Context.Documents);
        Assert.Equal("test-owner", loaded.State.Identities.OwnerSubject);
        engine.AddContext(authority.Session, Document("after upgrade"), true); store.Save(info, engine.GetSnapshot());
        Assert.Single(store.Load().State.Context.Documents);
    }
    [Fact]
    public void FailedContextWriteRollsBackWorldAndInvalidRecordsAreRefused()
    {
        using var store = new WorldStore(PathName); store.Create(info, engine.GetSnapshot());
        engine.AddContext(authority.Session, Document("new content"), true);
        Sql("CREATE TRIGGER reject_context BEFORE INSERT ON WorldContext BEGIN SELECT RAISE(ABORT, 'injected'); END;");
        Assert.Throws<SqliteException>(() => store.Save(info, engine.GetSnapshot()));
        Assert.Empty(store.Load().State.Context.Documents); Assert.Equal(info.Id, store.Load().Info.Id);
        Sql("DROP TRIGGER reject_context;");
        var bad = engine.GetSnapshot() with { Context = new(new[] { Document("bad", Guid.NewGuid()) }) };
        Assert.Throws<InvalidDataException>(() => store.Save(info, bad)); Assert.Empty(store.Load().State.Context.Documents);
    }
}
