using System.Diagnostics;
using System.Text.Json;
using Grid.Core;
using Grid.Core.Generation;
using Grid.Simulation.Applications;
using Grid.Simulation.Behavior;
using Grid.Simulation.Resources;
using Grid.Simulation.Social;
using Xunit;
using Xunit.Abstractions;
namespace Grid.Simulation.Tests;

public sealed class AutonomyTests(ITestOutputHelper output)
{
    private sealed class Clock : TimeProvider
    {
        private long ticks = DateTimeOffset.Parse("2026-10-20T00:00:00Z").UtcTicks;
        public DateTimeOffset Now { get => new(Interlocked.Read(ref ticks), TimeSpan.Zero); set => Interlocked.Exchange(ref ticks, value.UtcTicks); }
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public void SeededChoicesRepeatAcrossRestoreAndRespectAffinity()
    {
        var world = new GridWorld(); var district = new GridDistrict { Name = "Social" }; world.AddDistrict(district);
        for (var i = 0; i < 8; i++) world.AddProgram(new GridProgram { Name = $"Agent {i}", Occupation = "Unassigned", Kind = ResidentKind.Agent }, district.Id);
        var engine = new GridSimulationEngine(world);
        foreach (var id in world.Programs.Keys) engine.ConfigureSocial(id, SocialPreference.Cooperative);
        for (var i = 0; i < 17; i++) engine.Step();
        var restored = GridSimulationEngine.Restore(engine.GetSnapshot());
        for (var i = 0; i < 100; i++) { engine.Step(); restored.Step(); }
        Assert.Equal(JsonSerializer.Serialize(engine.GetSnapshot().Social), JsonSerializer.Serialize(restored.GetSnapshot().Social));
        var actor = world.Programs.Keys.First(); var candidate = world.Programs.Keys.Last();
        Assert.Equal(DecisionRandom.Rank(world.Id, 7, actor, candidate), DecisionRandom.Rank(world.Id, 7, actor, candidate));
        Assert.NotEqual(DecisionRandom.Rank(world.Id, 7, actor, candidate), DecisionRandom.Rank(Guid.NewGuid(), 7, actor, candidate));
        Assert.NotEqual(DecisionRandom.Rank(world.Id, 7, actor, candidate), DecisionRandom.Rank(world.Id, 8, actor, candidate));
    }

