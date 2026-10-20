using Grid.Core;
using Grid.Simulation;
namespace Grid.Terminal;

public static class ProgramListCommand
{
    public static CommandResult Execute(GridSimulationSnapshot state, IReadOnlyList<string> args)
    {
        const string usage = "Usage: programs [ids] [-active|-possible|-sleeping]";
        var tokens = args.Select(a => a.ToLowerInvariant()).ToArray();
        var filters = tokens.Where(a => a != "ids").ToArray();
        if (tokens.Distinct().Count() != tokens.Length || filters.Length > 1 ||
            filters.Any(a => a is not ("-active" or "-possible" or "-sleeping"))) return new(usage);
        var ids = tokens.Contains("ids");
        var filter = filters.SingleOrDefault();
        var programs = state.Programs.Where(p => filter switch
        {
            "-active" => p.Kind == ResidentKind.Application && p.Application is { Confirmed: true, Activity: ApplicationActivity.Running },
            "-sleeping" => p.Kind == ResidentKind.Application && p.Application is { Confirmed: true, Activity: ApplicationActivity.Sleeping or ApplicationActivity.OnCall },
            "-possible" => p.Kind == ResidentKind.Application,
            _ => true
        }).ToArray();
        if (filter == "-possible")
        {
            var current = programs.Where(p => p.Application is not null).ToDictionary(p => p.Application!.ApplicationKey);
            var catalog = state.ApplicationCatalog.ToDictionary(a => a.Key);
            foreach (var p in current.Values)
                catalog.TryAdd(p.Application!.ApplicationKey, new(p.Application.ApplicationKey, p.Name, p.Application.Source, p.Application.ObservedAtUtc));
            var rows = catalog.Values.OrderBy(a => a.Name).Select(a =>
            {
                var p = current.GetValueOrDefault(a.Key);
                return ids ? new[] { a.Name, a.Key, p?.Id.ToString() ?? "No current resident" } :
                    new[] { a.Name, p is null ? "Historical / unknown" : ResidentFormatting.Status(p), a.LastSeenUtc.ToString("O"), a.Source };
            });
            return new(TerminalTable.Render(ids ? ["Name", "Catalog key", "Resident ID"] : ["Name", "State", "Last catalog observation UTC", "Source"], rows) +
                "\nKnown installed/observed catalog; historical entries do not imply current installation or availability.");
        }
        return new(ids ? TerminalTable.Render(["Name", "ID"], programs.Select(p => new[] { p.Name, p.Id.ToString() })) :
            TerminalTable.Render(["Name", "Type", "State", "Energy", "District"], programs.Select(p => new[] {
                p.Name, p.Kind.ToString(), ResidentFormatting.Status(p), ResidentFormatting.Energy(p), state.Districts.Single(d => d.Id == p.DistrictId).Name })) +
            "\nDetails: inspect program <name> | Full identifiers: programs ids");
    }
}
