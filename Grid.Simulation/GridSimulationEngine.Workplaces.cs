using Grid.Core;
using Grid.Simulation.Behavior;
namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    /// <summary>Trusted configuration API. Assigns the nearest reachable compatible free workstation.</summary>
    public Workstation AssignWorkplace(Guid programId, Guid? workstationId = null)
    {
        lock (gate)
        {
            var program = world.GetProgram(programId);
            var before = BehaviorOf(program);
            if (program.Kind is not (ResidentKind.Agent or ResidentKind.Special) || before?.Routine is not { } routine)
                throw new InvalidOperationException("Workstations require an explicitly configured Agent or Special routine.");
            var state = GetSnapshot();
            var occupied = state.Programs.Where(p => p.Id != programId && p.Behavior?.Travel is not null)
                .Select(p => p.Behavior!.Travel!.WorkstationId).ToHashSet();
            var options = WorkplacePolicy.Stations(state)
                .Where(s => s.Occupation == routine.Occupation && !occupied.Contains(s.Id) && (workstationId is null || s.Id == workstationId))
                .Select(s => (Station: s, Route: WorkplacePolicy.Route(state, program.CurrentDistrictId, s.DistrictId)))
                .Where(s => s.Route is not null).OrderBy(s => s.Route!.Count).ThenBy(s => s.Station.Id).ToArray();
            if (options.Length == 0) throw new InvalidOperationException("No reachable, compatible, unoccupied workstation is available.");
            var chosen = options[0];
            var revision = checked(program.Revision + 1);
            behaviors[programId] = before with {
                Action = ResidentAction.Idle, Goal = "Reach assigned workstation", Need = "Workplace", Priority = 70,
                Travel = new(chosen.Station.Id, chosen.Route!, chosen.Route!.Count > 1 ? WorkplacePolicy.EdgeTicks : 0) };
            program.Commit(program.Values with { Status = GridProgramStatus.Idle }, revision);
            RecordEvent("WorkplaceAssigned", programId, program.Name,
                $"Assigned {chosen.Station.Name}; route has {chosen.Route.Count - 1} road edges.",
                System.Text.Json.JsonSerializer.Serialize(new { chosen.Station.Id, chosen.Station.DistrictId, route = chosen.Route }));
            return chosen.Station;
        }
    }

    public void ReleaseWorkplace(Guid programId)
    {
        lock (gate)
        {
            var program = world.GetProgram(programId);
            var before = BehaviorOf(program);
            if (before?.Travel is null) return;
            var revision = checked(program.Revision + 1);
            behaviors[programId] = before with { Travel = null, Action = ResidentAction.Idle,
                Goal = "Workplace assignment released", Need = "Assignment", Priority = 0 };
            program.Commit(program.Values with { Status = GridProgramStatus.Idle }, revision);
            RecordEvent("WorkplaceReleased", programId, program.Name, "Released workstation; remaining travel cancelled.");
        }
    }
}
