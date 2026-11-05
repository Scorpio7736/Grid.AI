using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Globalization;
using Grid.Simulation.Resources;

namespace Grid.Terminal;

public static class ModelDeviceDiscovery
{
    public static async Task<string> DescribeAsync(string root, CancellationToken token)
    {
        var readings = await new WindowsHostTelemetryProvider().SampleAsync(token);
        string Memory(ResourceMetric metric) => readings.FirstOrDefault(r => r.Metric == metric && r.Status == ObservationStatus.Available)?.Value?.ToString("N0") ?? "unknown";
        string disk;
        try { disk = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(root))!).AvailableFreeSpace.ToString("N0"); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { disk = "unknown"; }
        var ollama = File.Exists(Path.Combine(root, "ollama", "ollama.exe"));
        var llama = File.Exists(Path.Combine(root, "llamacpp", "b12110", "llama-server.exe"));
        var gpu = await NvidiaAsync(token);
        return $"OS: {RuntimeInformation.OSDescription}; architecture: {RuntimeInformation.ProcessArchitecture}\n" +
            $"RAM total: {Memory(ResourceMetric.RamCapacityBytes)} bytes; available: {Memory(ResourceMetric.RamAvailableBytes)} bytes\n" +
            $"Runtime-volume free space: {disk} bytes\n{gpu}\n" +
            $"Bundled engine files: Ollama={ollama}; llama.cpp CPU={llama}. File presence is not a version/hash or backend test.\n" +
            "Profile promotion: pending device-specific runtime/model acceptance. Hardware alone does not enable Standard/Performance or image models. Use ai test for the current baseline.";
    }

    public static IReadOnlyList<(string Name, long Total, long Free)> ParseNvidia(string text)
    {
        var result = new List<(string, long, long)>();
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length != 3 || parts[0].Length > 256 || parts[0].Any(char.IsControl) ||
                !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var total) ||
                !long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var free) || total <= 0 || total > 1_048_576 || free > total)
                throw new InvalidDataException("GPU counters unavailable or malformed.");
            result.Add((parts[0], checked(total * 1_048_576), checked(free * 1_048_576)));
            if (result.Count > 16) throw new InvalidDataException("GPU collector coverage limit reached.");
        }
        return result;
    }

    private static async Task<string> NvidiaAsync(CancellationToken token)
    {
        const string unknown = "GPU vendor/total VRAM/free VRAM: unknown. Optional NVIDIA driver collector unavailable; other GPU backends require dedicated collectors.";
        if (!OperatingSystem.IsWindows()) return unknown;
        var executable = Path.Combine(Environment.SystemDirectory, "nvidia-smi.exe");
        if (!File.Exists(executable)) return unknown;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(3));
        using var process = new Process { StartInfo = new(executable, "--query-gpu=name,memory.total,memory.free --format=csv,noheader,nounits")
            { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardOutput = true, RedirectStandardError = true } };
        try
        {
            process.Start();
            async Task<string> Read(System.IO.StreamReader reader)
            {
                var buffer = new char[4097]; var count = 0;
                while (count < buffer.Length)
                {
                    var read = await reader.ReadAsync(buffer.AsMemory(count), deadline.Token);
                    if (read == 0) return new string(buffer, 0, count);
                    count += read;
                }
                throw new InvalidDataException("GPU collector output exceeds limit.");
            }
            var output = Read(process.StandardOutput); var errors = Read(process.StandardError);
            await Task.WhenAll(output, errors, process.WaitForExitAsync(deadline.Token));
            if (process.ExitCode != 0) return unknown;
            var devices = ParseNvidia(await output);
            return devices.Count == 0 ? unknown : string.Join("\n", devices.Select(g =>
                $"GPU: {g.Name}; total VRAM: {g.Total:N0} bytes; free VRAM: {g.Free:N0} bytes (driver snapshot; adapters are not pooled).")) +
                "\nNVIDIA driver detected; CUDA/runtime compatibility still needs model-specific verification.";
        }
        catch (Exception e) when (e is IOException or System.ComponentModel.Win32Exception or OperationCanceledException)
        { token.ThrowIfCancellationRequested(); return unknown; }
        finally { try { if (!process.HasExited) process.Kill(true); } catch (InvalidOperationException) { } }
    }
}
