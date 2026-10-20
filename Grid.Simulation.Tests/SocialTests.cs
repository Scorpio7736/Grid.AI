using Grid.Core;
using Grid.Simulation.Behavior;
using Grid.Simulation.Social;
using Xunit;
namespace Grid.Simulation.Tests;

public sealed class SocialTests
{
    private static (GridSimulationEngine Engine, Guid District, Guid[] Ids) Fixture(int count, SocialPreference preference = SocialPreference.Cooperative,
        TimeSpan? step = null)
    {
        var world = new GridWorld();
        var district = new GridDistrict { Name = "Community" }; world.AddDistrict(district);
        var ids = new List<Guid>();
        for (var i = 0; i < count; i++)
        {
            var program = new GridProgram { Name = $"Agent {i}", Occupation = "Unassigned", Kind = ResidentKind.Agent };
            world.AddProgram(program, district.Id); ids.Add(program.Id);
        }
        var engine = new GridSimulationEngine(world, step ?? TimeSpan.FromSeconds(10));
        foreach (var id in ids) engine.ConfigureSocial(id, preference);
        return (engine, district.Id, ids.ToArray());
    }

    [Fact]
    public void CooperationCreatesReciprocalRelationshipsMemoriesAndExplicitCivicConsumption()
    {
        var (engine, _, ids) = Fixture(2);
        engine.Step();
        var state = engine.GetSnapshot();
        Assert.Equal(1, state.Social.Round); Assert.Equal(3, state.Social.Pools[0].Available);
        foreach (var resident in state.Social.Residents)
        {
            Assert.Equal(2, Assert.Single(resident.Relationships).Affinity);
            Assert.Equal(SocialOutcome.Cooperated, Assert.Single(resident.Memories).Outcome);
            Assert.Equal(1, resident.CivicCredits);
            Assert.NotEqual(resident.ProgramId, resident.Memories[0].PeerId);
        }
        Assert.All(state.Programs, p => { Assert.Equal(100, p.Energy); Assert.Equal(0, p.Behavior!.CompletedUnits); });
        Assert.Contains(engine.CaptureHistory().Events, e => e.Kind == "SocialInteraction");
        Assert.Equal(2, ids.Length);
        GridSimulationEngine.ValidateSnapshot(state);
    }

    [Fact]
    public void ScarcityAndRecoveryAreBoundedAndDoNotInventHostResources()
    {
        var (engine, district, ids) = Fixture(2);
        engine.ConfigureCivicPool(district, 1, 0); engine.Step(); engine.Step();
        Assert.Equal(0, engine.GetSnapshot().Social.Pools[0].Available);
        Assert.All(engine.GetSnapshot().Social.Residents, r => Assert.Equal(SocialGoal.WaitForCivicResources, r.Goal));
        engine.ConfigureCivicPool(district, 1, 1); engine.Step();
        Assert.All(engine.GetSnapshot().Social.Residents, r => Assert.Equal(SocialOutcome.Cooperated, r.Memories[^1].Outcome));
        foreach (var id in ids) engine.ConfigureSocial(id, SocialPreference.Reserved);
        for (var i = 0; i < 5; i++) engine.Step();
        Assert.Equal(1, engine.GetSnapshot().Social.Pools[0].Available);
        Assert.Empty(engine.GetHostResources());
    }

    [Fact]
    public void CompetitionRotatesWinnersAndChangesRememberedAffinity()
    {
        var (engine, district, _) = Fixture(2, SocialPreference.Competitive);
        engine.ConfigureCivicPool(district, 1, 1);
        engine.Step(); engine.Step();
        foreach (var r in engine.GetSnapshot().Social.Residents)
        {
            Assert.Equal(1, r.CivicCredits);
            Assert.Contains(r.Memories, m => m.Outcome == SocialOutcome.WonContest);
            Assert.Contains(r.Memories, m => m.Outcome == SocialOutcome.LostContest);
            Assert.Equal(-2, r.Relationships[0].Affinity);
        }
    }

    [Fact]
    public void PreferencesAndRememberedAffinityGuidePartnerChoice()
    {
        var (engine, _, _) = Fixture(3);
        var snapshot = engine.GetSnapshot();
        var roster = snapshot.Social.Residents.OrderBy(r => r.ProgramId).ToArray();
        roster[0] = roster[0] with { Relationships = [new(roster[2].ProgramId, 50, 0)] };
        var restored = GridSimulationEngine.Restore(snapshot with { Social = snapshot.Social with { Residents = roster } });
        restored.Step();
        var result = restored.GetSnapshot().Social.Residents.Single(r => r.ProgramId == roster[0].ProgramId);
        Assert.Equal(roster[2].ProgramId, result.Memories[0].PeerId);
        Assert.Empty(restored.GetSnapshot().Social.Residents.Single(r => r.ProgramId == roster[1].ProgramId).Memories);
    }

