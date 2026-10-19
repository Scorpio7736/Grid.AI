using Grid.Core;
using Grid.Simulation;
using System.Globalization;
namespace Grid.Terminal;

public static class ResidentFormatting
{
    public static string Status(GridProgramSnapshot p) => p.Kind == ResidentKind.Application
        ? p.Application?.Confirmed == true ? "Observed running" : "Observation unknown" : p.Status.ToString();
    public static string Energy(GridProgramSnapshot p) => p.Kind == ResidentKind.Legacy
        ? p.Energy.ToString("0.###", CultureInfo.InvariantCulture)
        : p.Condition?.Score is { } score ? score.ToString("0.###", CultureInfo.InvariantCulture) + " (interpreted)" : "unknown";
    public static string Details(GridProgramSnapshot p) => $"Resident type: {p.Kind}" +
        (p.Application is { } a ? $" | Incarnation: {a.Incarnation} | Processes: {a.Processes.Count} | Observation: {(a.Confirmed ? "fresh" : "unknown/stale")} at {a.ObservedAtUtc:O} | Source: {a.Source}\nWorking set: {(a.Confirmed && a.WorkingSetBytes is { } bytes ? bytes + " bytes (measured; shared pages may overlap)" : "unknown")} | CPU/GPU/storage/network attribution: unknown" : "") +
        (p.Kind != ResidentKind.Legacy ? "\nCondition: " + (p.Condition?.Explanation ?? "No explicit resource requirements are known.") : "");
}
