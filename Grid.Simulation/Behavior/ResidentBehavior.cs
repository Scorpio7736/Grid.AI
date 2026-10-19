using Grid.Core;
using Grid.Simulation.Resources;
namespace Grid.Simulation.Behavior;

public enum ResidentAction { Unknown, Observing, Idle, Working, Resting, WaitingForResources, OffDuty }
public enum RoutineOccupation { Administrator, Archivist, Researcher, Technician }
public sealed record WorkRoutine(RoutineOccupation Occupation, long WorkBurstTicks, long RestTicks,
    long UnitTicks, int ShiftStartHour, int ShiftHours, IReadOnlyList<ResourceRequirement> Requirements);
public sealed record ResidentBehavior(ResidentAction Action, string Goal, string Need, int Priority,
    long WorkTicks, long RestRemainingTicks, long CompletedUnits, WorkRoutine? Routine)
{
    public static ResidentBehavior Initial => new(ResidentAction.Idle, "Await assigned work", "Assignment", 0, 0, 0, 0, null);
    public static WorkRoutine Copy(WorkRoutine r) => r with { Requirements = Array.AsReadOnly(r.Requirements.ToArray()) };
    public static void Validate(ResidentBehavior b, ResidentKind kind)
    {
        if (!Enum.IsDefined(b.Action) || string.IsNullOrWhiteSpace(b.Goal) || b.Goal.Length > 120 || b.Goal.Any(char.IsControl) ||
            string.IsNullOrWhiteSpace(b.Need) || b.Need.Length > 80 || b.Need.Any(char.IsControl) || b.Priority is < 0 or > 100 ||
            b.WorkTicks < 0 || b.RestRemainingTicks < 0 || b.CompletedUnits < 0)
            throw new InvalidDataException("Invalid resident behavior.");
        if (b.Routine is not { } r)
        {
            if (b.WorkTicks != 0 || b.RestRemainingTicks != 0 || b.CompletedUnits != 0 ||
                b.Action is not (ResidentAction.Idle or ResidentAction.Observing or ResidentAction.Unknown))
                throw new InvalidDataException("Unassigned behavior cannot contain routine progress.");
            return;
        }
        if (kind is not (ResidentKind.Agent or ResidentKind.Special) || !Enum.IsDefined(r.Occupation) ||
            r.WorkBurstTicks < TimeSpan.TicksPerSecond || r.WorkBurstTicks > TimeSpan.TicksPerDay ||
            r.RestTicks < TimeSpan.TicksPerSecond || r.RestTicks > TimeSpan.TicksPerDay || r.UnitTicks < TimeSpan.TicksPerSecond ||
            r.UnitTicks > r.WorkBurstTicks || r.WorkBurstTicks % r.UnitTicks != 0 || r.ShiftStartHour is < 0 or > 23 || r.ShiftHours is < 1 or > 24 ||
            r.Requirements is null || r.Requirements.Count > 32 || b.WorkTicks > r.WorkBurstTicks || b.RestRemainingTicks > r.RestTicks)
            throw new InvalidDataException("Invalid work routine or progress.");
        if (b.Action == ResidentAction.Observing || b.RestRemainingTicks > 0 && b.WorkTicks != r.WorkBurstTicks ||
            r.Requirements.Any(q => q is null || q.Scope is null || q.Provenance is null || q.Scope.Length > 256 || q.Provenance.Length > 256))
            throw new InvalidDataException("Inconsistent routine state or unbounded requirement provenance.");
        new ResourceConditionPolicy().Evaluate([], r.Requirements, DateTimeOffset.UtcNow);
    }
}

/// <summary>Pure, deterministic priority state machine; only simulated work, never host task execution.</summary>
public static class ResidentBehaviorPolicy
{
    public static ResidentBehavior Next(ResidentKind kind, ResidentBehavior current, bool applicationFresh,
        double? condition, TimeSpan simulationTime, TimeSpan step)
    {
        ResidentBehavior.Validate(current, kind);
        if (step <= TimeSpan.Zero || simulationTime < TimeSpan.Zero || condition is { } value && (!double.IsFinite(value) || value is < 0 or > 100))
            throw new ArgumentOutOfRangeException(nameof(step), "Invalid step, simulation time or condition.");
        if (kind == ResidentKind.Application)
            return ResidentBehavior.Initial with { Action = applicationFresh ? ResidentAction.Observing : ResidentAction.Unknown,
                Goal = applicationFresh ? "Reflect observed application availability" : "Await fresh application observation",
                Need = "Observation", Priority = applicationFresh ? 10 : 100 };
        if (current.Routine is not { } r) return ResidentBehavior.Initial;
        var shiftOffset = ((int)(simulationTime.Ticks / TimeSpan.TicksPerHour % 24) - r.ShiftStartHour + 24) % 24;
        if (shiftOffset >= r.ShiftHours)
            return current with { Action = ResidentAction.OffDuty, Goal = "Await simulation-time shift", Need = "Schedule", Priority = 90 };
        if (condition is null)
            return current with { Action = ResidentAction.Unknown, Goal = "Await sourced resource observations", Need = "Resource evidence", Priority = 100 };
        // Hysteresis: entering/re-entering work needs 60; continuing work needs at least 40.
        if (condition < (current.Action == ResidentAction.Working ? 40 : 60))
            return current with { Action = ResidentAction.WaitingForResources, Goal = "Wait for sufficient headroom", Need = "Resources", Priority = 100 };
        if (current.RestRemainingTicks > 0)
            return current with { Action = ResidentAction.Resting, Goal = "Scheduled simulated rest", Need = "Rest", Priority = 80,
                RestRemainingTicks = Math.Max(0, current.RestRemainingTicks - Math.Min(step.Ticks, current.RestRemainingTicks)) };
        var prior = current.WorkTicks == r.WorkBurstTicks ? 0 : current.WorkTicks;
        var work = prior + Math.Min(step.Ticks, r.WorkBurstTicks - prior);
        var completed = checked(current.CompletedUnits + work / r.UnitTicks - prior / r.UnitTicks);
        var goal = r.Occupation switch
        {
            RoutineOccupation.Administrator => "Review simulated civic records",
            RoutineOccupation.Archivist => "Index simulated archive records",
            RoutineOccupation.Researcher => "Analyze simulated research records",
            _ => "Inspect simulated maintenance records"
        };
        return current with { Action = ResidentAction.Working, Goal = goal, Need = "Assigned work", Priority = 50,
            WorkTicks = work, CompletedUnits = completed, RestRemainingTicks = work == r.WorkBurstTicks ? r.RestTicks : 0 };
    }
}
