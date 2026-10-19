using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class PersistenceLifecycleTests
{
    [Theory]
    [InlineData("step\nexit\n")]
    [InlineData("step\n")]
    public async Task ExitAndEofStopSchedulerBeforeSaving(string input)
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        var persistence = new FakePersistence(engine);
        var output = new StringWriter();
        var code = await new TerminalHost(engine, DefaultCommands.Create(engine), new StringReader(input), output,
            false, persistence: persistence).RunAsync();
        Assert.Equal(0, code);
        Assert.Equal(1, persistence.Saved!.TickCount);
        Assert.False(persistence.Saved.IsRunning);
        Assert.True(persistence.PeriodicStopped);
        Assert.Contains("World saved.", output.ToString());
    }

    [Theory]
    [InlineData(true, false, "Autosave failed")]
    [InlineData(false, true, "Shutdown save failed")]
    public async Task FailuresAreVisibleAndReturnNonzero(bool periodicFailure, bool finalFailure, string message)
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        var persistence = new FakePersistence(engine) { PeriodicFailure = periodicFailure, FinalFailure = finalFailure };
        var output = new StringWriter();
        var code = await new TerminalHost(engine, DefaultCommands.Create(engine), new StringReader("exit\n"), output,
            false, persistence: persistence).RunAsync();
        Assert.Equal(1, code);
        Assert.Contains(message, output.ToString());
        Assert.False(engine.IsRunning);
    }

    [Fact]
    public async Task CancellationStillSavesWithUncancelledToken()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis()); engine.Start();
        var persistence = new FakePersistence(engine);
        var code = await new TerminalHost(engine, DefaultCommands.Create(engine), new StringReader(""), new StringWriter(),
            false, persistence: persistence).RunAsync(new CancellationToken(true));
        Assert.Equal(0, code); Assert.NotNull(persistence.Saved); Assert.False(persistence.Saved.IsRunning);
    }

    [Theory]
    [InlineData("--world")]
    [InlineData("--unknown")]
    [InlineData("--start --start")]
    [InlineData("--ephemeral --recover")]
    [InlineData("--ephemeral --world x.db")]
    public void InvalidStartupArgumentsAreRejected(string args) => Assert.Throws<ArgumentException>(() => TerminalOptions.Parse(args.Split(' ')));

    [Fact]
    public void StartupOptionsRetainExplicitPathAndFlags()
    {
        var options = TerminalOptions.Parse(["--WORLD", "a folder/world.db", "--recover", "--start"]);
        Assert.Equal("a folder/world.db", options.WorldPath); Assert.True(options.Start); Assert.True(options.Recover); Assert.False(options.Ephemeral);
    }

    private sealed class FakePersistence(GridSimulationEngine engine) : IWorldPersistenceSession
    {
        public bool PeriodicFailure { get; init; }
        public bool FinalFailure { get; init; }
        public bool PeriodicStopped { get; private set; }
        public GridSimulationSnapshot? Saved { get; private set; }
        public Task SaveAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Assert.True(PeriodicStopped);
            Assert.False(engine.IsRunning);
            if (FinalFailure) throw new IOException("Injected final failure");
            Saved = engine.GetSnapshot(); return Task.CompletedTask;
        }
        public async Task RunPeriodicAsync(CancellationToken token)
        {
            try
            {
                if (PeriodicFailure) throw new IOException("Injected periodic failure");
                await Task.Delay(Timeout.Infinite, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            finally { PeriodicStopped = true; }
        }
    }
}
