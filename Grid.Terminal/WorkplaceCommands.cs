using Grid.Core;
using Grid.Simulation;
using Grid.Simulation.Behavior;
namespace Grid.Terminal;

public static class WorkplaceCommands
{
    public static void Register(CommandDispatcher dispatcher, GridSimulationEngine engine)
    {
        dispatcher.Register(new("jobs", "jobs", "List civic workstations, occupations and assignments.", args =>
        {
            if (args.Count != 0) return new("Usage: jobs");
            var state = engine.GetSnapshot();
            return new("Simulated civic workplaces; assignments do not authorize OS tasks.\n" +
                TerminalTable.Render(["Station", "Occupation", "District", "Assigned", "ID"],
                    WorkplacePolicy.Stations(state).Select(s => new[] { s.Name, s.Occupation.ToString(),
                        state.Districts.Single(d => d.Id == s.DistrictId).Name,
                        state.Programs.SingleOrDefault(p => p.Behavior?.Travel?.WorkstationId == s.Id)?.Name ?? "Vacant", s.Id.ToString() })));
        }));
        dispatcher.Register(new("activity", "activity", "Show observed application availability and measured working sets.", args =>
        {
            if (args.Count != 0) return new("Usage: activity");
            var state = engine.GetSnapshot();
            return new("Measured host activity; shared memory may overlap. CPU/GPU/I/O attribution and requirements remain unknown.\n" +
                TerminalTable.Render(["Application", "Availability", "Working set bytes", "Source"],
                    state.Programs.Where(p => p.Kind == ResidentKind.Application).Select(p => new[] { p.Name,
                        ResidentFormatting.Status(p),
                        p.Application is { Confirmed: true, WorkingSetBytes: { } bytes } ? bytes.ToString(System.Globalization.CultureInfo.InvariantCulture) : "unknown",
                        p.Application?.Source ?? "unknown" })));
        }));
    }
}
