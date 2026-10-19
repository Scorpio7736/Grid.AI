using Grid.Core;
namespace Grid.Simulation.Applications;

public sealed record ApplicationProcess(string Key, string Name, ProcessLifetime Lifetime, long? Memory, bool Window);

/// <summary>One executable group, including all observed same-image helper processes and independent windows.</summary>
public static class ApplicationGrouping
{
    public static IReadOnlyList<ObservedApplication> Group(IReadOnlyList<ApplicationProcess> rows,
        IReadOnlySet<string> known, bool complete) => Array.AsReadOnly(rows.GroupBy(r => r.Key, StringComparer.Ordinal)
        .Where(g => known.Contains(g.Key) || g.Any(r => r.Window)).OrderBy(g => g.Key, StringComparer.Ordinal)
        .Select(g => new ObservedApplication(g.Key, g.First().Name,
            Array.AsReadOnly(g.Select(r => r.Lifetime).Distinct().OrderBy(p => p.Pid).ToArray()),
            complete && g.All(r => r.Memory.HasValue) ? g.DistinctBy(r => r.Lifetime).Sum(r => r.Memory!.Value) : null)).ToArray());
}
