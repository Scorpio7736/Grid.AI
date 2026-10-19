using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Grid.Core.Generation;

namespace Grid.Terminal;

/// <summary>First-creation inventory only. Fixed local CIM queries; no serials, MACs, peers or user files.</summary>
public sealed class WindowsHardwareDiscovery : IHardwareDiscovery
{
    private const string Script = """
        $ErrorActionPreference = 'Stop'
        [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
        function Label($value) { $s = [string]$value; if ($s.Length -gt 256) { $s.Substring(0,256) } else { $s } }
        $specs = @(
          @{ Kind=0; Class='Win32_BaseBoard'; Props=@('Manufacturer','Product') },
          @{ Kind=1; Class='Win32_Processor'; Props=@('Name','NumberOfLogicalProcessors') },
          @{ Kind=2; Class='Win32_VideoController'; Props=@('Name') },
          @{ Kind=3; Class='Win32_PhysicalMemory'; Props=@('Manufacturer','PartNumber','Capacity') },
          @{ Kind=4; Class='Win32_DiskDrive'; Props=@('Model','Size') },
          @{ Kind=5; Class='Win32_NetworkAdapter'; Props=@('Name','PhysicalAdapter') }
        )
        $categories = @()
        foreach ($spec in $specs) {
          try {
            $rows = @(Get-CimInstance -ClassName $spec.Class -Property $spec.Props -OperationTimeoutSec 3 -ErrorAction Stop)
            $components = @($rows | ForEach-Object {
              $row = $_
              $label = ''; $capacity = $null; $units = $null; $virtual = $null
              switch ($spec.Kind) {
                0 { $label = "$($row.Manufacturer) $($row.Product)" }
                1 { $label = $row.Name; $units = $row.NumberOfLogicalProcessors }
                2 { $label = $row.Name }
                3 { $label = "$($row.Manufacturer) $($row.PartNumber)"; $capacity = $row.Capacity }
                4 { $label = $row.Model; $capacity = $row.Size }
                5 { $label = $row.Name; if ($null -ne $row.PhysicalAdapter) { $virtual = -not $row.PhysicalAdapter } }
              }
              [pscustomobject]@{ Label=(Label $label); CapacityBytes=$capacity; Units=$units; IsVirtual=$virtual }
            } | Sort-Object Label,CapacityBytes,Units,IsVirtual)
            $status = 0; if ($components.Count -eq 0) { $status = 1 }
            $categories += [pscustomobject]@{ Kind=$spec.Kind; Status=$status; Source=$spec.Class;
              Note='Local inventory; no physical board coordinates. GPU capacity intentionally omitted.';
              Components=@($components | Select-Object -First 32); Truncated=($components.Count -gt 32) }
          } catch {
            $categories += [pscustomobject]@{ Kind=$spec.Kind; Status=2; Source=$spec.Class;
              Note='Query failed or timed out; fallback geometry used.'; Components=@(); Truncated=$false }
          }
        }
        [pscustomobject]@{ CollectedAtUtc=[DateTimeOffset]::UtcNow.ToString('O'); Categories=$categories } | ConvertTo-Json -Depth 8 -Compress
        """;

    public async Task<HardwareProfile> DiscoverAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) return HardwareProfile.Unavailable(DiscoveryStatus.Unsupported, "Windows inventory adapter unavailable.");
        var executable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
        var info = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 };
        foreach (var arg in new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-EncodedCommand", Convert.ToBase64String(Encoding.Unicode.GetBytes(Script)) })
            info.ArgumentList.Add(arg);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(25));
        Process? process = null;
        Task<string>? output = null, error = null;
        try
        {
            process = Process.Start(info) ?? throw new IOException("Inventory collector could not start.");
            output = ReadBounded(process.StandardOutput, timeout.Token);
            error = ReadBounded(process.StandardError, timeout.Token);
            // Read streams concurrently so diagnostics cannot block the subprocess.
            await Task.WhenAll(output, error, process.WaitForExitAsync(timeout.Token)).ConfigureAwait(false);
            if (process.ExitCode != 0) return HardwareProfile.Unavailable(DiscoveryStatus.Unavailable, "Local inventory collector failed.");
            return HardwareProfile.Normalize(JsonSerializer.Deserialize<HardwareProfile>(await output) ?? throw new InvalidDataException("Empty hardware profile."));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { return HardwareProfile.Unavailable(DiscoveryStatus.Unavailable, "Inventory timed out; fallback geometry used."); }
        catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception or JsonException or ArgumentException)
        { return HardwareProfile.Unavailable(DiscoveryStatus.Unavailable, "Inventory unavailable: " + ex.GetType().Name); }
        finally
        {
            if (process is not null)
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { }
                process.Dispose();
            }
        }
    }

    private static async Task<string> ReadBounded(StreamReader reader, CancellationToken token)
    {
        var builder = new StringBuilder(); var buffer = new char[2048];
        int count;
        while ((count = await reader.ReadAsync(buffer.AsMemory(), token).ConfigureAwait(false)) != 0)
        {
            if (builder.Length + count > 512000) throw new InvalidDataException("Inventory output exceeds limit.");
            builder.Append(buffer, 0, count);
        }
        return builder.ToString();
    }
}
