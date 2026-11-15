using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Grid.Agent;
using Grid.Simulation.Tasks;

namespace Grid.Terminal;

public sealed class WindowsDeviceIntegration : IDeviceIntegration
{
    public string DeviceId => Environment.MachineName + ":" + Environment.UserName + ":" + Environment.ProcessPath;
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint window);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateDirectoryW(string path, nint securityAttributes);

    private static void RequireWindows()
    { if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows device integration is unavailable. Existing local file tools remain available."); }
    public DeviceDiscovery Discover(bool windowsOnly)
    {
        RequireWindows();
        using var self = Process.GetCurrentProcess();
        var rows = new List<DeviceObservation>(); var watch = Stopwatch.StartNew(); var incomplete = false;
        var processes = Process.GetProcesses();
        try
        {
            foreach (var p in processes)
            {
                if (rows.Count >= 256 || watch.Elapsed > TimeSpan.FromSeconds(2)) { incomplete = true; break; }
                try
                {
                    if (p.SessionId != self.SessionId) continue;
                    if (windowsOnly && p.MainWindowHandle == 0) continue;
                    rows.Add(new(p.Id.ToString(), Safe(p.ProcessName), p.MainWindowHandle == 0 ? "Process; no main window" : "Window available; title not read"));
                }
                catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
                { incomplete = true; }
            }
        }
        finally { foreach (var p in processes) p.Dispose(); }
        return new(rows.AsReadOnly(), incomplete, "Windows current-session process metadata", DateTimeOffset.UtcNow);
    }
    private static string Safe(string value) => new(value.Where(c => !char.IsControl(c)).Take(256).ToArray());
    private static string LocalPath(string path)
    {
        if (!Path.IsPathFullyQualified(path) || path.StartsWith(@"\\") || path.Length > 1024 ||
            path[Path.GetPathRoot(path)!.Length..].Contains(':'))
            throw new ArgumentException("Choose an absolute local path without streams or device/UNC syntax.");
        WorkspaceFileChange.ValidateDestinationRoot(path);
        var parent = Path.GetDirectoryName(Path.GetFullPath(path)) ?? throw new ArgumentException("Choose a non-root target.");
        return DevelopmentPolicy.CheckedPath(parent, Path.GetFileName(path), fileDestination: true);
    }
    public DeviceTarget Resolve(DeviceRequest request)
    {
        RequireWindows(); request.Validate();
        if (request.Operation is DeviceOperation.Focus or DeviceOperation.InspectWindow)
        {
            if (!int.TryParse(request.Target, out var pid) || pid <= 0) throw new ArgumentException("Select an observed process ID.");
            using var p = Process.GetProcessById(pid); using var self = Process.GetCurrentProcess();
            if (p.SessionId != self.SessionId || p.HasExited || p.MainWindowHandle == 0)
                throw new InvalidOperationException("Selected window is unavailable in the current desktop session.");
            var fingerprint = p.Id + ":" + p.StartTime.ToUniversalTime().Ticks + ":" + p.MainWindowHandle;
            return new(pid.ToString(), fingerprint, Safe(p.ProcessName) + " window (title withheld until approved inspection)");
        }
        var path = LocalPath(request.Target);
        if (request.Operation == DeviceOperation.CreateFolder)
        {
            var parent = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(parent) || File.Exists(path) || Directory.Exists(path)) throw new IOException("Choose a new folder beneath an existing writable parent.");
            return new(path, parent + ":" + Directory.GetCreationTimeUtc(parent).Ticks, path);
        }
        if (!path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) throw new IOException("Select an existing .exe; shortcuts, URLs and command arguments are unsupported.");
        var blocked = new[] { "cmd", "powershell", "pwsh", "wscript", "cscript", "mshta", "rundll32", "regsvr32" };
        if (blocked.Contains(Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Command/script hosts are outside the application-launch capability.");
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (file.Length > 256L * 1024 * 1024) throw new IOException("Executable exceeds the 256 MiB verification limit.");
        return new(path, Convert.ToHexString(SHA256.HashData(file)), path);
    }
    public string Execute(DeviceRequest request, DeviceTarget expected)
    {
        if (Resolve(request) != expected) throw new InvalidOperationException("Selected target changed immediately before execution.");
        switch (request.Operation)
        {
            case DeviceOperation.Launch:
                using (var process = Process.Start(new ProcessStartInfo(expected.Locator) {
                    UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(expected.Locator)! }))
                    return process is null ? throw new IOException("Windows did not return a process receipt.") :
                        "Started process " + process.Id + ". Application readiness and later behavior are not verified.";
            case DeviceOperation.Focus:
                using (var process = Process.GetProcessById(int.Parse(expected.Locator)))
                {
                    if (!SetForegroundWindow(process.MainWindowHandle)) throw new InvalidOperationException("Windows refused focus. Select the window manually; GRID will not bypass foreground restrictions.");
                    return "Windows accepted the foreground-window request.";
                }
            case DeviceOperation.InspectWindow:
                using (var process = Process.GetProcessById(int.Parse(expected.Locator)))
                    return "Selected window title: " + Safe(process.MainWindowTitle) + ". No controls, clipboard or pixels read.";
            case DeviceOperation.CreateFolder:
                if (!CreateDirectoryW(expected.Locator, 0)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Windows refused creation; the target may already exist or the media may be read-only.");
                return "Created empty folder: " + expected.Locator;
            default: throw new ArgumentException("Unsupported device operation.");
        }
    }
}
