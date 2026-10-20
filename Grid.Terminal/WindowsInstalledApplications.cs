using Microsoft.Win32;
using System.Security.Cryptography;
using System.Text;
using Grid.Core;
using Grid.Simulation.Applications;
using System.Runtime.Versioning;
namespace Grid.Terminal;

/// <summary>Read-only uninstall inventory; ambiguous executable mappings remain unknown.</summary>
[SupportedOSPlatform("windows")]
public static class WindowsInstalledApplications
{
    public static IReadOnlyList<ObservedApplication> Read(int session)
    {
        var result = new Dictionary<string, ObservedApplication>(StringComparer.Ordinal);
        foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var root = RegistryKey.OpenBaseKey(hive, view);
                using var uninstall = root.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
                foreach (var id in uninstall?.GetSubKeyNames() ?? [])
                {
                    using var entry = uninstall!.OpenSubKey(id);
                    if (entry?.GetValue("SystemComponent") is int systemComponent && systemComponent == 1) continue;
                    var name = entry?.GetValue("DisplayName") as string;
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    name = new string(name.Where(c => !char.IsControl(c)).Take(80).ToArray()).Trim();
                    if (name.Length == 0) continue;
                    // Only existing executable icon targets provide a usable process mapping.
                    var key = "inventory:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hive + ":" + id)));
                    string? executableName = null;
                    var icon = entry?.GetValue("DisplayIcon") as string;
                    var candidate = icon?.Trim().Trim('"');
                    if (candidate is not null)
                    {
                        var end = candidate.LastIndexOf(".exe", StringComparison.OrdinalIgnoreCase);
                        if (end >= 0) candidate = candidate[..(end + 4)].Trim('"');
                        if (Path.IsPathFullyQualified(candidate) && candidate.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(candidate))
                        {
                            executableName = Path.GetFileNameWithoutExtension(candidate);
                            key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(session + ":" + candidate.ToUpperInvariant())));
                        }
                    }
                    result.TryAdd(key, new(key, name, [], null) { ExecutableName = executableName, Activity = ApplicationActivity.Unknown });
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException) { }
        }
        return result.Values.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
