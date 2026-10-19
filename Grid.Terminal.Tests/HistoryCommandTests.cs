using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class HistoryCommandTests
{
    [Fact]
    public async Task CommandQueriesSavedEventsAndRejectsInvalidFilters()
    {
        var folder = Path.Combine(Path.GetTempPath(), "GridHistoryCommand-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            using var store = new WorldStore(Path.Combine(folder, "world.db"));
            var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
            var info = new WorldInfo(engine.GetSnapshot().WorldId, "Test", DateTimeOffset.UtcNow);
            store.Create(info, engine.GetSnapshot());
            var commands = DefaultCommands.Create(engine); HistoryCommand.Register(commands, store);
            engine.Start(); engine.Pause();
            Assert.Contains("No saved events", commands.Execute("history").Text);
            await new WorldPersistenceSession(store, info, engine).SaveAsync();
            Assert.Contains("Clock", commands.Execute("history kind=clock severity=Information limit=1").Text);
            Assert.Contains("No saved events", commands.Execute("history kind=Tick").Text);
            foreach (var argument in new[] { "limit=0", "limit=501", "entity=bad", "since=bad", "offset=-1", "other=x", "limit=1 limit=2" })
                Assert.Contains("History query failed", commands.Execute("history " + argument).Text);
        }
        finally { foreach (var file in Directory.GetFiles(folder)) File.Delete(file); Directory.Delete(folder); }
    }
}
