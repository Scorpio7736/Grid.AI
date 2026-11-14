using System.Reflection;
using System.Text.Json;
using Grid.Agent;
using Grid.Communication;
using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Grid.Simulation.Editing;
using Grid.Simulation.Tasks;
using Microsoft.Data.Sqlite;
using Xunit;
using Status = Grid.Simulation.Tasks.TaskStatus;

namespace Grid.Terminal.Tests;

public sealed class AgentOwnershipMigrationTests
{
    public static IEnumerable<object[]> ForwardedContracts()
    {
        foreach (var type in new[] { typeof(ParticipantKind), typeof(MessageOrigin), typeof(ConversationStatus),
            typeof(Recipient), typeof(GridMessage), typeof(Conversation), typeof(CommunicationState),
            typeof(IGridCommunicationHub), typeof(ContextDocument), typeof(ContextState), typeof(LocalIdentityState),
            typeof(PasswordCredential), typeof(VoiceProfile), typeof(ConversationMemoryValidation) })
            yield return [type, "Grid.Communication"];
        foreach (var type in new[] { typeof(EditSessionReference), typeof(ProgramEditIdentity), typeof(IProgramEditAuthorizer),
            typeof(IProgramEditLease), typeof(AgentRole), typeof(Status), typeof(SystemAgent), typeof(TaskAudit), typeof(TaskRecord) })
            yield return [type, "Grid.Simulation"];
    }

    [Theory]
    [MemberData(nameof(ForwardedContracts))]
    public void LegacyAssemblyNamesResolveToTheSingleAgentDefinition(Type type, string legacyAssembly)
    {
        Assert.Equal("Grid.Agent", type.Assembly.GetName().Name);
        Assert.Same(type, Type.GetType($"{type.FullName}, {legacyAssembly}", throwOnError: true));
        var facade = Assembly.Load(legacyAssembly);
        Assert.Contains(type, facade.GetForwardedTypes());
        Assert.DoesNotContain(facade.DefinedTypes, candidate => candidate.FullName == type.FullName);
    }

