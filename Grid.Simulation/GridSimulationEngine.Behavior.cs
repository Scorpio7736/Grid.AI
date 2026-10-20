using Grid.Core;
using Grid.Simulation.Behavior;
namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    private readonly Dictionary<Guid, ResidentBehavior> behaviors = new();
    private ResidentBehavior? BehaviorOf(GridProgram p) => p.Kind == ResidentKind.Legacy ? null :
        behaviors.GetValueOrDefault(p.Id) ?? ResidentBehavior.Initial;

    /// <summary>Trusted service configuration, not authentication or permission to execute OS work.</summary>
    public void ConfigureRoutine(Guid id, WorkRoutine routine)
    {
        var copy = ResidentBehavior.Copy(routine);
        var behavior = ResidentBehavior.Initial with { Routine = copy };
        lock (gate)
        {
            var program = world.GetProgram(id);
            ResidentBehavior.Validate(behavior, program.Kind);
            var revision = checked(program.Revision + 1);
            var nextSocial = WithOccupation(id, copy.Occupation);
            behaviors[id] = behavior;
            social = nextSocial;
            requirements[id] = copy.Requirements.ToArray(); conditions.Remove(id);
            program.Commit(program.Values with { Occupation = copy.Occupation.ToString(), Status = GridProgramStatus.Idle }, revision);
            RecordEvent("RoutineConfigured", id, program.Name, $"Assigned {copy.Occupation} simulated routine; progress reset.");
        }
    }

    private List<(GridProgram Program, ResidentBehavior Before, ResidentBehavior After)> StageBehaviors(TimeSpan nextTime)
    {
        var staged = new List<(GridProgram, ResidentBehavior, ResidentBehavior)>();
        if (behaviors.Values.Any(b => b.Travel is not null)) WorkplacePolicy.ValidateAssignments(GetSnapshot());
        foreach (var p in world.Programs.Values.Where(p => p.Kind != ResidentKind.Legacy).OrderBy(p => p.Id))
        {
            var before = BehaviorOf(p)!;
            var after = ResidentBehaviorPolicy.Next(p.Kind, before, CurrentBinding(p)?.Confirmed == true,
                CurrentCondition(p)?.Score, nextTime, TickDuration);
            if (after != before)
            {
                _ = checked(p.Revision + 1);
                staged.Add((p, before, after));
            }
        }
        return staged;
    }
    private void CommitBehaviors(List<(GridProgram Program, ResidentBehavior Before, ResidentBehavior After)> staged)
    {
        foreach (var (p, before, after) in staged)
        {
            behaviors[p.Id] = after;
            var status = after.Action == ResidentAction.Working ? GridProgramStatus.Working :
                after.Action == ResidentAction.Moving ? GridProgramStatus.Moving : GridProgramStatus.Idle;
            var revision = p.Revision + 1;
            var previous = p.CurrentDistrictId;
            var destination = after.Travel?.Route[0] ?? previous;
            world.CommitProgram(p, p.Values with { Status = status, DistrictId = destination });
            p.Commit(p.Values, revision);
            if (previous != destination)
                RecordEvent("Movement", p.Id, p.Name, $"Work route: {world.GetDistrict(previous).Name} -> {world.GetDistrict(destination).Name}",
                    System.Text.Json.JsonSerializer.Serialize(new { fromDistrictId = previous, toDistrictId = destination, workstationId = after.Travel!.WorkstationId }));
            if (before.Action != after.Action)
                RecordEvent("Behavior", p.Id, p.Name, $"{before.Action} -> {after.Action}: {after.Goal}");
            if (after.CompletedUnits != before.CompletedUnits)
                RecordEvent("WorkCompleted", p.Id, p.Name, $"Simulated {after.Routine!.Occupation} units completed: {after.CompletedUnits - before.CompletedUnits}; total {after.CompletedUnits}.");
        }
    }
}
