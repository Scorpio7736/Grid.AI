using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class CommandFollowupTests
{
    [Fact]
    public void FiltersSeparateActivityAndKeepUnknownHistoricalEntriesInPossible()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        var state = engine.GetSnapshot();
        var template = state.Programs[0];
        GridProgramSnapshot App(string name, ApplicationActivity activity, bool confirmed = true) =>
            template with { Id = Guid.NewGuid(), Name = name, Kind = ResidentKind.Application,
                Application = new(name, Guid.NewGuid(), [], DateTimeOffset.UtcNow, confirmed, null) { Activity = activity } };
        state = state with { Programs = [App("Windowed", ApplicationActivity.Running),
            App("Background", ApplicationActivity.OnCall), App("Asleep", ApplicationActivity.Sleeping),
            App("Stale", ApplicationActivity.Running, false), template],
            ApplicationCatalog = [new("historical", "PastApp", "Saved inventory", DateTimeOffset.UtcNow)] };
        var active = ProgramListCommand.Execute(state, ["-active"]).Text;
        Assert.Contains("Windowed", active); Assert.DoesNotContain("Background", active); Assert.DoesNotContain("Stale", active);
        var sleeping = ProgramListCommand.Execute(state, ["-sleeping"]).Text;
        Assert.Contains("Background", sleeping); Assert.Contains("Asleep", sleeping); Assert.DoesNotContain("Windowed", sleeping);
        var possible = ProgramListCommand.Execute(state, ["-possible"]).Text;
        Assert.Contains("PastApp", possible); Assert.Contains("Historical / unknown", possible); Assert.Contains("Stale", possible);
        Assert.DoesNotContain(template.Name, possible);
        Assert.Contains(state.Programs[0].Id.ToString(), ProgramListCommand.Execute(state, ["ids", "-active"]).Text);
        Assert.Contains("Usage:", ProgramListCommand.Execute(state, ["-active", "-sleeping"]).Text);
        Assert.Contains("Usage:", ProgramListCommand.Execute(state, ["ids", "ids"]).Text);
        Assert.Contains("Usage:", ProgramListCommand.Execute(state, ["-bad"]).Text);
        Assert.Contains("No entries.", ProgramListCommand.Execute(state with { Programs = [] }, ["-active"]).Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ClearPreservesWorldAndClockAndReturnsToPrompt(bool available)
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        var before = engine.GetSnapshot(); var clears = 0; var output = new StringWriter();
        var host = new TerminalHost(engine, DefaultCommands.Create(engine), new StringReader("step\nclear\nstatus\nexit\n"), output,
            clearScreen: () => { clears++; return available; });
        Assert.Equal(0, await host.RunAsync());
        Assert.Equal(1, clears); Assert.Equal(1, engine.TickCount); Assert.Equal(before.WorldId, engine.GetSnapshot().WorldId);
        Assert.Contains("Tick: 1", output.ToString()); Assert.Contains("NOT SAVED", output.ToString());
        Assert.Equal(!available, output.ToString().Contains("Clear unavailable"));
        Assert.DoesNotContain('\u001b', output.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FarewellFollowsFinalSaveAndNeverClaimsSuccessOnFailure(bool fails)
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis()); var output = new StringWriter();
        var persistence = new SaveSession(fails);
        var host = new TerminalHost(engine, DefaultCommands.Create(engine), new StringReader("exit\n"), output, false, persistence: persistence);
        Assert.Equal(fails ? 1 : 0, await host.RunAsync());
        Assert.Equal(1, persistence.Saves);
        var text = output.ToString();
        Assert.Contains("GRID7736", text);
        if (fails) { Assert.Contains("SHUTDOWN ERROR", text); Assert.DoesNotContain("WORLD SAVED", text); }
        else Assert.True(text.IndexOf("World saved.", StringComparison.Ordinal) < text.IndexOf("GRID7736", StringComparison.Ordinal));
        Assert.DoesNotContain("Press any key", text);
        Assert.DoesNotContain("______", TerminalPresentation.Farewell(true, false, 30));
    }

    private sealed class SaveSession(bool fails) : IWorldPersistenceSession
    {
        public int Saves;
        public Task SaveAsync(CancellationToken token = default) { Saves++; return fails ? Task.FromException(new IOException("Test failure")) : Task.CompletedTask; }
        public async Task RunPeriodicAsync(CancellationToken token)
        { try { await Task.Delay(Timeout.Infinite, token); } catch (OperationCanceledException) { } }
    }
}
