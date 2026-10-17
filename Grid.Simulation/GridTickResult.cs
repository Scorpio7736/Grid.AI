namespace Grid.Simulation;

public abstract record GridSimulationEvent(long Tick, TimeSpan SimulationTime);

public sealed record ProgramEnergyChanged(
    long Tick, TimeSpan SimulationTime, Guid ProgramId, float PreviousEnergy, float Energy)
    : GridSimulationEvent(Tick, SimulationTime);

public sealed record SimulationTickCompleted(
    long Tick, TimeSpan SimulationTime, int ProgramCount, int DistrictCount)
    : GridSimulationEvent(Tick, SimulationTime);

/// <summary>An immutable snapshot of events from one committed tick; the engine retains only the latest tick.</summary>
public sealed record GridTickResult(
    long Tick, TimeSpan SimulationTime, IReadOnlyList<GridSimulationEvent> Events);
