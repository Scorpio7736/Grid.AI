using Grid.Core;
using Grid.Simulation.Applications;
using Grid.Simulation.Editing;
using Grid.Simulation.Resources;
using Grid.TestSupport;
using Xunit;
namespace Grid.Simulation.Tests;

public sealed class ApplicationResidentTests
{
    private sealed class Time : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.Parse("2026-10-19T00:00:00Z");
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class Fixture
    {
        public readonly Time Time = new();
        public readonly GridWorld World = new();
        public readonly GridSimulationEngine Engine;
        public readonly TestEditAuthority Authority;
        public Fixture()
        {
            World.AddDistrict(new() { Name = "Apps" });
            Authority = new(World.InstanceId);
            Engine = new(World, timeProvider: Time, editAuthorizer: Authority);
        }
        public ObservedApplication App(int pid = 10, string key = "app", int started = -100) =>
            new(key, "Editor", [new(pid, DateTimeOffset.Parse("2026-10-19T00:00:00Z").AddSeconds(started))], 4096);
        public void Sample(bool complete, params ObservedApplication[] apps)
        { Time.Now = Time.Now.AddSeconds(2); Engine.ReconcileApplications(new(Time.Now, complete, apps, "Fake")); }
    }

    [Fact]
    public void GroupsRetainIdentityAcrossHelperChurnButPidReuseCreatesNewIncarnation()
    {
        var f = new Fixture(); var a = f.App(); f.Sample(true, a);
        var original = Assert.Single(f.Engine.GetSnapshot().Programs);
        f.Sample(true, a with { Processes = [.. a.Processes, f.App(11).Processes[0]] });
        Assert.Equal(original.Id, Assert.Single(f.Engine.GetSnapshot().Programs).Id);
        f.Sample(true, f.App(started: 1));
        var replacement = Assert.Single(f.Engine.GetSnapshot().Programs);
        Assert.NotEqual(original.Id, replacement.Id); Assert.NotEqual(original.Application!.Incarnation, replacement.Application!.Incarnation);
        Assert.Single(f.Engine.GetSnapshot().Districts[0].ProgramIds); Assert.Equal(0, f.Engine.TickCount);
        Assert.Throws<KeyNotFoundException>(() => f.World.MoveProgram(original.Id, replacement.DistrictId));
    }

    [Fact]
    public void IncompleteSamplesNeverRemoveAndCompleteAbsenceRequiresTwoConsecutiveSamples()
    {
        var f = new Fixture(); f.Sample(true, f.App()); f.Sample(false); f.Sample(false);
        Assert.False(Assert.Single(f.Engine.GetSnapshot().Programs).Application!.Confirmed);
        f.Sample(true); Assert.Single(f.Engine.GetSnapshot().Programs);
        f.Sample(false); f.Sample(true); Assert.Single(f.Engine.GetSnapshot().Programs);
        f.Sample(true); Assert.Empty(f.Engine.GetSnapshot().Programs); Assert.Empty(f.Engine.GetSnapshot().Districts[0].ProgramIds);
    }

    [Fact]
    public void ConfirmedPidExitRemovesEvenWhenUnrelatedProcessesAreDenied()
    {
        var f = new Fixture(); var app = f.App(); f.Sample(false, app);
        f.Time.Now = f.Time.Now.AddSeconds(2);
        f.Engine.ReconcileApplications(new(f.Time.Now, false, [], "Partial enumeration") { ConfirmedExited = app.Processes });
        Assert.Empty(f.Engine.GetSnapshot().Programs);
    }

    [Fact]
    public void StaleDuplicateAndFutureSamplesDoNotInventActivityAndReadsExposeExpiry()
    {
        var f = new Fixture(); f.Sample(true, f.App()); var before = f.Engine.GetSnapshot();
        f.Engine.ReconcileApplications(new(f.Time.Now, true, [], "Duplicate"));
        f.Engine.ReconcileApplications(new(f.Time.Now.AddMinutes(1), true, [], "Future"));
        f.Time.Now = f.Time.Now.AddSeconds(11);
        Assert.False(Assert.Single(f.Engine.GetSnapshot().Programs).Application!.Confirmed);
        Assert.Equal(before.Programs[0].Id, f.Engine.GetSnapshot().Programs[0].Id);
        Assert.Null(f.Engine.GetSnapshot().Programs[0].Condition!.Score);
    }

    [Fact]
    public void InvalidObservationIsRejectedBeforeAnyMembershipChanges()
    {
        var f = new Fixture(); f.Sample(true, f.App());
        Assert.Throws<ArgumentException>(() => f.Sample(true, f.App(), f.App()));
        Assert.Single(f.Engine.GetSnapshot().Programs);
        Assert.Single(f.Engine.GetSnapshot().Districts[0].ProgramIds);
    }

