using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class NullificationTests
{
    [Theory]
    [InlineData("n\nexit\n")]
    [InlineData("\nexit\n")]
    [InlineData("maybe\nn\nexit\n")]
    [InlineData("")]
    public async Task CancelRetainsWorld(string answer)
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        var session = new FakeSession(); var text = new StringWriter();
        var host = new TerminalHost(engine, DefaultCommands.Create(engine), new StringReader("nge\n" + answer),
            text, false, persistence: session, interactiveConfirmation: true);
        Assert.Equal(0, await host.RunAsync());
        Assert.Equal(0, session.Deletions); Assert.Equal(1, session.Saves);
        Assert.Contains("NGE cancelled", text.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConfirmStopsWorkersAndSkipsFinalSaveEvenOnDeletionFailure(bool fails)
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        var session = new FakeSession { Fails = fails }; var text = new StringWriter();
        var host = new TerminalHost(engine, DefaultCommands.Create(engine), new StringReader("start\nNGE\nY\n"),
            text, false, persistence: session, interactiveConfirmation: true);
        Assert.Equal(fails ? 1 : 0, await host.RunAsync());
        Assert.True(session.Stopped); Assert.Equal(1, session.Deletions); Assert.Equal(0, session.Saves);
        Assert.DoesNotContain("World saved.", text.ToString()); Assert.DoesNotContain("WORLD SAVED", text.ToString());
        Assert.Equal(!fails, text.ToString().Contains("GRID NULLIFIED"));
    }

    [Fact]
    public async Task RedirectedAndEphemeralSessionsCannotDelete()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        var session = new FakeSession(); var text = new StringWriter();
        await new TerminalHost(engine, DefaultCommands.Create(engine), new StringReader("nge\ny\nexit\n"),
            text, false, persistence: session).RunAsync();
        Assert.Equal(0, session.Deletions); Assert.Contains("redirected", text.ToString());
        text.GetStringBuilder().Clear();
        await new TerminalHost(engine, DefaultCommands.Create(engine), new StringReader("nge\nexit\n"),
            text, false, interactiveConfirmation: true).RunAsync();
        Assert.Contains("no persistent world", text.ToString());
        Assert.Contains("Usage: nge", DefaultCommands.Create(engine).Execute("nge y").Text);
    }

    private sealed class FakeSession : IWorldPersistenceSession, IWorldNullificationSession
    {
        public int Saves, Deletions; public bool Fails, Stopped;
        public NullificationPlan PrepareNullification() => new(Guid.NewGuid(), "Test", ["disposable world"]);
        public Task NullifyAsync(NullificationPlan plan)
        {
            Assert.True(Stopped); Deletions++;
            return Fails ? Task.FromException(new IOException("Test deletion failure")) : Task.CompletedTask;
        }
        public Task SaveAsync(CancellationToken token = default) { Saves++; return Task.CompletedTask; }
        public async Task RunPeriodicAsync(CancellationToken token)
        { try { await Task.Delay(Timeout.Infinite, token); } catch (OperationCanceledException) { } finally { Stopped = true; } }
    }
}
