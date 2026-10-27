using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class UsabilityTests
{
    [Theory]
    [InlineData(20)]
    [InlineData(60)]
    [InlineData(120)]
    public void TablesFitNarrowAndWideSurfacesAndRetainLongValues(int width)
    {
        var text = TerminalTable.Render(["Name", "State", "Detail"], [ ["A", "unknown", new string('x', 250)], ["B", "Working", "end"] ], width);
        Assert.All(text.Split(Environment.NewLine), line => Assert.True(line.Length <= width));
        Assert.Equal(250, text.Count(c => c == 'x')); Assert.Contains("unknown", text); Assert.Contains("end", text);
        Assert.Equal("No entries.", TerminalTable.Render(["Name"], [], width));
    }
    [Fact]
    public void TablesSanitizeControlsAndPlainOutputContainsNoEscapeSequences()
    {
        var text = TerminalTable.Render(["Name"], [["Name\u001b[2J\nwide界"]]);
        Assert.DoesNotContain('\u001b', text); Assert.All(text.Replace(Environment.NewLine, ""), c => Assert.InRange((int)c, 32, 126));
    }
    [Fact]
    public void ColorPolicyHonorsFlagsEnvironmentAndRedirectionWithTextLabelsIntact()
    {
        Assert.True(TerminalTheme.SupportsColor(false, false, "xterm", null));
        Assert.False(TerminalTheme.SupportsColor(true, false, "xterm", null));
        Assert.False(TerminalTheme.SupportsColor(false, true, "xterm", null));
        Assert.False(TerminalTheme.SupportsColor(false, false, "dumb", null));
        Assert.False(TerminalTheme.SupportsColor(false, false, "xterm", ""));
        Assert.True(TerminalOptions.Parse(["--no-color"]).NoColor);
        Assert.Equal(ConsoleColor.Yellow, TerminalTheme.Color("grid> "));
        Assert.Equal(ConsoleColor.Yellow, TerminalTheme.Color("unknown (Unavailable)"));
        Assert.Equal(ConsoleColor.Yellow, TerminalTheme.Color("Save failed"));
        Assert.Equal(ConsoleColor.Yellow, TerminalTheme.Color("Observed running"));
    }
    [Fact]
    public async Task PlainWriterPreservesTextWithoutEmittingColorCodes()
    {
        using var text = new StringWriter(); var output = new TerminalOutput(text, false);
        await output.WriteLineAsync("Working\nunknown"); await output.WriteAsync("grid> ");
        Assert.Equal("Working" + Environment.NewLine + "unknown" + Environment.NewLine + "grid> ", text.ToString());
    }
    [Fact]
    public async Task RepeatedLiveBackNavigationKeepsWorldAndPersistenceSessionOpenUntilExit()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), TimeSpan.FromHours(1));
        var worldId = engine.GetSnapshot().WorldId; var persistence = new Persistence();
        var terminal = new Surface(); var output = new StringWriter();
        terminal.Check = () => { Assert.True(engine.IsRunning); Assert.False(persistence.Stopped); Assert.Equal(0, persistence.Saves); };
        var host = new TerminalHost(engine, DefaultCommands.Create(engine), new StringReader("grid -start\nlive\nstatus\nlive\nstatus\nexit\n"), output, false, terminal, persistence);
        Assert.Equal(0, await host.RunAsync().WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(2, terminal.Ends); Assert.Equal(worldId, engine.GetSnapshot().WorldId);
        Assert.Equal(2, output.ToString().Split("Back at command prompt").Length - 1);
        Assert.Equal(2, output.ToString().Split("State: Running").Length - 1);
        Assert.Equal(1, persistence.Saves); Assert.True(persistence.Stopped);
    }
    private sealed class Surface : ILiveTerminal
    {
        public Action? Check; public int Ends; private bool drawn;
        public bool TryGetSize(out int width, out int height) { width = 60; height = 18; return true; }
        public bool TryReadKey(out ConsoleKey key) { key = Ends == 0 ? ConsoleKey.Escape : ConsoleKey.Q; return drawn; }
        public void Begin() => drawn = false;
        public void Draw(IReadOnlyList<string> lines) { Check?.Invoke(); Assert.Contains("Back", lines[0]); Assert.Contains("Back", lines[^1]); drawn = true; }
        public void End() => Ends++;
    }
    private sealed class Persistence : IWorldPersistenceSession
    {
        public int Saves; public bool Stopped;
        public Task SaveAsync(CancellationToken token = default) { Saves++; return Task.CompletedTask; }
        public async Task RunPeriodicAsync(CancellationToken token)
        { try { await Task.Delay(Timeout.Infinite, token); } catch (OperationCanceledException) { } finally { Stopped = true; } }
    }
}