    [Fact]
    public void MemoriesRelationshipsCreditsAndOccupationHistoryStayBounded()
    {
        var (engine, _, ids) = Fixture(20);
        for (var i = 0; i < 130; i++) engine.Step();
        Assert.All(engine.GetSnapshot().Social.Residents, r => {
            Assert.InRange(r.Relationships.Count, 1, 16); Assert.Equal(16, r.Memories.Count);
            Assert.InRange(r.CivicCredits, 0, 100); Assert.All(r.Relationships, x => Assert.InRange(x.Affinity, -100, 100));
        });
        for (var i = 0; i < 12; i++)
            engine.ConfigureRoutine(ids[0], new((RoutineOccupation)(i % 4), TimeSpan.TicksPerSecond,
                TimeSpan.TicksPerSecond, TimeSpan.TicksPerSecond, 0, 24, []));
        Assert.Equal(8, engine.GetSnapshot().Social.Residents.Single(r => r.ProgramId == ids[0]).Occupations.Count);
        GridSimulationEngine.ValidateSnapshot(engine.GetSnapshot());
    }

    [Fact]
    public void WorkingMovingAndSeparateDistrictsCannotInteract()
    {
        var (engine, _, _) = Fixture(2);
        var snapshot = engine.GetSnapshot();
        var first = snapshot.Programs[0];
        var routine = new WorkRoutine(RoutineOccupation.Archivist, TimeSpan.TicksPerSecond * 10,
            TimeSpan.TicksPerSecond, TimeSpan.TicksPerSecond, 0, 24, []);
        var projected = snapshot with { TickCount = 1,
            Programs = [first with { Behavior = ResidentBehavior.Initial with { Routine = routine, Action = ResidentAction.Working } }, snapshot.Programs[1]] };
        Assert.Empty(SocialPolicy.Next(projected, TimeSpan.FromSeconds(10)).Interactions);
        projected = projected with { Programs = [first with { Behavior = ResidentBehavior.Initial with { Action = ResidentAction.Moving } }, snapshot.Programs[1]] };
        Assert.Empty(SocialPolicy.Next(projected, TimeSpan.FromSeconds(10)).Interactions);
        var otherDistrict = Guid.NewGuid();
        projected = snapshot with { TickCount = 1, Districts = [.. snapshot.Districts, new(otherDistrict, "Other", "", [], new Dictionary<string, float>())],
            Programs = [first with { DistrictId = otherDistrict }, snapshot.Programs[1]],
            Social = snapshot.Social with { Pools = [.. snapshot.Social.Pools, new(otherDistrict, 4, 4, 1)] } };
        Assert.Empty(SocialPolicy.Next(projected, TimeSpan.FromSeconds(10)).Interactions);
    }

    [Fact]
    public void FailedTickAndCancellationDoNotSpendTokensOrWriteMemories()
    {
        var (engine, _, _) = Fixture(2);
        var state = engine.GetSnapshot();
        var programs = state.Programs.Select(p => p with { Revision = long.MaxValue }).ToArray();
        var restored = GridSimulationEngine.Restore(state with { Programs = programs });
        Assert.Throws<OverflowException>(() => restored.Step());
        Assert.Equal(0, restored.TickCount); Assert.Equal(4, restored.GetSnapshot().Social.Pools[0].Available);
        Assert.All(restored.GetSnapshot().Social.Residents, r => Assert.Empty(r.Memories));
        Assert.Empty(restored.CaptureHistory().Events);
        Assert.Throws<OperationCanceledException>(() => engine.Step(new CancellationToken(true)));
        Assert.Equal(0, engine.GetSnapshot().Social.Round);
    }

    [Fact]
    public void TimingAndReplayAreDeterministicWithoutOfflineOrLargeStepCatchup()
    {
        var (engine, _, _) = Fixture(2, step: TimeSpan.FromSeconds(1));
        var second = GridSimulationEngine.Restore(engine.GetSnapshot());
        for (var i = 0; i < 9; i++) { engine.Step(); second.Step(); }
        Assert.Equal(0, engine.GetSnapshot().Social.Round);
        engine.Step(); second.Step();
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(engine.GetSnapshot().Social),
            System.Text.Json.JsonSerializer.Serialize(second.GetSnapshot().Social));
        var (large, _, _) = Fixture(2, step: TimeSpan.FromDays(1));
        large.Step(); Assert.Equal(1, large.GetSnapshot().Social.Round);
    }

    [Fact]
    public void ApplicationsAndLegacyResidentsCannotOptInAndInvalidSavedStateIsRejected()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        Assert.Throws<ArgumentException>(() => engine.ConfigureSocial(engine.GetSnapshot().Programs[0].Id, SocialPreference.Cooperative));
        var now = DateTimeOffset.UtcNow;
        engine.ReconcileApplications(new(now, true, [new("app", "App", [new(99, now.AddSeconds(-1))], 1024)], "Fake"));
        Assert.Throws<ArgumentException>(() => engine.ConfigureSocial(engine.GetSnapshot().Programs.Single(p => p.Kind == ResidentKind.Application).Id, SocialPreference.Cooperative));
        var (valid, _, _) = Fixture(2); var state = valid.GetSnapshot();
        Assert.Throws<InvalidDataException>(() => GridSimulationEngine.Restore(state with {
            Social = state.Social with { Pools = [state.Social.Pools[0] with { Available = -1 }] } }));
        var resident = state.Social.Residents[0] with { Relationships = [new(Guid.NewGuid(), 0, 0)] };
        Assert.Throws<InvalidDataException>(() => GridSimulationEngine.Restore(state with {
            Social = state.Social with { Residents = [resident, state.Social.Residents[1]] } }));
    }
}
