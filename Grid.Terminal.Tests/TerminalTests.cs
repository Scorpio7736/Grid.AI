using Grid.TestSupport;
using Grid.Simulation.Editing;
using Grid.Core;
using Grid.Simulation;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class TerminalTests
{
    private static (GridWorld World, GridSimulationEngine Engine, CommandDispatcher Commands) Setup()
    {
        var world = GridWorldFactory.CreateGenesis();
        var engine = new GridSimulationEngine(world);
        return (world, engine, DefaultCommands.Create(engine));
    }

    [Fact]
    public void StatusAndListsUseActualFactoryWorld()
    {
        var (world, _, commands) = Setup();
        Assert.Contains("Districts: 5 | Programs: 20", commands.Execute("status").Text);
        Assert.Contains("Paused", commands.Execute("status").Text);
        foreach (var program in world.Programs.Values)
        {
            Assert.Contains(program.Id.ToString(), commands.Execute("programs ids").Text);
            Assert.Contains(program.Name, commands.Execute("programs").Text);
        }
        foreach (var district in world.Districts.Values)
            Assert.Contains(district.Id.ToString(), commands.Execute("districts ids").Text);
    }

    [Theory]
    [InlineData("inspect district The Core")]
    [InlineData("INSPECT DISTRICT \"The Core\"")]
    [InlineData("  inspect\tdistrict 'The Core'  ")]
    public void InspectAcceptsNamesQuotesCaseAndWhitespace(string line)
    {
        var (_, _, commands) = Setup();
        var result = commands.Execute(line);
        Assert.Contains("District: The Core", result.Text);
        Assert.Contains("Population: 4", result.Text);
        Assert.Contains("Residents: Citizen", result.Text);
    }

    [Fact]
    public void InspectReadsProgramIdsAndResourceValues()
    {
        var (world, _, commands) = Setup();
        var program = world.Programs.Values.First();
        TestEdit.Update(world, program, new ProgramFieldEdit("energy", new EnergyValue(42)));
        Assert.Contains("Energy: 42", commands.Execute($"inspect program {program.Id}").Text);
        Assert.Contains(program.Occupation, commands.Execute($"inspect program {program.Name}").Text);
        var district = world.Districts.Values.First();
        district.Resources["Power"] = 12;
        Assert.Contains("Power: 12", commands.Execute($"inspect district {district.Id}").Text);
    }

    [Fact]
    public void AmbiguousNamesRequireId()
    {
        var (world, _, commands) = Setup();
        foreach (var program in world.Programs.Values) TestEdit.Update(world, program, new ProgramFieldEdit("name", new TextValue("Same")));
        Assert.Contains("Multiple matches", commands.Execute("inspect program Same").Text);
        Assert.Contains("Program: Same", commands.Execute($"inspect program {world.Programs.Keys.First()}").Text);
    }

    [Theory]
    [InlineData("inspect", "Usage:")]
    [InlineData("inspect other thing", "Usage:")]
    [InlineData("inspect program missing", "No match")]
    [InlineData("inspect district missing", "No match")]
    [InlineData("inspect district \"The Core", "Unclosed quote")]
    [InlineData("unknown", "Unknown command")]
    [InlineData("start extra", "Usage:")]
    [InlineData("exit extra", "Usage:")]
    [InlineData("step 3", "Usage:")]
    public void InvalidInputIsReportedWithoutMutation(string line, string message)
    {
        var (_, engine, commands) = Setup();
        var result = commands.Execute(line);
        Assert.Contains(message, result.Text);
        Assert.False(result.ExitRequested);
        Assert.False(engine.IsRunning);
        Assert.Equal(0, engine.TickCount);
    }

    [Fact]
    public void CommandsControlEngineAndStepsReportActualChanges()
    {
        var (world, engine, commands) = Setup();
        TestEdit.Update(world, world.Programs.Values.First(), new ProgramFieldEdit("status", new StatusValue(GridProgramStatus.Working)));
        Assert.Equal("", commands.Execute("  ").Text);
        Assert.Contains("Energy changes: 1", commands.Execute("step").Text);
        commands.Execute("tick");
        Assert.Equal(2, engine.TickCount);
        commands.Execute("start");
        Assert.True(engine.IsRunning);
        Assert.Contains("Error:", commands.Execute("step").Text);
        Assert.Equal(2, engine.TickCount);
        commands.Execute("pause");
        Assert.False(engine.IsRunning);
        Assert.Contains("Tick: 2", commands.Execute("status").Text);
        Assert.True(commands.Execute("exit").ExitRequested);
    }

    [Fact]
    public void RegistryExtendsDispatchAndGeneratedHelp()
    {
        var (_, _, commands) = Setup();
        commands.Register(new("echo", "echo <text>", "Echo text.", args => new(string.Join("|", args))));
        Assert.Equal("hello world|test", commands.Execute("ECHO \"hello world\" test").Text);
        Assert.Contains("echo <text>", commands.Execute("help").Text);
        Assert.Contains("Echo text.", commands.Execute("help").Text);
        Assert.Throws<ArgumentException>(() => commands.Register(new("ECHO", "", "", _ => new(""))));
        Assert.Throws<ArgumentException>(() => commands.Register(new("two words", "", "", _ => new(""))));
    }

    [Theory]
    [InlineData("step\nstatus\nexit\nstep\n", 1)]
    [InlineData("step\n", 1)]
    [InlineData("start\npause\nstep\nexit\n", 1)]
    [InlineData("invalid\nstep\nexit\n", 1)]
    public async Task HostHandlesCommandsExitAndEof(string script, long expected)
    {
        var (_, engine, commands) = Setup();
        using var input = new StringReader(script);
        using var output = new StringWriter();
        var code = await new TerminalHost(engine, commands, input, output, false).RunAsync();
        Assert.Equal(0, code);
        Assert.Equal(expected, engine.TickCount);
        Assert.False(engine.IsRunning);
        Assert.Contains("Advanced to tick 1", output.ToString());
        Assert.DoesNotContain("grid>", output.ToString());
        engine.Start();
        Assert.NotNull(engine.Tick()); // Scheduler has relinquished ownership.
    }

    [Fact]
    public async Task CancellationEndsSessionEvenWhileInputIsBlocked()
    {
        var (_, engine, commands) = Setup();
        using var input = new BlockingReader();
        using var output = new StringWriter();
        using var cancellation = new CancellationTokenSource();
        var run = new TerminalHost(engine, commands, input, output).RunAsync(cancellation.Token);
        try
        {
            await input.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            cancellation.Cancel();
            Assert.Equal(0, await run.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.False(engine.IsRunning);
        }
        finally { input.Release.Set(); }
    }

    [Fact]
    public async Task SchedulerFailureIsReportedAndHostReturnsFailure()
    {
        var world = GridWorldFactory.CreateGenesis();
        var engine = new GridSimulationEngine(world, TimeSpan.FromTicks(1));
        using var output = new StringWriter();
        var result = await new TerminalHost(engine, DefaultCommands.Create(engine),
            new StringReader(""), output, false).RunAsync();
        Assert.Equal(1, result);
        Assert.Contains("Simulation stopped with an error:", output.ToString());
    }

    [Fact]
    public void SnapshotIsDetachedAndCannotModifyWorld()
    {
        var (world, engine, _) = Setup();
        var program = world.Programs.Values.First();
        TestEdit.Update(world, program, new ProgramFieldEdit("status", new StatusValue(GridProgramStatus.Working)));
        var district = world.Districts.Values.First();
        district.Resources["Power"] = 10;
        var before = engine.GetSnapshot();
        engine.Step();
        district.Resources["Power"] = 20;
        Assert.Equal(100f, before.Programs.Single(p => p.Id == program.Id).Energy);
        Assert.Equal(10f, before.Districts.Single(d => d.Id == district.Id).Resources["Power"]);
        Assert.Throws<NotSupportedException>(() => ((IList<GridProgramSnapshot>)before.Programs).Clear());
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, float>)before.Districts[0].Resources).Clear());
        Assert.Equal(1, engine.GetSnapshot().TickCount);
    }

    private sealed class BlockingReader : TextReader
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ManualResetEventSlim Release { get; } = new();
        public override string? ReadLine()
        {
            Entered.TrySetResult();
            Release.Wait();
            return null;
        }
    }
}
