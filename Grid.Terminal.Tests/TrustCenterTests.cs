using Grid.AI;
using Grid.Communication;
using Grid.Core;
using Grid.Simulation;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class TrustCenterTests : IAsyncLifetime, IDisposable
{
    private sealed class Backend : IInferenceBackend { public Task<string> GenerateAsync(string prompt, CancellationToken token) => Task.FromResult("test"); }
    private readonly string root = Path.Combine(Path.GetTempPath(), "grid-trust-" + Guid.NewGuid().ToString("N"));
    private string State => Path.Combine(root, "trust.json");
    private readonly GridSimulationEngine engine; private readonly TerminalIdentitySession identity; private readonly SharedLocalInference inference;
    public TrustCenterTests()
    {
        Directory.CreateDirectory(root); identity = new(() => "owner"); var registry = new TerminalSessionRegistry(); registry.Add(identity); engine = new(GridWorldFactory.CreateGenesis(), editAuthorizer: registry); identity.Attach(engine); identity.Login(); engine.StartAgents(identity.Reference); inference = new(new Backend());
    }
    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await inference.DisposeAsync();
    public void Dispose() { try { Directory.Delete(root, true); } catch { } }
    private CommandDispatcher Commands(TrustCenter center)
    { var commands = DefaultCommands.Create(engine); TrustCommands.Register(commands, engine, identity, center, inference, null); commands.UseIdentity(identity); return commands; }

    [Fact]
    public void ProfilesAreDenyFirstAndGrantRevokesImmediately()
    {
        var center = new TrustCenter(State); Assert.False(center.CanSuggest("MCP")); center.ControlProfile("goal-advisor", true); center.Grant("goal-advisor", "MCP"); center.Suggestions(true);
        Assert.True(center.CanSuggest("MCP")); center.Revoke("goal-advisor", "MCP"); Assert.False(center.CanSuggest("MCP"));
        Assert.Contains(center.Snapshot().Receipts, value => value.Action == "Revoke");
    }

    [Fact]
    public void GoalSuggestionsRequireEveryExplicitControl()
    {
        var center = new TrustCenter(State); var commands = Commands(center);
        var plan = commands.ExecuteTokens(["trust", "goal", "plan", "ship-grid", "30", "Complete the terminal acceptance work"]).Text; var planId = plan.Split("trust goal add ").Last();
        Assert.Contains("added disabled", commands.Execute("trust goal add " + planId).Text); Assert.Contains("denied", commands.Execute("trust suggest MCP").Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("completed", commands.Execute("trust profile enable goal-advisor").Text); var grant = commands.Execute("trust grant plan goal-advisor MCP").Text; var grantId = grant.Split("trust grant approve ").Last(); commands.Execute("trust grant approve " + grantId);
        commands.Execute("trust suggestions on"); commands.Execute("trust goal enable ship-grid"); var suggestion = commands.Execute("trust suggest MCP").Text;
        Assert.Contains("ship-grid", suggestion); Assert.Contains("not facts", suggestion); Assert.Empty(engine.GetSnapshot().Tasks.Records);
    }

    [Fact]
    public void ReviewedMemoryExportIsNewFileOnlyAndHashBound()
    {
        var center = new TrustCenter(State); var commands = Commands(center); var target = Path.Combine(root, "memory-export.json");
        var preview = commands.ExecuteTokens(["trust", "export", "plan", target]).Text; Assert.False(File.Exists(target)); var id = preview.Split("trust export approve ").Last();
        var applied = commands.Execute("trust export approve " + id).Text; Assert.True(File.Exists(target)); Assert.Contains("SHA256", applied); Assert.Contains("Grid.AI.TrustMemoryExport.v1", File.ReadAllText(target));
        Assert.Contains("target must be new", commands.ExecuteTokens(["trust", "export", "plan", target]).Text);
    }

    [Fact]
    public void ActivityAndAcceptanceDistinguishEvidenceFromManualGates()
    {
        var commands = Commands(new TrustCenter(State)); var activity = commands.Execute("trust activity").Text; Assert.Contains("VERIFIED ACTIVITY", activity); Assert.Contains("Models:", activity); Assert.Contains("Task", activity);
        var acceptance = commands.Execute("trust acceptance").Text; Assert.Contains("MANUAL | representative-user", acceptance); Assert.Contains("MANUAL | physical Windows", acceptance); Assert.Contains("does not claim", acceptance);
    }

    [Fact]
    public void TrustStatePersistsAndRejectsCapabilityTampering()
    {
        var center = new TrustCenter(State); center.ControlProfile("goal-advisor", true); Assert.True(new TrustCenter(State).Snapshot().Profiles.Single(value => value.Id == "goal-advisor").Enabled);
        File.WriteAllText(State, File.ReadAllText(State).Replace("suggestion.propose", "host.auto-approve")); Assert.Throws<InvalidDataException>(() => new TrustCenter(State).Snapshot());
    }
}
