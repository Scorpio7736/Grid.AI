using Grid.Simulation.Behavior;
namespace Grid.Simulation.Social;

public sealed record SocialInteraction(Guid First, Guid Second, Guid District, SocialOutcome FirstOutcome, SocialOutcome SecondOutcome);
public sealed record SocialUpdate(SocialState State, IReadOnlyList<SocialInteraction> Interactions);

/// <summary>Bounded deterministic civic interactions, separate from host resources and real communications.</summary>
public static class SocialPolicy
{
    public static SocialUpdate Next(GridSimulationSnapshot projected, TimeSpan step)
    {
        SocialState.Validate(projected);
        if (step <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(step));
        var state = projected.Social;
        if (state.Residents.Count == 0) return new(state, []);
        var progress = state.ProgressTicks + Math.Min(step.Ticks, SocialState.RoundTicks - state.ProgressTicks);
        if (progress < SocialState.RoundTicks) return new(state with { ProgressTicks = progress }, []);
        var residents = state.Residents.ToDictionary(r => r.ProgramId);
        var programs = projected.Programs.ToDictionary(p => p.Id);
        var events = new List<SocialInteraction>();
        var pools = new List<CivicPool>();
        foreach (var pool in state.Pools.OrderBy(p => p.DistrictId))
        {
            var available = Math.Min(pool.Capacity, pool.Available + pool.RecoveryPerRound);
            var candidates = state.Residents.Where(r => programs[r.ProgramId].DistrictId == pool.DistrictId &&
                (programs[r.ProgramId].Behavior?.Action ?? ResidentAction.Idle) is ResidentAction.Idle or ResidentAction.Resting or ResidentAction.OffDuty &&
                programs[r.ProgramId].Behavior?.Travel is not { Route.Count: > 1 }).OrderBy(r => r.ProgramId).ToArray();
            var offset = candidates.Length == 0 ? 0 : (int)(state.Round % candidates.Length);
            var ordered = candidates.Skip(offset).Concat(candidates.Take(offset)).ToArray();
            var used = new HashSet<Guid>();
            foreach (var candidate in ordered)
            {
                if (!used.Add(candidate.ProgramId)) continue;
                var first = residents[candidate.ProgramId];
                if (first.Preference == SocialPreference.Reserved)
                { residents[first.ProgramId] = first with { Goal = SocialGoal.Observe }; continue; }
                // Preference and remembered affinity influence partner choice; reserved programs never consent automatically.
                var second = ordered.Where(r => !used.Contains(r.ProgramId) && r.Preference == first.Preference)
                    .OrderBy(r => first.Preference == SocialPreference.Cooperative
                        ? -first.Relationships.FirstOrDefault(x => x.PeerId == r.ProgramId)?.Affinity ?? 0
                        : first.Relationships.FirstOrDefault(x => x.PeerId == r.ProgramId)?.Affinity ?? 0)
                    .ThenBy(r => r.ProgramId).FirstOrDefault();
                if (second is null)
                { residents[first.ProgramId] = first with { Goal = SocialGoal.SeekPartner }; continue; }
                used.Add(second.ProgramId);
                SocialOutcome a, b;
                if (available == 0) a = b = SocialOutcome.ResourcesUnavailable;
                else
                {
                    available--;
                    a = first.Preference == SocialPreference.Cooperative ? SocialOutcome.Cooperated : SocialOutcome.WonContest;
                    b = first.Preference == SocialPreference.Cooperative ? SocialOutcome.Cooperated : SocialOutcome.LostContest;
                }
                residents[first.ProgramId] = Experience(first, second.ProgramId, pool.DistrictId, a, projected.TickCount);
                residents[second.ProgramId] = Experience(residents[second.ProgramId], first.ProgramId, pool.DistrictId, b, projected.TickCount);
                events.Add(new(first.ProgramId, second.ProgramId, pool.DistrictId, a, b));
            }
            pools.Add(pool with { Available = available });
        }
        return new(new(checked(state.Round + 1), 0, Array.AsReadOnly(residents.Values.OrderBy(r => r.ProgramId).ToArray()),
            pools.AsReadOnly()), events.AsReadOnly());
    }

    private static SocialResident Experience(SocialResident resident, Guid peer, Guid district, SocialOutcome outcome, long tick)
    {
        var prior = resident.Relationships.FirstOrDefault(r => r.PeerId == peer)?.Affinity ?? 0;
        var delta = outcome switch { SocialOutcome.Cooperated => 2, SocialOutcome.ResourcesUnavailable => 0, _ => -1 };
        var relationships = resident.Relationships.Where(r => r.PeerId != peer)
            .Append(new(peer, Math.Clamp(prior + delta, -100, 100), tick))
            .OrderByDescending(r => r.LastInteractionTick).ThenBy(r => r.PeerId).Take(16).ToArray();
        return resident with {
            Goal = outcome == SocialOutcome.ResourcesUnavailable ? SocialGoal.WaitForCivicResources :
                outcome == SocialOutcome.Cooperated ? SocialGoal.Share : SocialGoal.Compete,
            CivicCredits = Math.Min(100, resident.CivicCredits + (outcome is SocialOutcome.Cooperated or SocialOutcome.WonContest ? 1 : 0)),
            Relationships = Array.AsReadOnly(relationships),
            Memories = Array.AsReadOnly(resident.Memories.Append(new(tick, peer, district, outcome)).TakeLast(16).ToArray()) };
    }
}
