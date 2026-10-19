using System.Globalization;
using Grid.Simulation;

namespace Grid.Terminal;

/// <summary>Pure formatting. Rendering never advances the engine or invents ongoing actions.</summary>
public static class LiveViewRenderer
{
    public static (int Programs, int Events) PageSizes(int height)
    {
        height = Math.Clamp(height, 12, 100);
        var events = Math.Min(5, Math.Max(1, (height - 11) / 4));
        return (Math.Max(1, (height - 9 - events * 2) / 2), events);
    }

    public static IReadOnlyList<string> Render(GridLiveSnapshot snapshot, int width, int height,
        int programPage = 0, int eventPage = 0, bool interactive = true)
    {
        width = Math.Clamp(width, 20, 240);
        height = Math.Clamp(height, 12, 100);
        var state = snapshot.State;
        var (programSlots, eventSlots) = PageSizes(height);
        var programPages = Math.Max(1, (state.Programs.Count + programSlots - 1) / programSlots);
        var eventPages = Math.Max(1, (snapshot.Events.Count + eventSlots - 1) / eventSlots);
        programPage = Math.Clamp(programPage, 0, programPages - 1);
        eventPage = Math.Clamp(eventPage, 0, eventPages - 1);
        var lines = new List<string>
        {
            interactive ? "GRID LIVE | Q/Esc: prompt | PgUp/PgDn: citizens | Left/Right: events" : "GRID SNAPSHOT | Single frame; returning to commands",
            $"State: {(state.IsRunning ? "Running" : "Paused")} | Tick: {state.TickCount} | Simulation time: {state.ElapsedSimulationTime:c}",
            $"Programs: {state.Programs.Count} | Districts: {state.Districts.Count}" +
                (interactive ? " | Refresh: 250 ms (independent of ticks)" : " | No automatic redraw"),
            $"CURRENT STATE - citizens page {programPage + 1}/{programPages}",
        };
        var districts = state.Districts.ToDictionary(d => d.Id, d => d.Name);
        foreach (var program in state.Programs.Skip(programPage * programSlots).Take(programSlots))
        {
            lines.Add($"{program.Name} | {program.Kind} | {ResidentFormatting.Status(program)} | Energy: {ResidentFormatting.Energy(program)} | {program.Occupation}");
            lines.Add($"{program.Id} | {districts.GetValueOrDefault(program.DistrictId, "Unknown district")} | {(program.Application is { } a ? (a.Confirmed ? "fresh" : "unknown/stale") + " run " + a.Incarnation : program.Kind.ToString())}");
        }
        if (state.Programs.Count == 0) lines.Add("No programs.");
        lines.Add(snapshot.HostResources.Count == 0 ? "Status describes current state; no autonomous jobs/routes are implemented." :
            "HOST | " + string.Join(" | ", snapshot.HostResources.Where(r => r.Metric is Grid.Simulation.Resources.ResourceMetric.CpuAvailablePercent or Grid.Simulation.Resources.ResourceMetric.RamAvailableBytes)
                .Select(r => $"{r.Metric}: {(r.Value is { } v ? v.ToString("0.##", CultureInfo.InvariantCulture) + " " + r.Unit : "unknown (" + r.Status + ")")}")) + " | resources: full details");
        lines.Add($"RECENT COMMITTED EVENTS - page {eventPage + 1}/{eventPages} (newest first)");
        lines.Add($"Retained: {snapshot.Events.Count} | Latest sequence: {snapshot.LatestSequence} | Evicted: {snapshot.DroppedEvents}");
        foreach (var item in snapshot.Events.Reverse().Skip(eventPage * eventSlots).Take(eventSlots))
        {
            lines.Add($"#{item.Sequence} t{item.Tick} {item.SimulationTime:c} {item.Kind} {item.ProgramName}" +
                (item.ProgramId is { } id ? $" [{id}]" : ""));
            lines.Add("  " + item.Detail);
        }
        if (snapshot.Events.Count == 0) lines.Add("No events yet. Idle citizens do not invent activity.");
        lines.Add("In-memory history only. '+' marks clipped text. Entering/leaving does not control the clock.");
        return lines.Take(height - 1).Select(line => Fit(line, width - 1)).ToArray();
    }

    // A predictable one-cell ASCII surface prevents controls, escape injection and wide glyph wrapping.
    // Source names and events remain intact in snapshots; only the compact display is sanitized.
    private static string Fit(string text, int width)
    {
        var safe = new string(text.Select(c => c is >= ' ' and <= '~' ? c : '?').ToArray());
        return safe.Length <= width ? safe : safe[..(width - 1)] + "+";
    }
}
