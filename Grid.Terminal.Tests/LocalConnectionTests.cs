using System.Text.Json;
using Grid.Core;
using Grid.Simulation;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class LocalConnectionTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "grid-connections-" + Guid.NewGuid().ToString("N"));
    private string Source => Path.Combine(root, "tasks.json");
    private string Catalog => Path.Combine(root, "connections.json");
    public LocalConnectionTests() { Directory.CreateDirectory(root); WriteBoard("existing task"); }
    public void Dispose() { try { Directory.Delete(root, true); } catch { } }
    private void WriteBoard(params string[] titles) => File.WriteAllText(Source, JsonSerializer.Serialize(new LocalTaskBoard(1,
        titles.Select(title => new LocalTaskItem(Guid.NewGuid(), title, false, DateTimeOffset.UtcNow)).ToArray())));
    private LocalConnections Connected()
    {
        var catalog = new LocalConnections(Catalog, "device-world-owner"); var inspected = LocalConnections.InspectBoard(Source);
        catalog.Add("tasks", Source, inspected.Hash); catalog.Control("tasks", "enable", inspected.Hash); return catalog;
    }

    [Fact]
    public void AddEnableFreshReadAndReceiptsAreBoundedAndCited()
    {
        var catalog = Connected();
        var result = catalog.ReadTasks("tasks", "MASTER");
        Assert.Contains("local/offline", result); Assert.Contains("Source SHA256", result); Assert.Contains("citation tasks:", result);
        Assert.DoesNotContain("existing task", File.ReadAllText(Catalog));
        for (var i = 0; i < 110; i++) _ = catalog.ReadTasks("tasks", "MASTER");
        Assert.Equal(100, catalog.Read().Receipts.Length);
    }

    [Fact]
    public void DraftApprovalCommitIsStaleSafeAndRetainsBeforeImage()
    {
        var catalog = Connected(); var draft = catalog.DraftAdd("tasks", "new reviewed task");
        Assert.DoesNotContain("new reviewed task", File.ReadAllText(Source));
        Assert.Throws<UnauthorizedAccessException>(() => catalog.Commit(draft));
        catalog.Commit(draft with { Approved = true });
        Assert.Contains("new reviewed task", File.ReadAllText(Source));
        Assert.True(Directory.Exists(Catalog + ".artifacts"));
        var stale = catalog.DraftAdd("tasks", "stale task"); WriteBoard("changed elsewhere");
        Assert.Throws<IOException>(() => catalog.Commit(stale with { Approved = true }));
        Assert.DoesNotContain("stale task", File.ReadAllText(Source));
    }

    [Fact]
    public void AgentReadGrantIsExactRevocableAndClearedOnMove()
    {
        var catalog = Connected(); catalog.Control("tasks", "grant", role: "JN");
        Assert.Contains("existing task", catalog.ReadTasks("tasks", "JN"));
        Assert.Throws<UnauthorizedAccessException>(() => catalog.ReadTasks("tasks", "MAXIS"));
        catalog.Control("tasks", "revoke", role: "JN");
        Assert.Throws<UnauthorizedAccessException>(() => catalog.ReadTasks("tasks", "JN"));
        catalog.Control("tasks", "grant", role: "JN");
        var moved = new LocalConnections(Catalog, "other-device").Read().Connections.Single();
        Assert.False(moved.Enabled); Assert.Empty(moved.ReadGrants);
    }

    [Fact]
    public void DisableRemoveAndSourceFailureDoNotDeleteExternalData()
    {
        var catalog = Connected(); catalog.Control("tasks", "disable");
        Assert.Throws<UnauthorizedAccessException>(() => catalog.ReadTasks("tasks", "MASTER"));
        catalog.Control("tasks", "remove"); Assert.Empty(catalog.Read().Connections); Assert.True(File.Exists(Source));
        catalog = Connected(); File.Delete(Source);
        Assert.Single(catalog.Read().Connections);
        Assert.Throws<IOException>(() => catalog.ReadTasks("tasks", "MASTER"));
        catalog.Control("tasks", "remove"); Assert.Empty(catalog.Read().Connections);
    }

    [Fact]
    public void InvalidUnknownAndOversizedBoardsAreRejected()
    {
        File.WriteAllText(Source, "{\"Version\":1,\"Items\":[],\"Secret\":true}");
        Assert.Throws<JsonException>(() => LocalConnections.InspectBoard(Source));
        File.WriteAllText(Source, new string('x', 1024 * 1024 + 1));
        Assert.Throws<InvalidDataException>(() => LocalConnections.InspectBoard(Source));
        File.WriteAllText(Source, JsonSerializer.Serialize(new LocalTaskBoard(99, [])));
        Assert.Throws<InvalidDataException>(() => LocalConnections.InspectBoard(Source));
    }

    [Fact]
    public void TerminalSeparatesConsentReadDraftApprovalCommitAndAgentPrivacy()
    {
        var owner = new TerminalIdentitySession(() => "owner"); var registry = new TerminalSessionRegistry(); registry.Add(owner);
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: registry); owner.Attach(engine); owner.Login(); engine.StartAgents(owner.Reference);
        var commands = DefaultCommands.Create(engine); ConnectionCommands.Register(commands, engine, owner, Catalog); commands.UseIdentity(owner);
        Assert.Contains("email-service | unavailable", commands.Execute("connections providers").Text);
        var plan = commands.ExecuteTokens(["connections", "plan", "local-task-board", "tasks", Source]).Text;
        var add = plan.Split("connections add ").Last(); Assert.Contains("added disabled", commands.Execute("connections add " + add).Text);
        var hash = LocalConnections.InspectBoard(Source).Hash; Assert.Contains("enabled locally", commands.Execute($"connections enable tasks {hash}").Text);
        Assert.Contains("grant", commands.Execute("connections grant tasks JN").Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Active agent", owner.Switch("JN")); Assert.Contains("citation tasks:", commands.Execute("connections read tasks").Text);
        Assert.Contains("Active identity", owner.Switch("master")); Assert.Contains("Active agent", owner.Switch("MAXIS"));
        Assert.Contains("refused", commands.Execute("connections read tasks").Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Active identity", owner.Switch("master"));
        var draft = commands.ExecuteTokens(["connections", "draft-add", "tasks", "terminal task"]).Text;
        var id = draft.Split("connections approve ").Last().Split('\n')[0];
        Assert.DoesNotContain("terminal task", File.ReadAllText(Source));
        Assert.Contains("approved", commands.Execute("connections approve " + id).Text);
        Assert.Contains("committed", commands.Execute("connections commit " + id).Text);
        Assert.Contains("terminal task", File.ReadAllText(Source));
    }
}
