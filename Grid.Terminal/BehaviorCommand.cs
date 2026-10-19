using Grid.Simulation;
namespace Grid.Terminal;

public static class BehaviorCommand
{
    public static void Register(CommandDispatcher dispatcher, GridSimulationEngine engine) =>
        dispatcher.Register(new("behaviors", "behaviors", "Show last-tick needs, goals and simulated work progress.", args =>
        {
            if (args.Count != 0) return new("Usage: behaviors");
            var state = engine.GetSnapshot();
            var residents = state.Programs.Where(p => p.Behavior is not null).ToArray();
            return new($"Behavior at tick {state.TickCount}; simulated work, not OS task execution.\n" +
                TerminalTable.Render(["Name", "Action", "Need", "Priority", "Units", "Goal"], residents.Select(p => new[] {
                    p.Name, p.Behavior!.Action.ToString(), p.Behavior.Need, p.Behavior.Priority.ToString(), p.Behavior.CompletedUnits.ToString(), p.Behavior.Goal })) +
                "\nUse inspect program <name> for full resident details.");
        }));
}
