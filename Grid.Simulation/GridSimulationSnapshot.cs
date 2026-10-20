using Grid.Core;
namespace Grid.Simulation;

public sealed record GridProgramSnapshot(Guid Id, string Name, string Occupation,
    float Energy, Guid DistrictId, GridProgramStatus Status, long Revision)
{
    public ResidentKind Kind { get; init; } = ResidentKind.Legacy;
    public ApplicationBinding? Application { get; init; }
    public Resources.ResourceCondition? Condition { get; init; }
    public Behavior.ResidentBehavior? Behavior { get; init; }
}
public sealed record GridDistrictSnapshot(Guid Id, string Name, string Description,
    IReadOnlyList<Guid> ProgramIds, IReadOnlyDictionary<string, float> Resources);
public sealed record KnownApplication(string Key, string Name, string Source, DateTimeOffset LastSeenUtc);
public sealed record GridSimulationSnapshot(long TickCount, TimeSpan ElapsedSimulationTime,
    TimeSpan TickDuration, bool IsRunning, IReadOnlyList<GridProgramSnapshot> Programs,
    IReadOnlyList<GridDistrictSnapshot> Districts)
{
    public Grid.Communication.LocalIdentityState Identities { get; init; } = Grid.Communication.LocalIdentityState.Empty;
    public Grid.Communication.CommunicationState Communications { get; init; } = Grid.Communication.CommunicationState.Empty;
    public Social.SocialState Social { get; init; } = global::Grid.Simulation.Social.SocialState.Empty;
    public IReadOnlyList<KnownApplication> ApplicationCatalog { get; init; } = [];
    public Guid WorldId { get; init; }
    public string ApplicationDiscoveryStatus { get; init; } = "Not sampled";
    public Grid.Core.Generation.CityBlueprint? City { get; init; }
}
