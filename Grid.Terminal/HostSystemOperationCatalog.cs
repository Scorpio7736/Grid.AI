using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Grid.Terminal;

public sealed record HostSystemOperation(int Version, string Operation, int OffsetSeconds, string TargetDate, int TargetDay);

/// <summary>Bounded host mutations. Model output selects parameters; only registered typed operations execute.</summary>
public static class HostSystemOperationCatalog
{
    public static string Schema { get; } = JsonSerializer.Serialize(new
    {
        type = "object",
        properties = new
        {
            version = new { type = "integer", @enum = new[] { 1 } },
            operation = new { type = "string", @enum = new[] { "AdjustSystemClock", "SetSystemDate", "SetSystemDay" } },
            offsetSeconds = new { type = "integer", minimum = -86400, maximum = 86400 },
            targetDate = new { type = "string", description = "ISO yyyy-MM-dd for SetSystemDate; otherwise empty" },
            targetDay = new { type = "integer", minimum = 0, maximum = 31, description = "1..31 for SetSystemDay; otherwise 0" }
        },
        required = new[] { "version", "operation", "offsetSeconds", "targetDate", "targetDay" },
        additionalProperties = false
    });

    public static HostSystemOperation Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var fields = root.ValueKind == JsonValueKind.Object ? root.EnumerateObject().ToArray() : [];
        if (fields.Length != 5 || fields.Any(f => f.Name is not ("version" or "operation" or "offsetSeconds" or "targetDate" or "targetDay")) ||
            !root.TryGetProperty("version", out var version) || !version.TryGetInt32(out var v) || v != 1 ||
            !root.TryGetProperty("operation", out var operation) || operation.ValueKind != JsonValueKind.String ||
            !root.TryGetProperty("offsetSeconds", out var offset) || !offset.TryGetInt32(out var seconds) || seconds is < -86400 or > 86400 ||
            !root.TryGetProperty("targetDate", out var date) || date.ValueKind != JsonValueKind.String ||
            !root.TryGetProperty("targetDay", out var day) || !day.TryGetInt32(out var targetDay) || targetDay is < 0 or > 31)
            throw new ArgumentException("Invalid typed system operation.");
        var name = operation.GetString()!;
        var targetDate = date.GetString()!;
        var valid = name switch
        {
            // Fields not used by the selected operation are ignored after strict parsing. This
            // keeps model formatting noise from becoming behavior; only the selected operation's
            // validated parameters reach the fixed host adapter.
            "AdjustSystemClock" => seconds != 0,
            "SetSystemDate" => seconds == 0 &&
                DateOnly.TryParseExact(targetDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            "SetSystemDay" => seconds == 0 && targetDay is >= 1 and <= 31,
            _ => false
        };
        if (!valid) throw new ArgumentException("Invalid typed system operation.");
        return name switch
        {
            "AdjustSystemClock" => new(v, name, seconds, "", 0),
            "SetSystemDate" => new(v, name, 0, targetDate, 0),
            "SetSystemDay" => new(v, name, 0, "", targetDay),
            _ => throw new ArgumentException("Invalid typed system operation.")
        };
    }

    public static async Task<HostObservationResult> ExecuteAsync(HostSystemOperation operation, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("System clock adjustment currently requires Windows.");
        if (operation.Operation is not ("AdjustSystemClock" or "SetSystemDate" or "SetSystemDay"))
            throw new ArgumentException("Unsupported typed system operation.");
        var system = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var executable = Path.Combine(system, "WindowsPowerShell", "v1.0", "powershell.exe");
        if (!File.Exists(executable)) throw new InvalidOperationException("Windows PowerShell is unavailable.");
        var receipt = Path.Combine(Path.GetTempPath(), "GridAI-clock-" + Guid.NewGuid().ToString("N") + ".json");
        var escapedReceipt = receipt.Replace("'", "''", StringComparison.Ordinal);
        var targetExpression = operation.Operation switch
        {
            "AdjustSystemClock" => "(Get-Date).AddSeconds(" + operation.OffsetSeconds.ToString(CultureInfo.InvariantCulture) + ")",
            "SetSystemDate" => "([datetime]::ParseExact('" + operation.TargetDate + "','yyyy-MM-dd',[Globalization.CultureInfo]::InvariantCulture).Date + (Get-Date).TimeOfDay)",
            "SetSystemDay" => "(Get-Date -Year (Get-Date).Year -Month (Get-Date).Month -Day " + operation.TargetDay.ToString(CultureInfo.InvariantCulture) +
                " -Hour (Get-Date).Hour -Minute (Get-Date).Minute -Second (Get-Date).Second)",
            _ => throw new ArgumentException("Unsupported typed system operation.")
        };
        var script = "$ErrorActionPreference='Stop'; $target=" + targetExpression +
            "; Set-Date -Date $target -ErrorAction Stop | Out-Null; @{resultingLocalTime=(Get-Date).ToString('O'); operation='" + operation.Operation +
            "'; offsetSeconds=" + operation.OffsetSeconds.ToString(CultureInfo.InvariantCulture) + "; targetDate='" + operation.TargetDate +
            "'; targetDay=" + operation.TargetDay.ToString(CultureInfo.InvariantCulture) +
            "; source='Windows system clock'} | ConvertTo-Json -Compress | Set-Content -LiteralPath '" + escapedReceipt + "' -Encoding UTF8";
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Hidden
        };
        foreach (var value in new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-EncodedCommand", encoded }) start.ArgumentList.Add(value);
        try
        {
            using var process = Process.Start(start) ?? throw new InvalidOperationException("The elevated system-setting helper did not start.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token); deadline.CancelAfter(TimeSpan.FromMinutes(2));
            await process.WaitForExitAsync(deadline.Token);
            if (process.ExitCode != 0 || !File.Exists(receipt)) throw new InvalidOperationException("The system-setting helper did not complete successfully.");
            var evidence = await File.ReadAllTextAsync(receipt, token);
            using var parsed = JsonDocument.Parse(evidence);
            return new("host.system.write", "planned", DateTimeOffset.UtcNow,
                JsonSerializer.Serialize(new { operation = operation.Operation, elevatedHelper = true, receipt = parsed.RootElement.Clone() }));
        }
        catch (Win32Exception error) when (error.NativeErrorCode == 1223)
        {
            return new("host.system.write", "planned", DateTimeOffset.UtcNow,
                JsonSerializer.Serialize(new { operation = operation.Operation, success = false, reason = "Windows elevation was declined; the system clock was not changed." }), false);
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            return new("host.system.write", "planned", DateTimeOffset.UtcNow,
                JsonSerializer.Serialize(new { operation = operation.Operation, success = false, reason = error.Message }), false);
        }
        finally { try { File.Delete(receipt); } catch { } }
    }
}
