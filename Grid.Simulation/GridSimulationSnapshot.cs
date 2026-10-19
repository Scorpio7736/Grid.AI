using Grid.Core;
namespace Grid.Simulation;

public sealed record GridProgramSnapshot(Guid Id, string Name, string Occupation,
    float Energy, Guid DistrictId, GridProgramStatus Status, long Revision)
{
    public ResidentKind Kind { get; init; } = ResidentKind.Legacy;
    public ApplicationBinding? Application { get; init; }
    public Resources.ResourceCondition? Condition { get; init; }
}
public sealed record GridDistrictSnapshot(Guid Id, string Name, string Description,
    IReadOnlyList<Guid> ProgramIds, IReadOnlyDictionary<string, float> Resources);
public sealed record GridSimulationSnapshot(long TickCount, TimeSpan ElapsedSimulationTime,
    TimeSpan TickDuration, bool IsRunning, IReadOnlyList<GridProgramSnapshot> Programs,
    IReadOnlyList<GridDistrictSnapshot> Districts)
{
    public Guid WorldId { get; init; }
    public string ApplicationDiscoveryStatus { get; init; } = "Not sampled";
    public Grid.Core.Generation.CityBlueprint? City { get; init; }
}
