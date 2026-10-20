using Grid.Core;
using Grid.Simulation.Behavior;
namespace Grid.Simulation.Social;

public enum SocialPreference { Cooperative, Competitive, Reserved }
public enum SocialGoal { SeekPartner, Observe, Share, Compete, WaitForCivicResources }
public enum SocialOutcome { Cooperated, WonContest, LostContest, ResourcesUnavailable }
public sealed record Relationship(Guid PeerId, int Affinity, long LastInteractionTick);
public sealed record SocialMemory(long Tick, Guid PeerId, Guid DistrictId, SocialOutcome Outcome);
public sealed record OccupationMemory(long Tick, RoutineOccupation? Occupation);
public sealed record SocialResident(Guid ProgramId, SocialPreference Preference, SocialGoal Goal, int CivicCredits,
    IReadOnlyList<Relationship> Relationships, IReadOnlyList<SocialMemory> Memories, IReadOnlyList<OccupationMemory> Occupations);
public sealed record CivicPool(Guid DistrictId, int Capacity, int Available, int RecoveryPerRound);
public sealed record SocialState(long Round, long ProgressTicks, IReadOnlyList<SocialResident> Residents, IReadOnlyList<CivicPool> Pools)
{
    public const long RoundTicks = TimeSpan.TicksPerSecond * 10;
    public static SocialState Empty => new(0, 0, [], []);
    public static SocialState Copy(SocialState state) => state with {
        Residents = Array.AsReadOnly(state.Residents.Select(r => r with {
            Relationships = Array.AsReadOnly(r.Relationships.ToArray()), Memories = Array.AsReadOnly(r.Memories.ToArray()),
            Occupations = Array.AsReadOnly(r.Occupations.ToArray()) }).ToArray()),
        Pools = Array.AsReadOnly(state.Pools.ToArray()) };

    public static void Validate(GridSimulationSnapshot snapshot)
    {
        var state = snapshot.Social;
        if (state is null || state.Residents is null || state.Pools is null || state.Round < 0 || state.Round > snapshot.TickCount ||
            state.ProgressTicks < 0 || state.ProgressTicks >= RoundTicks || state.Residents.Count > 128 || state.Pools.Count > 256 ||
            state.Residents.Any(r => r is null) || state.Pools.Any(p => p is null))
            throw new InvalidDataException("Invalid social state or limits.");
        var programs = snapshot.Programs.ToDictionary(p => p.Id);
        var residents = state.Residents.Select(r => r.ProgramId).ToHashSet();
        var districts = snapshot.Districts.Select(d => d.Id).ToHashSet();
        var pools = state.Pools.Select(p => p.DistrictId).ToHashSet();
        if (residents.Count != state.Residents.Count || pools.Count != state.Pools.Count ||
            state.Pools.Any(p => !districts.Contains(p.DistrictId) || p.Capacity is < 1 or > 64 ||
                p.Available < 0 || p.Available > p.Capacity || p.RecoveryPerRound < 0 || p.RecoveryPerRound > p.Capacity))
            throw new InvalidDataException("Invalid civic resource pool.");
        foreach (var r in state.Residents)
        {
            if (!programs.TryGetValue(r.ProgramId, out var p) || p.Kind is not (ResidentKind.Agent or ResidentKind.Special) ||
                !pools.Contains(p.DistrictId) || !Enum.IsDefined(r.Preference) || !Enum.IsDefined(r.Goal) || r.CivicCredits is < 0 or > 100 ||
                r.Relationships is null || r.Memories is null || r.Occupations is null ||
                r.Relationships.Count > 16 || r.Memories.Count > 16 || r.Occupations.Count is < 1 or > 8 ||
                r.Relationships.Any(x => x is null) || r.Memories.Any(x => x is null) || r.Occupations.Any(x => x is null))
                throw new InvalidDataException("Invalid social resident.");
            bool Peer(Guid id) => id != r.ProgramId && residents.Contains(id);
            if (r.Relationships.Select(x => x.PeerId).Distinct().Count() != r.Relationships.Count ||
                r.Relationships.Any(x => !Peer(x.PeerId) || x.Affinity is < -100 or > 100 || x.LastInteractionTick < 0 || x.LastInteractionTick > snapshot.TickCount) ||
                r.Memories.Any(x => !Peer(x.PeerId) || !districts.Contains(x.DistrictId) || !Enum.IsDefined(x.Outcome) || x.Tick < 0 || x.Tick > snapshot.TickCount) ||
                r.Occupations.Any(x => x.Tick < 0 || x.Tick > snapshot.TickCount || x.Occupation is { } occupation && !Enum.IsDefined(occupation)) ||
                !r.Memories.Select(x => x.Tick).SequenceEqual(r.Memories.Select(x => x.Tick).Order()) ||
                !r.Occupations.Select(x => x.Tick).SequenceEqual(r.Occupations.Select(x => x.Tick).Order()))
                throw new InvalidDataException("Invalid social relationships or memories.");
        }
    }
}
