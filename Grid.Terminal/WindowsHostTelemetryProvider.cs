using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Grid.Simulation.Resources;

namespace Grid.Terminal;

/// <summary>Read-only Windows counters. Does not enumerate application processes or inspect network payloads.</summary>
public sealed class WindowsHostTelemetryProvider : IHostTelemetryProvider
{
    public static long? AvailablePhysicalMemory()
    {
        if (!OperatingSystem.IsWindows()) return null;
        var memory = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
        return GlobalMemoryStatusEx(ref memory) ? checked((long)memory.AvailablePhysical) : null;
    }
    private (ulong Idle, ulong Kernel, ulong User, long Stamp)? previousCpu;
    private readonly Dictionary<string, (long Received, long Sent, long Stamp)> previousNetwork = new();
    private readonly SemaphoreSlim sampling = new(1, 1);

    public async Task<IReadOnlyList<ResourceReading>> SampleAsync(CancellationToken cancellationToken)
    {
        await sampling.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { return await Task.Run(() => Sample(cancellationToken), cancellationToken).ConfigureAwait(false); }
        finally { sampling.Release(); }
    }

    private IReadOnlyList<ResourceReading> Sample(CancellationToken token)
    {
        var now = DateTimeOffset.UtcNow;
        var result = new List<ResourceReading>();
        void Add(ResourceMetric metric, string scope, double? value, string source, string detail = "",
            ObservationStatus missing = ObservationStatus.Unavailable) =>
            result.Add(new(metric, scope, value, value.HasValue ? ObservationStatus.Available : missing, now, source, detail));
        if (!OperatingSystem.IsWindows())
        {
            foreach (var metric in Enum.GetValues<ResourceMetric>())
                Add(metric, "host", null, "Windows adapter", "Windows is required.", ObservationStatus.Unsupported);
            return result.AsReadOnly();
        }
        token.ThrowIfCancellationRequested();
        if (GetSystemTimes(out var idle, out var kernel, out var user))
        {
            var stamp = Stopwatch.GetTimestamp();
            double? available = null;
            if (previousCpu is { } p && Stopwatch.GetElapsedTime(p.Stamp, stamp) <= TimeSpan.FromSeconds(10) &&
                idle >= p.Idle && kernel >= p.Kernel && user >= p.User)
            {
                var total = (double)(kernel - p.Kernel) + (user - p.User);
                var free = (double)(idle - p.Idle);
                if (total > 0 && free <= total) available = 100 * free / total;
            }
            previousCpu = (idle, kernel, user, stamp);
            Add(ResourceMetric.CpuAvailablePercent, "cpu:primary-group", available, "GetSystemTimes",
                "Interval idle fraction; primary processor group on systems with >64 processors.", ObservationStatus.WarmingUp);
        }
        else
        {
            previousCpu = null;
            Add(ResourceMetric.CpuAvailablePercent, "cpu:primary-group", null, "GetSystemTimes", "Counter unavailable.");
        }

        var memory = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
        var hasMemory = GlobalMemoryStatusEx(ref memory);
        Add(ResourceMetric.RamAvailableBytes, "host", hasMemory ? memory.AvailablePhysical : null, "GlobalMemoryStatusEx", "Available physical RAM, including reclaimable standby pages.");
        Add(ResourceMetric.RamCapacityBytes, "host", hasMemory ? memory.TotalPhysical : null, "GlobalMemoryStatusEx");

        var driveRoot = Path.GetPathRoot(Environment.SystemDirectory)!;
        try
        {
            var drive = new DriveInfo(driveRoot);
            Add(ResourceMetric.StorageAvailableBytes, driveRoot, drive.AvailableFreeSpace, "DriveInfo", "System-volume free space available to this user; not all drives.");
            Add(ResourceMetric.StorageCapacityBytes, driveRoot, drive.TotalSize, "DriveInfo");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Add(ResourceMetric.StorageAvailableBytes, driveRoot, null, "DriveInfo", ex.GetType().Name);
            Add(ResourceMetric.StorageCapacityBytes, driveRoot, null, "DriveInfo", ex.GetType().Name);
        }
        foreach (var metric in new[] { ResourceMetric.GpuAvailablePercent, ResourceMetric.VramAvailableBytes,
            ResourceMetric.StorageReadBytesPerSecond, ResourceMetric.StorageWriteBytesPerSecond })
            Add(metric, "host", null, "Windows adapter", "No supported collector configured.", ObservationStatus.Unsupported);

        token.ThrowIfCancellationRequested();
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces().Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .OrderBy(n => n.Id, StringComparer.Ordinal).ToArray();
            var current = new HashSet<string>();
            foreach (var nic in interfaces.Take(32))
            {
                token.ThrowIfCancellationRequested();
                current.Add(nic.Id);
                var scope = "network:" + nic.Id;
                try
                {
                    var connected = nic.OperationalStatus == OperationalStatus.Up;
                    Add(ResourceMetric.NetworkConnected, scope, connected ? 1 : 0, "NetworkInterface", "Local link state; does not prove Internet reachability.");
                    var stats = nic.GetIPStatistics(); var stamp = Stopwatch.GetTimestamp();
                    double? rx = null, tx = null;
                    if (connected && previousNetwork.TryGetValue(nic.Id, out var p))
                    {
                        var seconds = Stopwatch.GetElapsedTime(p.Stamp, stamp).TotalSeconds;
                        if (seconds > 0 && seconds <= 10 && stats.BytesReceived >= p.Received && stats.BytesSent >= p.Sent)
                        { rx = (stats.BytesReceived - p.Received) / seconds; tx = (stats.BytesSent - p.Sent) / seconds; }
                    }
                    if (connected) previousNetwork[nic.Id] = (stats.BytesReceived, stats.BytesSent, stamp);
                    else previousNetwork.Remove(nic.Id);
                    Add(ResourceMetric.NetworkReceiveBytesPerSecond, scope, rx, "NetworkInterface", "Per-interface counter delta; interfaces are not summed.", connected ? ObservationStatus.WarmingUp : ObservationStatus.Unavailable);
                    Add(ResourceMetric.NetworkSendBytesPerSecond, scope, tx, "NetworkInterface", "Per-interface counter delta; interfaces are not summed.", connected ? ObservationStatus.WarmingUp : ObservationStatus.Unavailable);
                }
                catch (Exception ex) when (ex is NetworkInformationException or PlatformNotSupportedException)
                {
                    previousNetwork.Remove(nic.Id);
                    // Remove a partial scope before publishing failure consistently.
                    result.RemoveAll(r => r.Scope == scope);
                    foreach (var metric in new[] { ResourceMetric.NetworkConnected, ResourceMetric.NetworkReceiveBytesPerSecond, ResourceMetric.NetworkSendBytesPerSecond })
                        Add(metric, scope, null, "NetworkInterface", ex.GetType().Name);
                }
            }
            foreach (var old in previousNetwork.Keys.Where(k => !current.Contains(k)).ToArray()) previousNetwork.Remove(old);
            if (interfaces.Length == 0 || interfaces.Length > 32)
                Add(ResourceMetric.NetworkConnected, "network:coverage", null, "NetworkInterface",
                    interfaces.Length == 0 ? "No non-loopback interfaces discovered." : "Collection limited to first 32 interfaces; coverage incomplete.");
        }
        catch (NetworkInformationException ex)
        {
            previousNetwork.Clear();
            Add(ResourceMetric.NetworkConnected, "network:coverage", null, "NetworkInterface", ex.GetType().Name);
        }
        return result.AsReadOnly();
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out ulong idle, out ulong kernel, out ulong user);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatus
    {
        public uint Length, MemoryLoad;
        public ulong TotalPhysical, AvailablePhysical, TotalPageFile, AvailablePageFile,
            TotalVirtual, AvailableVirtual, AvailableExtendedVirtual;
    }
}
