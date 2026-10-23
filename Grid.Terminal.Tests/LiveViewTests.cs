using Grid.Core;
using Grid.Simulation;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class LiveViewTests
{
    [Fact]
    public void LiveCommandIsRegisteredAndRejectsArguments()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        var commands = DefaultCommands.Create(engine);
        Assert.True(commands.Execute("LIVE").LiveViewRequested);
        Assert.False(commands.Execute("live extra").LiveViewRequested);
        Assert.Contains("Live status", commands.Execute("help").Text);
        Assert.False(engine.IsRunning);
        Assert.Equal(0, engine.TickCount);
    }

    [Theory]
    [InlineData(60, 18)]
    [InlineData(100, 30)]
    [InlineData(160, 60)]
    public void FramesFitViewportAndPagesExposeEveryCitizen(int width, int height)
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        var snapshot = engine.GetLiveSnapshot();
        var allLines = new List<string>();
        for (var page = 0; page < snapshot.State.Programs.Count; page++)
        {
            var frame = LiveViewRenderer.Render(snapshot, width, height, page);
            Assert.True(frame.Count < height);
            Assert.All(frame, line => Assert.True(line.Length < width));
            allLines.AddRange(frame);
        }
        foreach (var program in snapshot.State.Programs)
            Assert.Contains(allLines, line => line.Contains(program.Id.ToString()));
        Assert.Contains(allLines, line => line.Contains("Paused"));
        Assert.Contains(allLines, line => line.Contains("Idle"));
        Assert.Equal(0, engine.TickCount);
    }

    [Fact]
    public void EventPagingRetainsOrderAndShowsEviction()
    {
        var engine = new GridSimulationEngine(new GridWorld(), recentEventCapacity: 6);
        for (var i = 0; i < 10; i++) engine.Step();
        var newest = string.Join('\n', LiveViewRenderer.Render(engine.GetLiveSnapshot(), 120, 30));
        var older = string.Join('\n', LiveViewRenderer.Render(engine.GetLiveSnapshot(), 120, 30, eventPage: 1));
        Assert.Contains("Evicted: 4", newest);
        Assert.True(newest.IndexOf("#10 ", StringComparison.Ordinal) < newest.IndexOf("#9 ", StringComparison.Ordinal));
        Assert.Contains("#5 ", older);
        Assert.DoesNotContain("#10 ", older);
    }

    [Fact]
    public void DisplaySanitizesEscapeSequencesAndWideTextWithoutChangingSource()
    {
        var world = new GridWorld();
        var district = new GridDistrict { Name = "District\u001b[2J" };
        world.AddDistrict(district);
        var program = new GridProgram { Name = "Name\n\u001b[2J\u754c", Occupation = new string('x', 300) };
        world.AddProgram(program, district.Id);
        var engine = new GridSimulationEngine(world);
        var frame = LiveViewRenderer.Render(engine.GetLiveSnapshot(), 60, 18);
        Assert.All(string.Concat(frame), c => Assert.InRange((int)c, 32, 126));
        Assert.Contains(frame, line => line.EndsWith('+'));
        Assert.Equal(program.Name, engine.GetSnapshot().Programs[0].Name);
    }

    [Fact]
    public async Task RedirectedLivePrintsOnceAndLeavesFollowingCommandsIntact()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        using var output = new StringWriter();
        var host = new TerminalHost(engine, DefaultCommands.Create(engine), new StringReader("live\nstep\nstatus\nexit\n"), output, false);
        Assert.Equal(0, await host.RunAsync());
        var text = output.ToString();
        Assert.Contains("Showing one snapshot", text);
        Assert.Equal(1, text.Split("GRID SNAPSHOT").Length - 1);
        Assert.DoesNotContain("Refresh: 250 ms", text);
        Assert.Contains("Tick: 1", text);
        Assert.Equal(1, engine.TickCount);
        Assert.DoesNotContain('\u001b', text);
    }

    [Theory]
    [InlineData(ConsoleKey.Q)]
    [InlineData(ConsoleKey.Escape)]
    public async Task LeavingViewPreservesRunningState(ConsoleKey key)
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        engine.Start();
        var terminal = new FakeTerminal();
        terminal.OnDraw = () => terminal.Keys.Enqueue(key);
        await new TerminalLiveView(engine, TextWriter.Null, terminal).RunAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(engine.IsRunning);
        Assert.Equal(0, engine.TickCount); // Drawing alone cannot tick.
        Assert.Single(terminal.Frames);
        Assert.True(terminal.Ended);
    }

    [Fact]
    public async Task RefreshReadsNewStateAndHandlesResizeAndNavigation()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        var terminal = new FakeTerminal { Width = 100, Height = 30 };
        terminal.OnDraw = () =>
        {
            if (terminal.Frames.Count == 1)
            {
                engine.Step(); // A separate world-control action between display refreshes.
                terminal.Width = 60;
                terminal.Height = 18;
                terminal.Keys.Enqueue(ConsoleKey.PageDown);
            }
            else terminal.Keys.Enqueue(ConsoleKey.Q);
        };
        await new TerminalLiveView(engine, TextWriter.Null, terminal).RunAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, terminal.Frames.Count);
        Assert.Contains(terminal.Frames[0], line => line.Contains("Tick: 0"));
        Assert.Contains(terminal.Frames[1], line => line.Contains("Tick: 1"));
        Assert.Contains(terminal.Frames[1], line => line.Contains("page 2/"));
        Assert.All(terminal.Frames[1], line => Assert.True(line.Length < 60));
        Assert.False(engine.IsRunning);
    }

    [Fact]
    public async Task TerminalFailureRestoresSurfaceAndFallsBackOnce()
    {
        var engine = new GridSimulationEngine(new GridWorld());
        var terminal = new FakeTerminal { ThrowOnDraw = true };
        using var output = new StringWriter();
        await new TerminalLiveView(engine, output, terminal).RunAsync();
        Assert.True(terminal.Ended);
        Assert.Contains("Showing one snapshot", output.ToString());
        Assert.Equal(0, engine.TickCount);
    }

    [Fact]
    public async Task ShrinkingBelowMinimumReturnsToPlainOutput()
    {
        var engine = new GridSimulationEngine(new GridWorld());
        var terminal = new FakeTerminal();
        terminal.OnDraw = () => terminal.Width = 10;
        using var output = new StringWriter();
        await new TerminalLiveView(engine, output, terminal).RunAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(terminal.Ended);
        Assert.Single(terminal.Frames);
        Assert.Contains("Showing one snapshot", output.ToString());
    }

    [Fact]
    public async Task CancellationRestoresTerminalAndEndsHost()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        using var cancellation = new CancellationTokenSource();
        var terminal = new FakeTerminal { OnDraw = cancellation.Cancel };
        var host = new TerminalHost(engine, DefaultCommands.Create(engine), new StringReader("live\n"),
            TextWriter.Null, false, terminal);
        Assert.Equal(0, await host.RunAsync(cancellation.Token).WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.True(terminal.Ended);
        Assert.False(engine.IsRunning);
    }

    [Fact]
    public async Task LiveReturnResumesCommandLoopWithoutPausing()
    {
        var engine = new GridSimulationEngine(new GridWorld());
        var terminal = new FakeTerminal();
        terminal.OnDraw = () => terminal.Keys.Enqueue(ConsoleKey.Q);
        using var output = new StringWriter();
        var host = new TerminalHost(engine, DefaultCommands.Create(engine),
            new StringReader("grid -start\nlive\nstatus\ngrid -pause\nstep\nexit\n"), output, false, terminal);
        Assert.Equal(0, await host.RunAsync().WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Contains("State: Running", output.ToString());
        Assert.Contains("Advanced to tick 1", output.ToString());
        Assert.True(terminal.Ended);
    }

    [Fact]
    public async Task SchedulerFaultDuringLiveRestoresTerminalAndReportsFailure()
    {
        var world = new GridWorld();
        var district = new GridDistrict();
        world.AddDistrict(district);
        world.AddProgram(new GridProgram { Energy = float.NaN }, district.Id);
        var engine = new GridSimulationEngine(world, TimeSpan.FromMilliseconds(100));
        var terminal = new FakeTerminal();
        terminal.OnDraw = engine.Start; // Start only after live mode has entered.
        using var output = new StringWriter();
        var host = new TerminalHost(engine, DefaultCommands.Create(engine), new StringReader("live\n"), output, false, terminal);
        Assert.Equal(1, await host.RunAsync().WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.True(terminal.Ended);
        Assert.Contains("Simulation stopped with an error", output.ToString());
    }

    private sealed class FakeTerminal : ILiveTerminal
    {
        public int Width { get; set; } = 100;
        public int Height { get; set; } = 30;
        public Queue<ConsoleKey> Keys { get; } = new();
        public List<IReadOnlyList<string>> Frames { get; } = new();
        public Action? OnDraw { get; set; }
        public bool ThrowOnDraw { get; set; }
        public bool Ended { get; private set; }
        public bool TryGetSize(out int width, out int height)
        {
            width = Width; height = Height;
            return Width >= 60 && Height >= 18;
        }
        public bool TryReadKey(out ConsoleKey key) => Keys.TryDequeue(out key);
        public void Begin() { }
        public void Draw(IReadOnlyList<string> lines)
        {
            if (ThrowOnDraw) throw new IOException("Surface unavailable.");
            Frames.Add(lines);
            OnDraw?.Invoke();
        }
        public void End() => Ended = true;
    }
}
