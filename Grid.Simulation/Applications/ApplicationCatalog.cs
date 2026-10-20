using Grid.Core;
namespace Grid.Simulation.Applications;

/// <summary>Retains discovered applications independently of their process lifetimes.</summary>
public sealed class ApplicationCatalog
{
    private readonly Dictionary<string, ObservedApplication> entries = new(StringComparer.Ordinal);
    public void AddInstalled(IEnumerable<ObservedApplication> applications)
    {
        foreach (var app in applications) entries.TryAdd(app.Key, app);
    }

    public IReadOnlyList<ObservedApplication> Observe(IReadOnlyList<ObservedApplication> running,
        bool complete, IReadOnlySet<ProcessLifetime> exited, IReadOnlySet<string>? processNames = null)
    {
        var keys = running.Select(a => a.Key).ToHashSet(StringComparer.Ordinal);
        foreach (var app in running)
            entries[app.Key] = app;
        foreach (var app in entries.Values.ToArray())
        {
            if (keys.Contains(app.Key)) continue;
            var remaining = app.Processes.Where(p => !exited.Contains(p)).ToArray();
            // On partial enumeration, unknown processes could belong to this application.
            // Retain unresolved lifetimes so later positive exit evidence can settle them.
            var sleeping = (complete && !app.Key.StartsWith("inventory:", StringComparison.Ordinal)) ||
                (processNames is not null && app.ExecutableName is { } executable && !processNames.Contains(executable));
            entries[app.Key] = app with
            {
                Processes = sleeping ? [] : Array.AsReadOnly(remaining),
                WorkingSetBytes = null,
                Activity = sleeping ? ApplicationActivity.Sleeping : ApplicationActivity.Unknown
            };
        }
        // Active applications get priority if the presentation capacity is reached.
        return running.Concat(entries.Values.Where(a => !keys.Contains(a.Key))
            .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)).ToArray();
    }
}
