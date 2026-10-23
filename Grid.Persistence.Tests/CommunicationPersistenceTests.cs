using Grid.Core;
using Grid.Communication;
using Grid.Simulation;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using Xunit;
namespace Grid.Persistence.Tests;

public sealed class CommunicationPersistenceTests
{
    [Fact]
    public void GroundedReplySourcesSurviveSqliteReloadAndBuildNextContext()
    {
        var folder = Path.Combine(Path.GetTempPath(), "GridGrounded-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
            var id = engine.StartConversation(engine.LocalOperator, engine.CreateRunningCoordinator());
            engine.CompleteGroundedTurn(engine.PrepareGroundedTurn(id,engine.LocalOperator,"Remember the launch plan"),new(true,"Suggestion: review the plan.","test"));
            var snapshot = engine.GetSnapshot(); var info = new WorldInfo(snapshot.WorldId,"Grounded",DateTimeOffset.UtcNow);
            var path = Path.Combine(folder,"world.db");
            using(var store = new WorldStore(path)) store.Create(info,snapshot);
            using var reopened = new WorldStore(path); var restored = GridSimulationEngine.Restore(reopened.Load().State); restored.Start();
            Assert.Contains("SOURCES",restored.GetConversation(id).Messages[1].Content);
            Assert.Contains("Remember the launch plan",restored.PrepareGroundedTurn(id,restored.LocalOperator,"launch plan").Prompt);
            Assert.Empty(restored.GetSnapshot().Tasks.Records);
        }
        finally { Directory.Delete(folder,true); }
    }
    [Fact]
    public void TranscriptsSurviveWorldSaveReopenRecoveryAndMissingApplication()
    {
        var folder = Path.Combine(Path.GetTempPath(), "GridCommunication-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "world.db");
            var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis()); var now = DateTimeOffset.UtcNow;
            engine.ReconcileApplications(new(now, true, [new("app", "App", [new(99, now.AddSeconds(-1))], 4096)], "Fixture"));
            var human = new Recipient(ParticipantKind.Human, Guid.NewGuid(), "Operator");
            var id = engine.StartConversation(human, engine.ResolveProgram("App"));
            engine.SendMessage(id, human, "Remember this conversation");
            var snapshot = engine.GetSnapshot(); var info = new WorldInfo(snapshot.WorldId, "Communication", now);
            using (var initial = new WorldStore(path)) initial.Create(info, snapshot);
            using var store = new WorldStore(path);
            var restored = GridSimulationEngine.Restore(store.Load().State); restored.Start();
            Assert.Equal(JsonSerializer.Serialize(snapshot.Communications), JsonSerializer.Serialize(restored.GetSnapshot().Communications));
            Assert.DoesNotContain(restored.GetSnapshot().Programs, p => p.Kind == ResidentKind.Application);
            Assert.Throws<ArgumentException>(() => restored.SendMessage(id, human, "Cannot route to absent program"));
            restored.EndConversation(id, human); store.Save(info, restored.GetSnapshot());
            Assert.Equal(ConversationStatus.Ended, store.Load().State.Communications.Conversations[0].Status);
            File.WriteAllText(path, "damaged"); store.Recover();
            Assert.Equal("Remember this conversation", store.Load().State.Communications.Conversations[0].Messages[0].Content);
            Assert.Equal(ConversationStatus.Ended, store.Load().State.Communications.Conversations[0].Status);
        }
        finally { Directory.Delete(folder, true); }
    }

    [Fact]
    public void VersionNineWorldMigratesAndFailedSaveRetainsPriorTranscript()
    {
        var folder = Path.Combine(Path.GetTempPath(), "GridCommunicationUpgrade-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "world.db"); var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
            var info = new WorldInfo(engine.GetSnapshot().WorldId, "Old", DateTimeOffset.UtcNow);
            using var store = new WorldStore(path); store.Create(info, engine.GetSnapshot());
            void Sql(string text) { using var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString()); c.Open(); using var q = c.CreateCommand(); q.CommandText = text; q.ExecuteNonQuery(); }
            Sql("DROP TABLE WorldTasks; DROP TABLE WorldContext; DROP TABLE WorldIdentities; DROP TABLE WorldCommunications; DELETE FROM SchemaMigrations WHERE Version>=10; PRAGMA user_version=9;");
            Assert.Empty(store.Load().State.Communications.Conversations);
            var human = new Recipient(ParticipantKind.Human, Guid.NewGuid(), "Operator");
            var id = engine.StartConversation(human, engine.ResolveProgram(engine.GetSnapshot().Programs[0].Id.ToString()));
            engine.SendMessage(id, human, "Committed"); store.Save(info, engine.GetSnapshot());
            Sql("CREATE TRIGGER reject_conversations BEFORE INSERT ON WorldCommunications BEGIN SELECT RAISE(ABORT, 'injected'); END;");
            engine.SendMessage(id, human, "Uncommitted");
            Assert.Throws<SqliteException>(() => store.Save(info, engine.GetSnapshot()));
            Assert.Single(store.Load().State.Communications.Conversations[0].Messages);
            store.Recover(); Assert.Equal("Committed", store.Load().State.Communications.Conversations[0].Messages[0].Content);
        }
        finally { Directory.Delete(folder, true); }
    }
}
