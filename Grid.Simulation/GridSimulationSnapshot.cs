using Grid.Core;
namespace Grid.Simulation;

public sealed record GridProgramSnapshot(Guid Id, string Name, string Occupation,
    float Energy, Guid DistrictId, GridProgramStatus Status, long Revision);
public sealed record GridDistrictSnapshot(Guid Id, string Name, string Description,
    IReadOnlyList<Guid> ProgramIds, IReadOnlyDictionary<string, float> Resources);
public sealed record GridSimulationSnapshot(long TickCount, TimeSpan ElapsedSimulationTime,
    TimeSpan TickDuration, bool IsRunning, IReadOnlyList<GridProgramSnapshot> Programs,
    IReadOnlyList<GridDistrictSnapshot> Districts)
{
    public Guid WorldId { get; init; }
}
