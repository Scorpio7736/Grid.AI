using Grid.Simulation;
namespace Grid.Terminal;

public static class BehaviorCommand
{
    public static void Register(CommandDispatcher dispatcher, GridSimulationEngine engine) =>
        dispatcher.Register(new("behaviors", "behaviors", "Show last-tick needs, goals and simulated work progress.", args =>
        {
            if (args.Count != 0) return new("Usage: behaviors");
            var state = engine.GetSnapshot();
            var lines = state.Programs.Where(p => p.Behavior is not null).Select(p =>
                $"{p.Name} ({p.Id}) | {p.Kind} | {p.Behavior!.Action} | Need: {p.Behavior.Need} | Priority: {p.Behavior.Priority} | Goal: {p.Behavior.Goal} | Simulated units: {p.Behavior.CompletedUnits}");
            return new($"Behavior at tick {state.TickCount}; routine work is simulated, not OS task execution.\n" +
                (state.Programs.Any(p => p.Behavior is not null) ? string.Join(Environment.NewLine, lines) : "No non-demo residents."));
        }));
}
