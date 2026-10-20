using Grid.Core;
using Grid.Simulation.Behavior;
using Grid.Simulation.Social;
namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    private SocialState social = SocialState.Empty;

    /// <summary>Trusted opt-in configuration for persistent residents, not a terminal privilege or host capability.</summary>
    public void ConfigureSocial(Guid programId, SocialPreference preference)
    {
        lock (gate)
        {
            var program = world.GetProgram(programId);
            if (program.Kind is not (ResidentKind.Agent or ResidentKind.Special) || !Enum.IsDefined(preference))
                throw new ArgumentException("Social simulation requires an Agent/Special resident and a valid preference.");
            var previous = social.Residents.FirstOrDefault(r => r.ProgramId == programId);
            if (previous is null && social.Residents.Count >= 128 || world.Districts.Count > 256)
                throw new InvalidOperationException("Social simulation capacity exceeded.");
            var next = previous is null
                ? new SocialResident(programId, preference, SocialGoal.SeekPartner, 0, [], [],
                    Array.AsReadOnly(new[] { new OccupationMemory(clock.TickCount, BehaviorOf(program)?.Routine?.Occupation) }))
                : previous with { Preference = preference, Goal = SocialGoal.SeekPartner };
            var staged = social with {
                Residents = Array.AsReadOnly(social.Residents.Where(r => r.ProgramId != programId).Append(next).OrderBy(r => r.ProgramId).ToArray()),
                Pools = social.Pools.Count != 0 ? social.Pools :
                    Array.AsReadOnly(world.Districts.Keys.Order().Select(id => new CivicPool(id, 4, 4, 1)).ToArray()) };
            SocialState.Validate(GetSnapshot() with { Social = staged });
            var revision = checked(program.Revision + 1);
            social = staged;
            program.Commit(program.Values, revision);
            RecordEvent("SocialConfigured", programId, program.Name, $"Opted into simulated civic interaction: {preference}.");
        }
    }

    public void ConfigureCivicPool(Guid districtId, int capacity, int recoveryPerRound)
    {
        lock (gate)
        {
            _ = world.GetDistrict(districtId);
            if (capacity is < 1 or > 64 || recoveryPerRound < 0 || recoveryPerRound > capacity || social.Residents.Count == 0)
                throw new ArgumentException("Configure social residents first; capacity must be 1..64 and recovery 0..capacity.");
            var prior = social.Pools.Single(p => p.DistrictId == districtId);
            social = social with { Pools = Array.AsReadOnly(social.Pools.Select(p => p.DistrictId == districtId
                ? p with { Capacity = capacity, Available = Math.Min(p.Available, capacity), RecoveryPerRound = recoveryPerRound } : p).ToArray()) };
            RecordEvent("CivicPoolConfigured", districtId, world.GetDistrict(districtId).Name,
                $"Simulated civic tokens: capacity {prior.Capacity} -> {capacity}, recovery {recoveryPerRound}/round; unrelated to host resources.");
        }
    }

    private SocialState WithOccupation(Guid id, RoutineOccupation occupation)
    {
        var resident = social.Residents.FirstOrDefault(r => r.ProgramId == id);
        if (resident is null || resident.Occupations[^1].Occupation == occupation) return social;
        var changed = resident with { Occupations = Array.AsReadOnly(resident.Occupations
            .Append(new(clock.TickCount, occupation)).TakeLast(8).ToArray()) };
        return social with { Residents = Array.AsReadOnly(social.Residents.Select(r => r.ProgramId == id ? changed : r).ToArray()) };
    }

    private SocialUpdate StageSocial(long nextTick, TimeSpan nextTime,
        List<(GridProgram Program, ResidentBehavior Before, ResidentBehavior After)> behaviorChanges)
    {
        if (social.Residents.Count == 0) return new(social, []);
        var projected = GetSnapshot();
        var changes = behaviorChanges.ToDictionary(c => c.Program.Id, c => c.After);
        // Policy reads post-behavior locations/actions, but cannot mutate the authoritative world.
        projected = projected with { TickCount = nextTick, ElapsedSimulationTime = nextTime,
            Programs = projected.Programs.Select(p => changes.TryGetValue(p.Id, out var after)
                ? p with { Behavior = after, DistrictId = after.Travel?.Route[0] ?? p.DistrictId } : p).ToArray() };
        var result = SocialPolicy.Next(projected, TickDuration);
        var changed = ChangedSocialResidents(result.State);
        foreach (var id in changed)
            _ = checked(world.GetProgram(id).Revision + (changes.ContainsKey(id) ? 2 : 1));
        return result;
    }

    private Guid[] ChangedSocialResidents(SocialState next)
    {
        var prior = social.Residents.ToDictionary(r => r.ProgramId);
        return next.Residents.Where(r => !Equals(prior.GetValueOrDefault(r.ProgramId), r)).Select(r => r.ProgramId).ToArray();
    }

    private void CommitSocial(SocialUpdate update)
    {
        var changed = ChangedSocialResidents(update.State);
        var oldPools = social.Pools.ToDictionary(p => p.DistrictId);
        social = update.State;
        foreach (var id in changed)
        {
            var program = world.GetProgram(id);
            program.Commit(program.Values, program.Revision + 1);
        }
        foreach (var interaction in update.Interactions)
            RecordEvent("SocialInteraction", interaction.First, world.GetProgram(interaction.First).Name,
                $"Simulated civic interaction with {world.GetProgram(interaction.Second).Name}: {interaction.FirstOutcome} / {interaction.SecondOutcome}.",
                System.Text.Json.JsonSerializer.Serialize(interaction));
        foreach (var pool in social.Pools)
            if (oldPools.TryGetValue(pool.DistrictId, out var old) && pool.Available != old.Available)
                RecordEvent("CivicResources", pool.DistrictId, world.GetDistrict(pool.DistrictId).Name,
                    $"Simulated civic tokens: {old.Available} -> {pool.Available} of {pool.Capacity}; not host capacity.");
    }
}
