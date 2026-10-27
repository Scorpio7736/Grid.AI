using System.Diagnostics;
using System.Text;
using System.Text.Json;
namespace Grid.Terminal;

public sealed record ScreenMonitor(string Device, int X, int Y, int Width, int Height, bool Primary, string HardwareId = "");
public sealed record ScreenObservation(bool Available, string Text, string Reason, DateTimeOffset CapturedUtc, bool Truncated = false);
public interface IScreenContextProvider
{
    Task<IReadOnlyList<ScreenMonitor>> MonitorsAsync(CancellationToken token);
    Task<ScreenObservation> CaptureAsync(ScreenMonitor monitor, CancellationToken token);
}
/// <summary>Offline OS OCR, with no screenshot or OCR files written to disk.</summary>
public sealed class WindowsScreenContextProvider : IScreenContextProvider
{
    public async Task<IReadOnlyList<ScreenMonitor>> MonitorsAsync(CancellationToken token) =>
        JsonSerializer.Deserialize<ScreenMonitor[]>(await RunAsync("options", null, token)) ?? throw new IOException("No monitor data returned.");
    public async Task<ScreenObservation> CaptureAsync(ScreenMonitor monitor, CancellationToken token) =>
        JsonSerializer.Deserialize<ScreenObservation>(await RunAsync("capture", monitor, token)) ?? throw new IOException("No capture result returned.");
    public Task<string> VerifyOcrAsync(CancellationToken token = default) => RunAsync("verify", null, token);
    private static async Task<string> RunAsync(string mode, ScreenMonitor? monitor, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) throw new IOException("Screen OCR requires Windows; nothing captured.");
        var helper = Path.Combine(AppContext.BaseDirectory, "Grid-ScreenContext.ps1");
        if (!File.Exists(helper)) throw new IOException("Bundled screen helper missing. Rebuild or reinstall GRID.");
        var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe"))
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8 };
        foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-STA", "-ExecutionPolicy", "Bypass", "-File", helper, "-Mode", mode }) start.ArgumentList.Add(argument);
        if (monitor is not null) { start.ArgumentList.Add("-MonitorJson"); start.ArgumentList.Add(JsonSerializer.Serialize(monitor)); }
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
        stop.CancelAfter(TimeSpan.FromSeconds(25));
        using var process = Process.Start(start) ?? throw new IOException("Screen helper could not start.");
        using var cancel = stop.Token.Register(() => { try { if (!process.HasExited) process.Kill(true); } catch (InvalidOperationException) { } });
        var output = ReadBounded(process.StandardOutput, stop.Token);
        var errors = ReadBounded(process.StandardError, stop.Token);
        try
        {
            await process.WaitForExitAsync(stop.Token);
            var text = await output; await errors;
            if (process.ExitCode != 0) throw new IOException("Windows screen/OCR helper failed; no screen context sent.");
            return text;
        }
        finally
        {
            if (!process.HasExited) { try { process.Kill(true); } catch (InvalidOperationException) { } }
            try { await Task.WhenAll(output, errors); } catch (Exception e) when (e is IOException or OperationCanceledException) { }
        }
    }
    private static async Task<string> ReadBounded(StreamReader reader, CancellationToken token)
    {
        var result = new StringBuilder(); var buffer = new char[4096]; int count;
        while ((count = await reader.ReadAsync(buffer.AsMemory(), token)) > 0)
        {
            if (result.Length + count > 65536) throw new IOException("Screen helper output exceeded its bound.");
            result.Append(buffer, 0, count);
        }
        return result.ToString();
    }
}

