using Grid.Core;
using Grid.Simulation;
using Grid.Simulation.Editing;
using Microsoft.Data.Sqlite;
using Xunit;
namespace Grid.Persistence.Tests;
public sealed class IdentityPersistenceTests
{
    private sealed class TestAuthority : IProgramEditAuthorizer
    {
        private readonly Guid owner = Guid.NewGuid();
        public EditSessionReference Reference { get; } = new(Guid.NewGuid(), 1);
        public IProgramEditLease? Acquire(EditSessionReference session, Guid world) =>
            new TestLease(new(owner, session.SessionId, session.Generation, world, true, null, 1));
        private sealed class TestLease(ProgramEditIdentity identity) : IProgramEditLease
        { public ProgramEditIdentity Identity => identity; public void Dispose() { } }
    }
    [Fact]
    public void OwnerAgentRegistryAndAuthorizedEditSurviveSaveAndRecoveryWithoutRestoringSession()
    {
        var directory = Path.Combine(Path.GetTempPath(), "GridIdentity-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var session = new TestAuthority();
            var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: session); engine.BindLocalOwner("verified-test-owner");
            var agent = engine.EnsureCoordinator(); engine.RegisterAgentIdentity(agent.Id);
            var preview = engine.PrepareEdit(session.Reference, new(agent.Id, [new("occupation", new TextValue("Verified coordinator"))])).Preview!;
            Assert.Equal(ProgramEditOutcome.Applied, engine.ApplyEdit(session.Reference, preview).Outcome);
            using var store = new WorldStore(Path.Combine(directory, "world.db"));
            store.Create(new(engine.GetSnapshot().WorldId, "Identity", DateTimeOffset.UtcNow), engine.GetSnapshot());
            var restored = GridSimulationEngine.Restore(store.Load().State);
            Assert.Equal(ProgramEditOutcome.Forbidden, restored.PrepareEdit(session.Reference, new(agent.Id, [new("name", new TextValue("Denied"))])).Outcome); Assert.Equal("verified-test-owner", restored.GetSnapshot().Identities.OwnerSubject);
            Assert.Contains(agent.Id, restored.GetSnapshot().Identities.RegisteredAgents);
            Assert.Equal("Verified coordinator", restored.GetSnapshot().Programs.Single(p => p.Id == agent.Id).Occupation);
            File.WriteAllText(store.DatabasePath, "damaged"); store.Recover();
            Assert.Equal("verified-test-owner", store.Load().State.Identities.OwnerSubject);
        }
        finally { Directory.Delete(directory, true); }
    }
    [Fact]
    public void VersionTenUpgradeStartsUnboundAndPreservesExistingWorld()
    {
        var directory = Path.Combine(Path.GetTempPath(), "GridIdentityUpgrade-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "world.db"); var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
            using var store = new WorldStore(path); store.Create(new(engine.GetSnapshot().WorldId, "Old", DateTimeOffset.UtcNow), engine.GetSnapshot());
            using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString()))
            {
                connection.Open(); using var command = connection.CreateCommand();
                command.CommandText = "DROP TABLE WorldTasks; DROP TABLE WorldContext; DROP TABLE WorldIdentities; DELETE FROM SchemaMigrations WHERE Version>=11; PRAGMA user_version=10;"; command.ExecuteNonQuery();
            }
            var loaded = store.Load(); Assert.Equal(engine.GetSnapshot().WorldId, loaded.State.WorldId);
            Assert.Null(loaded.State.Identities.OwnerSubject); Assert.Empty(loaded.State.Identities.RegisteredAgents);
        }
        finally { Directory.Delete(directory, true); }
    }
    [Fact]
    public void PortableCredentialsSurviveRestartAndBackupRecovery()
    {
        var directory = Path.Combine(Path.GetTempPath(), "GridCredential-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "world.db");
            var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
            engine.BindLocalOwner("original-owner");
            var agent = engine.EnsureCoordinator(); engine.RegisterAgentIdentity(agent.Id);
            var credential = Grid.Communication.PasswordCredential.Create("Owner", "test-password-only-123");
            engine.SetLocalCredential(credential);
            using (var store = new WorldStore(path))
                store.Create(new(engine.GetSnapshot().WorldId, "Credential", DateTimeOffset.UtcNow), engine.GetSnapshot());
            using (var store = new WorldStore(path))
            {
                var state = store.Load().State;
                Assert.Equal(credential, state.Identities.Credential);
                Assert.True(state.Identities.Credential!.Verify("Owner", "test-password-only-123"));
                Assert.Equal("original-owner", state.Identities.OwnerSubject);
                Assert.Contains(agent.Id, state.Identities.RegisteredAgents);
                File.WriteAllText(path, "damaged"); store.Recover();
                Assert.Equal(credential, store.Load().State.Identities.Credential);
            }
        }
        finally { Directory.Delete(directory, true); }
    }
}
