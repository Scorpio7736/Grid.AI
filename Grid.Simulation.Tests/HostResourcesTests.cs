using Grid.Core;
using Grid.Simulation.Resources;
using Xunit;

namespace Grid.Simulation.Tests;

public sealed class HostResourcesTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-19T00:00:00Z");
    private static ResourceReading Reading(double? value = 100, ObservationStatus status = ObservationStatus.Available) =>
        new(ResourceMetric.RamAvailableBytes, "host", value, status, Now, "Fake provider");
    private static readonly ResourceRequirement Demand = new(ResourceMetric.RamAvailableBytes, "host", 100, "Test additional work budget");

    [Fact]
    public void ConditionUsesWeakestRequiredHeadroomAndSmoothsOnlyNewSamples()
    {
        var policy = new ResourceConditionPolicy();
        var demands = new[] { Demand, new ResourceRequirement(ResourceMetric.CpuAvailablePercent, "cpu", 20, "Configured budget") };
        var cpu = new ResourceReading(ResourceMetric.CpuAvailablePercent, "cpu", 10, ObservationStatus.Available, Now, "Fake");
        Assert.Equal(50d, policy.Evaluate([Reading(200), cpu], demands, Now).Score);
        var next = new[] { Reading(200) with { ObservedAtUtc = Now.AddSeconds(2) }, cpu with { Value = 20, ObservedAtUtc = Now.AddSeconds(2) } };
        Assert.Equal(62.5d, policy.Evaluate(next, demands, Now.AddSeconds(2)).Score);
        Assert.Equal(62.5d, policy.Evaluate(next, demands, Now.AddSeconds(3)).Score);
    }

    [Theory]
    [InlineData(ObservationStatus.Unsupported)]
    [InlineData(ObservationStatus.Unavailable)]
    [InlineData(ObservationStatus.WarmingUp)]
    [InlineData(ObservationStatus.Stale)]
    public void UnknownInputResetsPriorScore(ObservationStatus status)
    {
        var policy = new ResourceConditionPolicy();
        Assert.Equal(100d, policy.Evaluate([Reading()], [Demand], Now).Score);
        Assert.Null(policy.Evaluate([Reading(null, status)], [Demand], Now).Score);
        Assert.Equal(0d, policy.Evaluate([Reading(0)], [Demand], Now).Score);
    }

    [Fact]
    public void MissingRequirementsStaleFutureAndMissingReadingsAreUnknown()
    {
        var policy = new ResourceConditionPolicy();
        Assert.Null(policy.Evaluate([Reading()], [], Now).Score);
        Assert.Null(policy.Evaluate([], [Demand], Now).Score);
        Assert.Null(policy.Evaluate([Reading()], [Demand], Now.AddSeconds(11)).Score);
        Assert.Null(policy.Evaluate([Reading()], [Demand], Now.AddSeconds(-1)).Score);
    }

    [Fact]
    public void ChangingRequirementsResetsSmoothingAndOldObservationsAreRejected()
    {
        var policy = new ResourceConditionPolicy();
        Assert.Equal(100d, policy.Evaluate([Reading()], [Demand], Now).Score);
        Assert.Equal(50d, policy.Evaluate([Reading()], [Demand with { Required = 200 }], Now).Score);
        var latest = Reading() with { ObservedAtUtc = Now.AddSeconds(2) };
        policy.Evaluate([latest], [Demand], Now.AddSeconds(2));
        Assert.Null(policy.Evaluate([Reading()], [Demand], Now.AddSeconds(2)).Score);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidObservationsCannotReplaceGoodState(double value)
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), timeProvider: new FakeTime());
        engine.UpdateHostResources([Reading()]);
        Assert.Throws<ArgumentException>(() => engine.UpdateHostResources([Reading(value)]));
        Assert.Equal(100d, engine.GetHostResources().Single().Value);
    }

    [Fact]
    public void InvalidUnitsSourcesDuplicateMetricsAndDemandsAreRejected()
    {
        var policy = new ResourceConditionPolicy();
        Assert.Throws<ArgumentException>(() => policy.Evaluate([Reading()], [Demand with { Required = 0 }], Now));
        Assert.Throws<ArgumentException>(() => policy.Evaluate([Reading()], [Demand with { Provenance = "" }], Now));
        Assert.Throws<ArgumentException>(() => policy.Evaluate([Reading()], [Demand, Demand], Now));
        Assert.Throws<ArgumentException>(() => policy.Evaluate([Reading()], [Demand with { Metric = ResourceMetric.NetworkSendBytesPerSecond }], Now));
        Assert.Throws<ArgumentException>(() => (Reading(101) with { Metric = ResourceMetric.CpuAvailablePercent }).Validate());
        Assert.Throws<ArgumentException>(() => (Reading(2) with { Metric = ResourceMetric.NetworkConnected }).Validate());
        Assert.Throws<ArgumentException>(() => Reading(1, ObservationStatus.Unavailable).Validate());
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        Assert.Throws<ArgumentException>(() => engine.UpdateHostResources([Reading(), Reading()]));
    }

    [Fact]
    public async Task FakeProviderObservationsAreDetachedExpireAndDoNotMutateWorldOrHistory()
    {
        var time = new FakeTime(); var world = GridWorldFactory.CreateGenesis();
        var engine = new GridSimulationEngine(world, timeProvider: time);
        var original = engine.GetSnapshot();
        var input = new List<ResourceReading> { Reading() };
        var provider = new FakeProvider { Readings = input };
        var monitor = new HostTelemetryMonitor(provider, engine);
        await monitor.SampleOnceAsync(); input.Clear();
        Assert.Single(engine.GetHostResources()); Assert.Single(engine.GetLiveSnapshot().HostResources);
        Assert.Equal(original, engine.CaptureHistory().State with { Programs = original.Programs, Districts = original.Districts });
        Assert.Equal(original.Programs, engine.GetSnapshot().Programs);
        Assert.All(world.Districts.Values, d => Assert.Empty(d.Resources));
        Assert.Empty(engine.CaptureHistory().Events);
        time.Now = Now.AddSeconds(11);
        var stale = Assert.Single(engine.GetHostResources()); Assert.Null(stale.Value); Assert.Equal(ObservationStatus.Stale, stale.Status);
    }

    [Fact]
    public async Task ProviderFailureClearsValuesAndCancellationStopsMonitoring()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), timeProvider: new FakeTime());
        engine.UpdateHostResources([Reading()]);
        var provider = new FakeProvider { Failure = new IOException("Unavailable") };
        var monitor = new HostTelemetryMonitor(provider, engine);
        await monitor.SampleOnceAsync();
        Assert.Null(engine.GetHostResources().Single().Value);
        Assert.Equal(ObservationStatus.Unavailable, engine.GetHostResources().Single().Status);
        await monitor.RunAsync(new CancellationToken(true));
        await Assert.ThrowsAsync<OperationCanceledException>(() => monitor.SampleOnceAsync(new CancellationToken(true)));
    }

    private sealed class FakeProvider : IHostTelemetryProvider
    {
        public IReadOnlyList<ResourceReading> Readings { get; init; } = [];
        public Exception? Failure { get; init; }
        public Task<IReadOnlyList<ResourceReading>> SampleAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Failure is null ? Task.FromResult(Readings) : Task.FromException<IReadOnlyList<ResourceReading>>(Failure);
        }
    }
    private sealed class FakeTime : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = HostResourcesTests.Now;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