    [Fact]
    public async Task SimulatedHourHandlesChurnResourcePressureMovementAndConcurrentReaders()
    {
        var clock = new Clock();
        var world = HardwareCityGenerator.Create(new(clock.Now, []), 7736);
        var start = world.City!.Read().Districts.Single(d => d.Category == HardwareKind.Network).Id;
        for (var i = 0; i < 8; i++) world.AddProgram(new GridProgram { Name = $"Persistent {i}", Occupation = "Unassigned", Kind = ResidentKind.Agent }, start);
        var engine = new GridSimulationEngine(world, timeProvider: clock);
        var ids = world.Programs.Keys.ToArray();
        foreach (var id in ids) engine.ConfigureSocial(id, SocialPreference.Cooperative);
        engine.ConfigureRoutine(ids[0], new(RoutineOccupation.Archivist, TimeSpan.TicksPerSecond * 20,
            TimeSpan.TicksPerSecond * 10, TimeSpan.TicksPerSecond, 0, 24,
            [new(ResourceMetric.RamAvailableBytes, "host", 100, "Controlled soak fixture")]));
        engine.AssignWorkplace(ids[0]);
        using var process = Process.GetCurrentProcess();
        var cpuStart = process.TotalProcessorTime; var timer = Stopwatch.StartNew();
        var memoryStart = GC.GetTotalMemory(false); long peakManaged = memoryStart;
        var stepMs = new List<double>(); var readMs = new List<double>();
        using var stop = new CancellationTokenSource();
        var reader = Task.Run(async () => {
            while (!stop.IsCancellationRequested)
            {
                var watch = Stopwatch.StartNew(); var snapshot = engine.GetSnapshot();
                readMs.Add(watch.Elapsed.TotalMilliseconds); GridSimulationEngine.ValidateSnapshot(snapshot);
                await Task.Delay(1);
            }
        });
        var kinds = new HashSet<string>(); var actions = new HashSet<ResidentAction>();
        var priorProcesses = new List<ProcessLifetime>(); var sawZero = false; var sawHigh = false;
        long previousEventTick = 0;
        try
        {
            for (var tick = 0; tick < 3600; tick++)
            {
                clock.Now = clock.Now.AddSeconds(1);
                if (tick % 10 == 0)
                {
                    // Alternate empty, moderate and maximum populations; changing PID lifetimes model starts/exits.
                    var count = tick % 300 < 100 ? 0 : tick % 300 < 200 ? 32 : 512;
                    var apps = Enumerable.Range(0, count).Select(i => new ObservedApplication($"app-{i}", $"App {i}",
                        [new ProcessLifetime(i + 1, clock.Now)], 4096) { Activity = i % 2 == 0 ? ApplicationActivity.Running : ApplicationActivity.OnCall }).ToArray();
                    engine.ReconcileApplications(new(clock.Now, true, apps, "Controlled soak") { ConfirmedExited = priorProcesses });
                    priorProcesses = apps.SelectMany(a => a.Processes).ToList();
                }
                var headroom = tick % 120 < 30 ? 0 : 100;
                engine.UpdateHostResources([new(ResourceMetric.RamAvailableBytes, "host", headroom, ObservationStatus.Available, clock.Now, "Controlled soak")]);
                var step = Stopwatch.StartNew(); engine.Step(); stepMs.Add(step.Elapsed.TotalMilliseconds);
                if (tick % 10 == 0)
                {
                    var snapshot = engine.GetSnapshot(); GridSimulationEngine.ValidateSnapshot(snapshot);
                    var population = snapshot.Programs.Count(p => p.Kind == ResidentKind.Application);
                    sawZero |= population == 0; sawHigh |= population == 512;
                    Assert.Equal(8, snapshot.Programs.Count(p => p.Kind == ResidentKind.Agent));
                    Assert.All(snapshot.Programs.Where(p => p.Kind == ResidentKind.Application), p => {
                        Assert.Null(p.Behavior!.Routine); Assert.Equal(0, p.Behavior.CompletedUnits);
                    });
                    actions.Add(snapshot.Programs.Single(p => p.Id == ids[0]).Behavior!.Action);
                    var history = engine.CaptureHistory();
                    foreach (var item in history.Events)
                    {
                        Assert.InRange(item.Tick, previousEventTick, snapshot.TickCount); previousEventTick = item.Tick;
                        kinds.Add(item.Kind);
                        if (item.Kind == "Movement")
                        {
                            using var json = JsonDocument.Parse(item.ContextJson);
                            var from = json.RootElement.GetProperty("fromDistrictId").GetGuid();
                            var to = json.RootElement.GetProperty("toDistrictId").GetGuid();
                            Assert.NotEqual(from, to);
                            Assert.Contains(snapshot.City!.Read().Roads, road => road.From == from && road.To == to || road.From == to && road.To == from);
                            Assert.Contains(snapshot.Districts, d => d.Id == from); Assert.Contains(snapshot.Districts, d => d.Id == to);
                        }
                    }
                    Assert.Equal(0, history.DroppedCount);
                    engine.AcknowledgeHistory(history); // Test consumer only; production acknowledges after durable save.
                    peakManaged = Math.Max(peakManaged, GC.GetTotalMemory(false));
                }
            }
        }
        finally { stop.Cancel(); await reader.WaitAsync(TimeSpan.FromSeconds(10)); }
        Assert.True(sawZero && sawHigh); Assert.NotEmpty(readMs);
        Assert.Equal(TimeSpan.FromHours(1), engine.GetSnapshot().ElapsedSimulationTime);
        Assert.Contains("Movement", kinds); Assert.Contains("SocialInteraction", kinds); Assert.Contains("WorkCompleted", kinds);
        Assert.Contains(ResidentAction.WaitingForResources, actions); Assert.Contains(ResidentAction.Working, actions);
        Assert.True(engine.GetSnapshot().Programs.Single(p => p.Id == ids[0]).Behavior!.CompletedUnits > 0);
        Assert.All(engine.GetSnapshot().Social.Residents, r => Assert.InRange(r.Memories.Count, 0, 16));
        timer.Stop(); process.Refresh();
        output.WriteLine($"Simulated seconds=3600; wall seconds={timer.Elapsed.TotalSeconds:F3}; CPU seconds={(process.TotalProcessorTime-cpuStart).TotalSeconds:F3}; peak process working set={process.PeakWorkingSet64}; managed start={memoryStart}; sampled managed peak={peakManaged}; managed end={GC.GetTotalMemory(false)}");
        output.WriteLine($"Step ms p95={stepMs.Order().ElementAt((int)(stepMs.Count*.95)):F3}, max={stepMs.Max():F3}; concurrent reads={readMs.Count}; read ms p95={readMs.Order().ElementAt((int)(readMs.Count*.95)):F3}, max={readMs.Max():F3}");
    }
}
