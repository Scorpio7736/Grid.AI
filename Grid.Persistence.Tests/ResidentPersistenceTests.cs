using Grid.Core;
using Grid.Simulation;
using Grid.Simulation.Applications;
using Xunit;
namespace Grid.Persistence.Tests;

public sealed class ResidentPersistenceTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "GridResidents-" + Guid.NewGuid().ToString("N"));
    private string PathName => Path.Combine(directory, "world.db");
    public ResidentPersistenceTests() => Directory.CreateDirectory(directory);
    public void Dispose() { foreach (var file in Directory.GetFiles(directory)) File.Delete(file); Directory.Delete(directory); }
    private static void Observe(GridSimulationEngine engine, string key)
    {
        var now = DateTimeOffset.UtcNow;
        engine.ReconcileApplications(new(now, true, [new(key, "Observed", [new(123, now.AddMinutes(-1))], 2048)], "Fake"));
    }
    [Fact]
    public async Task SaveRestartAndRecoveryRetainAgentButNeverResurrectApplications()
    {
        var world = new GridWorld(); var district = new GridDistrict { Name = "District" }; world.AddDistrict(district);
        var agent = new GridProgram { Name = "Coordinator", Occupation = "Agent", Kind = ResidentKind.Agent };
        var special = new GridProgram { Name = "Service", Occupation = "Special", Kind = ResidentKind.Special };
        world.AddProgram(agent, district.Id); world.AddProgram(special, district.Id);
        var engine = new GridSimulationEngine(world); Observe(engine, "old-app"); engine.Step();
        var oldApplication = engine.GetSnapshot().Programs.Single(p => p.Kind == ResidentKind.Application);
        using var store = new WorldStore(PathName); var info = new WorldInfo(world.Id, "World", DateTimeOffset.UtcNow);
        store.Create(info, engine.GetSnapshot()); await new WorldPersistenceSession(store, info, engine).SaveAsync();
        var saved = store.Load(); Assert.Equal(2, saved.State.Programs.Count); Assert.Equal(2, saved.State.Districts[0].ProgramIds.Count);
        Assert.Contains(saved.State.Programs, p => p.Id == agent.Id && p.Kind == ResidentKind.Agent);
        var restored = GridSimulationEngine.Restore(saved.State); Observe(restored, "different-app");
        Assert.DoesNotContain(restored.GetSnapshot().Programs, p => p.Id == oldApplication.Id);
        Assert.Equal(1, restored.TickCount); Assert.False(restored.IsRunning);
        File.WriteAllText(PathName, "corrupt"); store.Recover();
        var recovered = GridSimulationEngine.Restore(store.Load().State);
        Assert.Equal(2, recovered.GetSnapshot().Programs.Count); Observe(recovered, "third-app");
        Assert.Equal(3, recovered.GetSnapshot().Programs.Count);
    }
    [Fact]
    public void LegacyConversionArchivesAllOriginalResidentsAndIsIdempotent()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis()); engine.Step();
        using var store = new WorldStore(PathName); var info = new WorldInfo(engine.GetSnapshot().WorldId, "Legacy", DateTimeOffset.UtcNow);
        store.Create(info, engine.GetSnapshot()); var result = store.ArchiveLegacyResidents();
        Assert.Empty(result.Saved.State.Programs); Assert.All(result.Saved.State.Districts, d => Assert.Empty(d.ProgramIds));
        Assert.Equal(1, result.Saved.State.TickCount); Assert.NotNull(result.ArchivePath);
        using var archive = new WorldStore(result.ArchivePath!);
        Assert.Equal(20, archive.Load().State.Programs.Count);
        Assert.All(archive.Load().State.Programs, p => Assert.Equal(ResidentKind.Legacy, p.Kind));
        Assert.Null(store.ArchiveLegacyResidents().ArchivePath);
    }
    [Fact]
    public void CancelledLegacyConversionPreservesOriginalPopulation()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        using var store = new WorldStore(PathName); store.Create(new(engine.GetSnapshot().WorldId, "Legacy", DateTimeOffset.UtcNow), engine.GetSnapshot());
        Assert.Throws<OperationCanceledException>(() => store.ArchiveLegacyResidents(new CancellationToken(true)));
        Assert.Equal(20, store.Load().State.Programs.Count);
    }
    [Fact]
    public void FailedConversionWriteLeavesOriginalAndRecoverableArchive()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        using (var store = new WorldStore(PathName)) store.Create(new(engine.GetSnapshot().WorldId, "Legacy", DateTimeOffset.UtcNow), engine.GetSnapshot());
        using var failing = new WorldStore(PathName, _ => throw new IOException("Injected write failure"));
        Assert.Throws<IOException>(() => failing.ArchiveLegacyResidents());
        Assert.Equal(20, failing.Load().State.Programs.Count);
        using var archive = new WorldStore(Assert.Single(Directory.GetFiles(directory, "*.legacy-*.db")));
        Assert.Equal(20, archive.Load().State.Programs.Count);
    }
}