    [Fact]
    public void LegacyTaskJsonKeepsIdsEnumValuesAndMissingFieldDefaults()
    {
        const string json = """
        {"Id":"11111111-1111-1111-1111-111111111111","RequestKey":"legacy","AgentId":"22222222-2222-2222-2222-222222222222","Capability":"maxis.demo","Argument":"probe","OperatorId":"33333333-3333-3333-3333-333333333333","SubmittedAsAgent":null,"Status":0,"Attempts":0,"ApprovedBy":null,"Result":null,"Audit":[{"AtUtc":"2026-11-13T00:00:00+00:00","Action":"Proposed","OperatorId":"33333333-3333-3333-3333-333333333333","ActiveAgentId":null,"Detail":"Legacy request"}]}
        """;
        var task = JsonSerializer.Deserialize<TaskRecord>(json)!;
        TaskWorkflow.Validate([new(task.AgentId, AgentRole.MAXIS)], [task], TaskCapabilities.Valid);
        Assert.Equal(Status.AwaitingApproval, task.Status);
        Assert.False(task.Archived);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), task.Id);
        var persisted = JsonSerializer.Serialize(task);
        Assert.DoesNotContain("Grid.Agent", persisted);
        Assert.DoesNotContain("$type", persisted);
        var roundTrip = JsonSerializer.Deserialize<TaskRecord>(persisted)!;
        Assert.Equal(task.Id, roundTrip.Id);
        Assert.Equal(task.Audit, roundTrip.Audit);
        Assert.Throws<InvalidDataException>(() => TaskWorkflow.Validate([new(task.AgentId, AgentRole.MAXIS)],
            [task with { Status = Status.Queued }], TaskCapabilities.Valid));
        Assert.Throws<InvalidDataException>(() => TaskWorkflow.Validate([new(task.AgentId, AgentRole.MAXIS)],
            [task with { Capability = "shell" }], TaskCapabilities.Valid));
    }

    private sealed class Fixture
    {
        public readonly TerminalSessionRegistry Registry = new();
        public readonly TerminalIdentitySession Owner = new(() => "migration-owner");
        public readonly GridSimulationEngine Engine;
        public Fixture(GridSimulationSnapshot? snapshot = null)
        {
            Registry.Add(Owner);
            Engine = snapshot is null ? new(GridWorldFactory.CreateGenesis(), editAuthorizer: Registry)
                : GridSimulationEngine.Restore(snapshot, editAuthorizer: Registry);
            Owner.Attach(Engine);
        }
        public void Start() { Owner.Login(); Engine.StartAgents(Owner.Reference); }
        public TerminalIdentitySession Attach()
        {
            var child = Owner.CreateAttachment(); Registry.Add(child); return child;
        }
    }

    [Fact]
    public async Task LegacyAndTypedRoutesShareOneRegistryHistoryAndDispatchClaim()
    {
        var f = new Fixture(); f.Start();
        var attachment = f.Attach();
        var ids = f.Engine.GetSnapshot().Tasks.Agents.Select(a => a.Id).ToArray();
        f.Engine.StartAgents(attachment.Reference);
        Assert.Equal(ids, f.Engine.GetSnapshot().Tasks.Agents.Select(a => a.Id));
        Assert.Equal(6, ids.Length);

        IGridCommunicationHub hub = f.Engine;
        var conversation = hub.StartConversation(f.Engine.LocalOperator, hub.ResolveProgram("MCP"));
        hub.SendMessage(conversation, f.Engine.LocalOperator, "A single authoritative history.");
        f.Engine.SaveMemory(attachment.Reference, conversation, "release", "Cobalt release");
        Assert.Same(hub.GetConversation(conversation),
            f.Engine.GetSnapshot().Communications.Conversations.Single(c => c.Id == conversation));
        Assert.Equal(2, hub.GetConversation(conversation).Messages.Count);
        Assert.Single(f.Engine.ReviewMemories(f.Owner.Reference, conversation));

        var maxis = hub.ResolveProgram("MAXIS").Id;
        var proposals = await Task.WhenAll(
            Task.Run(() => f.Engine.ProposeTask(f.Owner.Reference, maxis, "maxis.demo", "migration", "same-request")),
            Task.Run(() => f.Engine.ProposeTool(attachment.Reference, new(maxis, "maxis.demo", "migration", "same-request"))));
        Assert.Equal(proposals[0].Id, proposals[1].Id);
        Assert.Single(f.Engine.TasksFor(attachment.Reference).Records);
        f.Engine.ApproveTask(f.Owner.Reference, proposals[0].Id);
        var claimed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var saves = 0;
        async Task Save(CancellationToken token)
        {
            if (Interlocked.Increment(ref saves) == 1) { claimed.SetResult(); await release.Task.WaitAsync(token); }
        }
        var run = f.Engine.RunNextTaskAsync(f.Owner.Reference, Save, default);
        try
        {
            await claimed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                f.Engine.RunNextTaskAsync(attachment.Reference, _ => Task.CompletedTask, default));
        }
        finally { release.TrySetResult(); await run; }
        var completed = Assert.Single(f.Engine.TasksFor(attachment.Reference).Records);
        Assert.Equal(Status.Completed, completed.Status);
        Assert.Equal(1, completed.Attempts);
        Assert.Single(completed.Audit, a => a.Action == "Started");
        Assert.Single(completed.Audit, a => a.Action == "Finished");
        Assert.Equal(2, saves);
        f.Owner.Logout();
        Assert.Throws<UnauthorizedAccessException>(() => f.Engine.TasksFor(attachment.Reference));
    }

    [Theory]
    [InlineData("reload")]
    [InlineData("corrupt-primary")]
    [InlineData("future-version")]
    [InlineData("invalid-backup")]
    [InlineData("schema-upgrade")]
    [InlineData("failed-schema-upgrade")]
    public void SavedWorldRetainsMigratedIdentitiesGrantsMemoryAndHistoryWithoutReplayingApproval(string recovery)
    {
        var directory = Path.Combine(Path.GetTempPath(), "GridOwnership-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var f = new Fixture(); f.Start();
            var mcp = f.Engine.ResolveProgram("MCP"); var maxis = f.Engine.ResolveProgram("MAXIS");
            var conversation = f.Engine.StartConversation(f.Engine.LocalOperator, mcp);
            var meeting = f.Engine.StartMeeting(f.Owner.Reference, ["MCP", "MAXIS"]);
            f.Engine.SendMessage(meeting, f.Engine.LocalOperator, "Shared meeting history.");
            var now = DateTimeOffset.UtcNow;
            var document = new ContextDocument(Guid.NewGuid(), "general", "captured.txt", new string('A', 64), 5,
                now, now, "utf8", "Cobalt", mcp.Id);
            f.Engine.AddContext(f.Owner.Reference, document, true);
            var memory = f.Engine.RetainContextSummary(f.Owner.Reference, conversation, document.Id,
                "summary", "Cobalt release", true);
            f.Engine.GrantMemory(f.Owner.Reference, conversation, "summary", maxis.Id, "general", false);
            var task = f.Engine.ProposeTool(f.Owner.Reference, new(maxis.Id, "maxis.demo", "probe", "saved-key"));
            f.Engine.ApproveTask(f.Owner.Reference, task.Id);
            var snapshot = f.Engine.GetSnapshot();
            GridSimulationEngine.ValidateSnapshot(snapshot);
            var info = new WorldInfo(snapshot.WorldId, "Migration", now);
            using var store = new WorldStore(Path.Combine(directory, "world.db"));
            Assert.Equal(Status.Queued, Assert.Single(snapshot.Tasks.Records).Status);
            Assert.Equal(f.Engine.LocalOperator.Id, snapshot.Tasks.Records[0].ApprovedBy);
            store.Create(info, snapshot);
            var backupBytes = File.ReadAllBytes(store.BackupPath);
            void Sql(string sql)
            {
                using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
                    { DataSource = store.DatabasePath, Mode = SqliteOpenMode.ReadWrite, Pooling = false }.ToString());
                connection.Open();
                using var command = connection.CreateCommand(); command.CommandText = sql; command.ExecuteNonQuery();
            }
            if (recovery == "corrupt-primary")
            {
                File.WriteAllText(store.DatabasePath, "damaged integration world");
                Assert.ThrowsAny<Exception>(() => store.Load());
                store.Recover();
                Assert.Equal("damaged integration world", File.ReadAllText(Directory.GetFiles(directory, "*.damaged-*").Single()));
            }
            else if (recovery == "future-version")
            {
                Sql($"PRAGMA user_version = {GridSchema.CurrentVersion + 1};");
                var futureBytes = File.ReadAllBytes(store.DatabasePath);
                Assert.Throws<InvalidDataException>(() => store.Load());
                Assert.Equal(futureBytes, File.ReadAllBytes(store.DatabasePath));
                store.Recover();
                Assert.Equal(futureBytes, File.ReadAllBytes(Directory.GetFiles(directory, "*.damaged-*").Single()));
            }
            else if (recovery == "invalid-backup")
            {
                var primaryBytes = File.ReadAllBytes(store.DatabasePath);
                File.WriteAllText(store.BackupPath, "invalid backup");
                Assert.ThrowsAny<Exception>(() => store.Recover());
                Assert.Equal(primaryBytes, File.ReadAllBytes(store.DatabasePath));
                Assert.Empty(Directory.GetFiles(directory, "*.damaged-*"));
                File.WriteAllBytes(store.BackupPath, backupBytes);
            }
            else if (recovery is "schema-upgrade" or "failed-schema-upgrade")
            {
                // v24 is a metadata-only migration. Preserve rich payloads while exercising its real upgrade path.
                Sql("DELETE FROM SchemaMigrations WHERE Version = 24; PRAGMA user_version = 23;");
                if (recovery == "failed-schema-upgrade")
                {
                    Sql("CREATE TRIGGER BlockUpgrade BEFORE INSERT ON SchemaMigrations BEGIN SELECT RAISE(ABORT, 'Injected migration interruption'); END;");
                    Assert.Throws<SqliteException>(() => store.Load());
                    using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder
                        { DataSource = store.DatabasePath, Pooling = false }.ToString()))
                    {
                        connection.Open(); using var command = connection.CreateCommand();
                        command.CommandText = "PRAGMA user_version;"; Assert.Equal(23L, command.ExecuteScalar());
                        command.CommandText = "SELECT COUNT(*) FROM SchemaMigrations WHERE Version = 24;";
                        Assert.Equal(0L, command.ExecuteScalar());
                    }
                    Assert.Equal(backupBytes, File.ReadAllBytes(store.BackupPath));
                    store.Recover();
                    Assert.Single(Directory.GetFiles(directory, "*.damaged-*"));
                }
            }
            Assert.Equal(backupBytes, File.ReadAllBytes(store.BackupPath));
            var saved = store.Load().State;
            using (var connection = new SqliteGridDatabase(store.DatabasePath).OpenExisting())
            {
                using var command = connection.CreateCommand(); command.CommandText = "PRAGMA user_version;";
                Assert.Equal((long)GridSchema.CurrentVersion, command.ExecuteScalar());
                command.CommandText = "SELECT COUNT(*) FROM SchemaMigrations;";
                Assert.Equal((long)GridSchema.CurrentVersion, command.ExecuteScalar());
            }
            Assert.Equal(Status.AwaitingApproval, Assert.Single(saved.Tasks.Records).Status);
            Assert.Null(saved.Tasks.Records[0].ApprovedBy);
            var next = new Fixture(saved);
            Assert.False(next.Owner.IsAuthenticated);
            Assert.Throws<UnauthorizedAccessException>(() => next.Engine.TasksFor(f.Owner.Reference));
            next.Start();
            var restored = next.Engine.GetSnapshot();
            Assert.Equal(snapshot.WorldId, restored.WorldId);
            Assert.Equal(snapshot.Tasks.Agents, restored.Tasks.Agents);
            Assert.Equal(snapshot.Identities.RegisteredAgents, restored.Identities.RegisteredAgents);
            Assert.Equal(snapshot.Identities.OwnerSubject, restored.Identities.OwnerSubject);
            Assert.Equal(JsonSerializer.Serialize(snapshot.Communications), JsonSerializer.Serialize(restored.Communications));
            Assert.Equal(JsonSerializer.Serialize(snapshot.Context), JsonSerializer.Serialize(restored.Context));
            Assert.Equal(memory.Id, Assert.Single(restored.Communications.Memories).Id);
            Assert.True(MemoryPolicy.Visible(restored.Communications.Memories[0], maxis.Id, "general", now));
            var recovered = Assert.Single(restored.Tasks.Records);
            Assert.Equal(task.Id, recovered.Id);
            Assert.Equal(Status.AwaitingApproval, recovered.Status);
            Assert.Null(recovered.ApprovedBy);
            Assert.Equal(0, recovered.Attempts);
            Assert.Contains(recovered.Audit, a => a.Action == "Recovered");
            next.Engine.ChangeContext(next.Owner.Reference, document.Id, false);
            Assert.False(MemoryPolicy.Visible(next.Engine.GetSnapshot().Communications.Memories[0], maxis.Id, "general", now));
        }
        finally { Directory.Delete(directory, true); }
    }
}
