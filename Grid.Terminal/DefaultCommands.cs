using System.Globalization;
using Grid.Simulation;

namespace Grid.Terminal;

public static class DefaultCommands
{
    public static CommandDispatcher Create(GridSimulationEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        var dispatcher = new CommandDispatcher();
        void Add(string name, string description, Func<CommandResult> execute) =>
            dispatcher.Register(new(name, name, description, args =>
            {
                if (args.Count != 0) return new($"Usage: {name}");
                return execute();
            }));

        Add("help", "Display the command reference.", () => new(TerminalPresentation.Help(dispatcher.Commands)));
        Add("clear", "Clear the visible terminal without changing GRID.", () => new("", ClearRequested: true));
        Add("status", "Show current world and clock state.", () =>
        {
            var state = engine.GetSnapshot();
            return new($"State: {(state.IsRunning ? "Running" : "Paused")}{Environment.NewLine}" +
                $"Tick: {state.TickCount} | Simulation time: {state.ElapsedSimulationTime:c} | Tick duration: {state.TickDuration:c}{Environment.NewLine}" +
                $"Districts: {state.Districts.Count} | Programs: {state.Programs.Count}{Environment.NewLine}Application discovery: {state.ApplicationDiscoveryStatus}");
        });
        dispatcher.Register(new("programs", "programs [ids] [-active|-possible|-sleeping]", "List residents, running applications, known applications or sleeping/background applications.", args =>
            ProgramListCommand.Execute(engine.GetSnapshot(), args)));
        dispatcher.Register(new("districts", "districts [ids]", "List district populations; ids shows full identifiers.", args =>
        {
            if (args.Count > 1 || args.Count == 1 && !args[0].Equals("ids", StringComparison.OrdinalIgnoreCase)) return new("Usage: districts [ids]");
            var state = engine.GetSnapshot();
            return new(args.Count == 1 ? TerminalTable.Render(["District", "ID"], state.Districts.Select(d => new[] { d.Name, d.Id.ToString() })) :
                TerminalTable.Render(["District", "Population"], state.Districts.Select(d => new[] { d.Name, d.ProgramIds.Count.ToString() })) +
                "\nDetails: inspect district <name> | Full identifiers: districts ids");
        }));
        dispatcher.Register(new("inspect", "inspect <program|district> <id|name>",
            "Inspect a citizen or district (names may contain spaces).", args =>
        {
            if (args.Count < 2 || !(args[0].Equals("program", StringComparison.OrdinalIgnoreCase) ||
                                    args[0].Equals("district", StringComparison.OrdinalIgnoreCase)))
                return new("Usage: inspect <program|district> <id|name>");
            var query = string.Join(" ", args.Skip(1));
            var state = engine.GetSnapshot();
            bool Matches(Guid id, string name) => Guid.TryParse(query, out var requested)
                ? id == requested : name.Equals(query, StringComparison.OrdinalIgnoreCase);
            if (args[0].Equals("program", StringComparison.OrdinalIgnoreCase))
            {
                var matches = state.Programs.Where(p => Matches(p.Id, p.Name)).ToArray();
                if (matches.Length != 1) return LookupFailure(matches.Length, query);
                var p = matches[0];
                return new($"Program: {p.Name}{Environment.NewLine}ID: {p.Id}{Environment.NewLine}" +
                    $"{ResidentFormatting.Details(p)}{Environment.NewLine}Occupation: {p.Occupation}{Environment.NewLine}Status: {ResidentFormatting.Status(p)}{Environment.NewLine}" +
                    $"Energy: {ResidentFormatting.Energy(p)}{Environment.NewLine}District: {state.Districts.Single(d => d.Id == p.DistrictId).Name} ({p.DistrictId})");
            }
            var districts = state.Districts.Where(d => Matches(d.Id, d.Name)).ToArray();
            if (districts.Length != 1) return LookupFailure(districts.Length, query);
            var district = districts[0];
            var resources = district.Resources.Count == 0 ? "none" :
                string.Join(", ", district.Resources.OrderBy(r => r.Key).Select(r => $"{r.Key}: {Number(r.Value)}"));
            var residents = state.Programs.Where(p => p.DistrictId == district.Id).Select(p => $"{p.Name} ({p.Id})");
            return new($"District: {district.Name}{Environment.NewLine}ID: {district.Id}{Environment.NewLine}" +
                $"Description: {district.Description}{Environment.NewLine}Population: {district.ProgramIds.Count}{Environment.NewLine}" +
                $"Resources: {resources}{Environment.NewLine}Residents: {string.Join(", ", residents)}");
        }));
        Add("start", "Start or resume automatic simulation.", () => { engine.Start(); return new("Simulation running."); });
        Add("pause", "Pause automatic simulation.", () => { engine.Pause(); return new("Simulation paused."); });
        Add("live", "Live status and recent events; Q/Esc returns to commands.", () => new("", LiveViewRequested: true));
        CommandResult Step()
        {
            var tick = engine.Step();
            return new($"Advanced to tick {tick.Tick} | Simulation time: {tick.SimulationTime:c} | Energy changes: {tick.Events.OfType<ProgramEnergyChanged>().Count()}");
        }
        Add("step", "Advance one tick while paused.", Step);
        Add("tick", "Alias for step; advance one tick while paused.", Step);
        Add("exit", "Stop the simulation and exit.", () => new("Stopping GRID.", true));
        return dispatcher;
    }

    private static string Number(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static CommandResult LookupFailure(int count, string query) => new(count == 0
        ? $"No match for '{query}'." : $"Multiple matches for '{query}'. Use the full ID.");
}
