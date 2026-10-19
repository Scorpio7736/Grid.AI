using System.Globalization;
using Grid.Simulation;

namespace Grid.Terminal;

public static class ResourcesCommand
{
    public static void Register(CommandDispatcher commands, GridSimulationEngine engine) =>
        commands.Register(new("resources", "resources [details]", "Show read-only host measurements, units, sources and freshness.", args =>
        {
            if (args.Count > 1 || args.Count == 1 && !args[0].Equals("details", StringComparison.OrdinalIgnoreCase)) return new("Usage: resources [details]");
            var readings = engine.GetHostResources();
            if (readings.Count == 0) return new("Host telemetry: no observations yet. Application energy: unknown (requirements not configured).");
            var lines = readings.Select(r => $"{r.Metric} [{Safe(r.Scope)}]: " +
                (r.Value is { } value ? value.ToString("0.##", CultureInfo.InvariantCulture) + " " + r.Unit : "unknown") +
                $" | {r.Status} | {r.ObservedAtUtc:O} | {Safe(r.Source)} | {Safe(r.Detail)}");
            return new((args.Count == 1 ? string.Join(Environment.NewLine, lines) : TerminalTable.Render(["Metric", "Scope", "Reading", "State"], readings.Select(r => new[] { r.Metric.ToString(), r.Scope, r.Value is { } v ? v.ToString("0.##", CultureInfo.InvariantCulture) + " " + r.Unit : "unknown", r.Status.ToString() }))) + Environment.NewLine +
                "Application energy: unknown unless sourced requirements are configured. Use resources details for timestamps/sources. Host observations do not control Windows allocations.");
        }));
    private static string Safe(string value) => new(value.Select(c => char.IsControl(c) ? ' ' : c).ToArray());
}
