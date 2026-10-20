using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Grid.Core;
using Grid.Simulation.Applications;
namespace Grid.Terminal;

/// <summary>Read-only, installed inventory and current-session executable groups. Never captures titles/arguments.</summary>
public sealed class WindowsApplicationDiscovery : IApplicationDiscovery
{
    private readonly ApplicationCatalog catalog = new();
    private DateTimeOffset inventoryAt;
    private readonly HashSet<string> knownApplications = new(StringComparer.Ordinal);
    private readonly HashSet<ProcessLifetime> previouslyObserved = new();
    public Task<ApplicationObservation> SampleAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) return Task.FromResult(new ApplicationObservation(DateTimeOffset.UtcNow, false, [], "Unsupported platform"));
        using var self = Process.GetCurrentProcess();
        var session = self.SessionId;
        if (DateTimeOffset.UtcNow - inventoryAt > TimeSpan.FromMinutes(5))
        {
            catalog.AddInstalled(WindowsInstalledApplications.Read(session));
            inventoryAt = DateTimeOffset.UtcNow;
        }
        var rows = new List<ApplicationProcess>();
        var budget = Stopwatch.StartNew();
        var complete = true;
        var processes = Process.GetProcesses();
        var liveIds = processes.Select(p => p.Id).ToHashSet();
        HashSet<string>? processNames = new(StringComparer.OrdinalIgnoreCase);
        foreach (var process in processes)
        {
            try { processNames.Add(process.ProcessName); }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or NotSupportedException)
            { processNames = null; break; }
        }
        try
        {
            foreach (var process in processes)
            {
                using (process)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (rows.Count >= 4096 || budget.Elapsed > TimeSpan.FromSeconds(5)) { complete = false; break; }
                    try
                    {
                        if (process.Id == Environment.ProcessId || process.SessionId != session || session == 0) continue;
                        var path = process.MainModule?.FileName;
                        if (string.IsNullOrWhiteSpace(path)) { complete = false; continue; }
                        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(session + ":" + path.ToUpperInvariant())));
                        var name = new string(process.ProcessName.Where(c => !char.IsControl(c)).ToArray()).Trim();
                        if (name.Length == 0) continue;
                        name = name[..Math.Min(name.Length, 80)];
                        var lifetime = new ProcessLifetime(process.Id, new DateTimeOffset(process.StartTime.ToUniversalTime()));
                        long? memory;
                        try { memory = process.WorkingSet64; } catch (System.ComponentModel.Win32Exception) { memory = null; }
                        rows.Add(new(key, name, lifetime, memory, process.MainWindowHandle != IntPtr.Zero));
                    }
                    catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or NotSupportedException)
                    { complete = false; }
                }
            }
        }
        finally { foreach (var process in processes) process.Dispose(); }
        var apps = ApplicationGrouping.Group(rows, knownApplications, complete).ToList();
        knownApplications.UnionWith(apps.Select(a => a.Key));
        if (complete) knownApplications.IntersectWith(apps.Select(a => a.Key));
        if (knownApplications.Count > 512) knownApplications.IntersectWith(apps.Take(512).Select(a => a.Key));
        if (apps.Count > 512) { apps = apps.Take(512).ToList(); complete = false; }
        var lifetimes = rows.Select(r => r.Lifetime).ToHashSet();
        var exited = previouslyObserved.Where(p => !liveIds.Contains(p.Pid) || lifetimes.Any(n => n.Pid == p.Pid && n != p)).ToArray();
        previouslyObserved.ExceptWith(exited);
        previouslyObserved.UnionWith(apps.SelectMany(a => a.Processes));
        if (previouslyObserved.Count > 8192)
            previouslyObserved.IntersectWith(previouslyObserved.OrderByDescending(p => p.StartedAtUtc).Take(8192).ToArray());
        apps = catalog.Observe(apps, complete, exited.ToHashSet(), processNames).ToList();
        if (apps.Count > 512) { apps = apps.Take(512).ToList(); complete = false; }
        return Task.FromResult(new ApplicationObservation(DateTimeOffset.UtcNow, complete, apps.AsReadOnly(), "Windows installed inventory + current-session processes v2; 512 resident limit") { ConfirmedExited = exited });
    }
}