    [Fact]
    public void ConditionRequiresSourcedBudgetsAndAppEnergyDoesNotDrainOnTicks()
    {
        var f = new Fixture(); f.Sample(true, f.App()); var p = f.Engine.GetSnapshot().Programs[0];
        Assert.Null(p.Condition!.Score);
        f.Engine.SetResidentRequirements(p.Id, [new(ResourceMetric.RamAvailableBytes, "host", 100, "Configured additional-work budget")]);
        f.Engine.UpdateHostResources([new(ResourceMetric.RamAvailableBytes, "host", 50, ObservationStatus.Available, f.Time.Now, "Fake")]);
        Assert.Equal(50, f.Engine.GetSnapshot().Programs[0].Condition!.Score);
        f.Engine.Step(); Assert.Equal(p.Energy, f.Engine.GetSnapshot().Programs[0].Energy);
        f.Engine.MarkHostResourcesUnavailable("Denied"); Assert.Null(f.Engine.GetSnapshot().Programs[0].Condition!.Score);
    }

    [Fact]
    public void ObservationFieldsCannotBeEditedAndExitedPreviewCannotTargetRelaunch()
    {
        var f = new Fixture(); f.Sample(true, f.App()); var p = f.Engine.GetSnapshot().Programs[0];
        var invalid = f.Engine.PrepareEdit(f.Authority.Session, new(p.Id, [new("energy", new EnergyValue(5))]));
        Assert.Equal(ProgramEditOutcome.ValidationFailed, invalid.Outcome);
        var preview = f.Engine.PrepareEdit(f.Authority.Session, new(p.Id, [new("name", new TextValue("Alias"))])).Preview!;
        f.Sample(true, f.App(started: 1));
        Assert.Equal(ProgramEditOutcome.NotFound, f.Engine.ApplyEdit(f.Authority.Session, preview).Outcome);
        Assert.Equal("Editor", f.Engine.GetSnapshot().Programs[0].Name);
    }

    [Fact]
    public void IncompleteDisjointGroupDoesNotReplaceKnownIncarnation()
    {
        var f = new Fixture(); f.Sample(true, f.App()); var id = f.Engine.GetSnapshot().Programs[0].Id;
        f.Sample(false, f.App(11)); Assert.Equal(id, Assert.Single(f.Engine.GetSnapshot().Programs).Id);
        Assert.False(f.Engine.GetSnapshot().Programs[0].Application!.Confirmed);
    }

    [Fact]
    public async Task FakeProviderRunsWithZeroApplicationsAndDoesNotAdvanceClock()
    {
        var f = new Fixture(); f.Time.Now = f.Time.Now.AddSeconds(1);
        await new ApplicationMonitor(new Fake(new(f.Time.Now, true, [], "Fake")), f.Engine).SampleOnceAsync();
        Assert.Empty(f.Engine.GetSnapshot().Programs); Assert.Equal(0, f.Engine.TickCount);
    }
    private sealed class Fake(ApplicationObservation sample) : IApplicationDiscovery
    { public Task<ApplicationObservation> SampleAsync(CancellationToken token) => Task.FromResult(sample); }

    [Fact]
    public async Task RapidChurnWithConcurrentTicksKeepsMembershipAndExpiredIdentitiesConsistent()
    {
        var f = new Fixture();
        var ticks = Task.Run(() => { for (var i = 0; i < 100; i++) f.Engine.Step(); });
        for (var i = 0; i < 100; i++)
        {
            f.Sample(true, f.App(pid: i + 1));
            var snapshot = f.Engine.GetSnapshot();
            GridSimulationEngine.ValidateSnapshot(snapshot);
            Assert.Single(snapshot.Programs); Assert.Single(snapshot.Districts[0].ProgramIds);
        }
        await ticks;
        Assert.Equal(100, f.Engine.TickCount);
    }

    [Fact]
    public void PartialGroupRetainsUnknownHelpersUntilTheyAreConfirmedExited()
    {
        var f = new Fixture(); var app = f.App(); var helper = f.App(11).Processes[0];
        f.Sample(true, app with { Processes = [.. app.Processes, helper] });
        f.Sample(false, app);
        Assert.Equal(2, f.Engine.GetSnapshot().Programs[0].Application!.Processes.Count);
        f.Time.Now = f.Time.Now.AddSeconds(2);
        f.Engine.ReconcileApplications(new(f.Time.Now, false, [], "Partial") { ConfirmedExited = app.Processes });
        Assert.Single(f.Engine.GetSnapshot().Programs);
    }
}
