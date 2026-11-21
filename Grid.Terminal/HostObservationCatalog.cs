using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Grid.Communication;
using Grid.Agent;

namespace Grid.Terminal;

public sealed record HostObservationCapability(string Id, string Argument, AccessRiskTier Risk, string Description, string Permission);
public sealed record HostObservationResult(string Capability, string Argument, DateTimeOffset ObservedUtc, string Evidence, bool Success = true);

/// <summary>Trusted, read-only adapters exposed to model-selected conversational routing.</summary>
public static class HostObservationCatalog
{
    public static IReadOnlyList<HostObservationCapability> Entries { get; } =
    [
        new("host.system.read", "once", AccessRiskTier.Normal, "use this PC's bounded read-only system-information providers for the current request", "read-only system-information access"),
        new("host.system.write", "planned", AccessRiskTier.High, "use a bounded typed system-setting provider for the current request", "high-risk system-setting access; Windows may also require a UAC confirmation")
    ];

    public static string Prompt => string.Join("; ", Entries.Select(entry =>
        entry.Id + " => argument " + entry.Argument + " (" + entry.Risk + " risk; " + entry.Description + "; permission: " + entry.Permission + ")"));
    public static string CompactPrompt => string.Join("; ", Entries.Select(entry =>
        entry.Id + "/" + entry.Argument + "=" + entry.Risk + " system access"));

    public static string RouteSchema => JsonSerializer.Serialize(new
    {
        type = "object",
        properties = new
        {
            route = new { type = "string", @enum = new[] { "Conversation", "Access" } },
            hostCapability = new { type = "string", @enum = new[] { "" }.Concat(Entries.Select(e => e.Id)).ToArray() },
            argument = new { type = "string", @enum = new[] { "" }.Concat(Entries.Select(e => e.Argument)).ToArray() },
            message = new { type = "string" }
        },
        required = new[] { "route", "hostCapability", "argument", "message" },
        additionalProperties = false
    });

    public static string RouteSchemaFor(string capability)
    {
        var entry = Entries.Single(e => e.Id == capability);
        return JsonSerializer.Serialize(new
        {
            type = "object",
            properties = new
            {
                route = new { type = "string", @enum = new[] { "Access" } },
                hostCapability = new { type = "string", @enum = new[] { entry.Id } },
                argument = new { type = "string", @enum = new[] { entry.Argument } },
                message = new { type = "string" }
            },
            required = new[] { "route", "hostCapability", "argument", "message" },
            additionalProperties = false
        });
    }

    public static HostObservationCapability Require(string capability, string argument) =>
        Entries.SingleOrDefault(entry => entry.Id.Equals(capability, StringComparison.Ordinal) &&
            entry.Argument.Equals(argument, StringComparison.Ordinal))
        ?? throw new ArgumentException("That host observation is not registered.");

    public static bool TryParseRoute(string text, out bool access, out string capability, out string argument, out string message)
    {
        access = false; capability = argument = message = "";
        try
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            var fields = root.ValueKind == JsonValueKind.Object ? root.EnumerateObject().ToArray() : [];
            if (fields.Length != 4 || fields.Any(field => field.Name is not ("route" or "hostCapability" or "argument" or "message")) ||
                !root.TryGetProperty("route", out var routeValue) || routeValue.ValueKind != JsonValueKind.String ||
                !root.TryGetProperty("hostCapability", out var capabilityValue) || capabilityValue.ValueKind != JsonValueKind.String ||
                !root.TryGetProperty("argument", out var argumentValue) || argumentValue.ValueKind != JsonValueKind.String ||
                !root.TryGetProperty("message", out var messageValue) || messageValue.ValueKind != JsonValueKind.String)
                return false;
            var route = routeValue.GetString();
            capability = capabilityValue.GetString()!; argument = argumentValue.GetString()!; message = messageValue.GetString()!;
            if (message.Length is not (> 0 and <= 1200) || !CommunicationState.ValidContent(message)) return false;
            if (route == "Conversation") return capability.Length == 0 && argument.Length == 0;
            if (route != "Access") return false;
            // An Access route is a user-facing permission turn. Refusals, instructions,
            // missing-information questions and capability explanations must remain ordinary
            // conversation text, never latent consent state.
            if (!TryNormalizePermissionQuestion(message, out message)) return false;
            _ = Require(capability, argument); access = true;
            return true;
        }
        catch (Exception error) when (error is JsonException or ArgumentException) { return false; }
    }

    private static bool TryNormalizePermissionQuestion(string input, out string question)
    {
        question = input.Trim();
        var consent = Regex.IsMatch(question,
            @"\b(?:may|can|could|should|would)\s+I\b|\bwould\s+you\s+like\b|\bdo\s+you\s+(?:want|allow|authorize|approve|consent)\b|\b(?:permission|allow|authorize|approve|proceed|consent|okay|ok)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!consent) return false;
        if (question.Contains('?')) return true;
        if (!Regex.IsMatch(question, @"^(?:confirm|grant|allow|authorize|approve|permission)\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) return false;
        question = question.TrimEnd('.', '!', ' ') + "?";
        return true;
    }

    public static async Task<HostObservationResult> ExecuteAsync(string capability, string argument,
        TimeProvider? clock = null, CancellationToken token = default)
    {
        _ = Require(capability, argument);
        if (capability != "host.system.read") throw new ArgumentException("This access path requires a typed system operation plan.");
        var utc = (clock ?? TimeProvider.System).GetUtcNow();
        var local = TimeZoneInfo.ConvertTime(utc, TimeZoneInfo.Local);
        return new(capability, argument, utc, JsonSerializer.Serialize(new
        {
            source = "bounded read-only host system-information providers",
            clock = new
            {
                observedUtc = utc.ToString("O", CultureInfo.InvariantCulture),
                localDateTime = local.ToString("O", CultureInfo.InvariantCulture),
                utcOffset = local.ToString("zzz", CultureInfo.InvariantCulture),
                timeZone = TimeZoneInfo.Local.IsDaylightSavingTime(local.DateTime)
                    ? TimeZoneInfo.Local.DaylightName : TimeZoneInfo.Local.StandardName
            },
            platform = new
            {
                operatingSystem = Environment.OSVersion.VersionString,
                machine = Environment.MachineName,
                architecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
                processorCount = Environment.ProcessorCount,
                availableMemoryBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes
            },
            bluetooth = JsonDocument.Parse(await ReadBluetoothAsync(token)).RootElement.Clone()
        }));
    }

    private static async Task<string> ReadBluetoothAsync(CancellationToken token)
    {
        if (!OperatingSystem.IsWindows())
            return "{\"available\":false,\"reason\":\"Bluetooth inventory requires Windows\"}";
        const string body = """
            Import-Module (Join-Path $PSHOME 'Modules/PnpDevice/PnpDevice.psd1') -ErrorAction Stop
            $all = @(PnpDevice\Get-PnpDevice -Class Bluetooth -PresentOnly -ErrorAction Stop)
            $rows = @($all | Select-Object -First 64 | ForEach-Object { @{name=$_.FriendlyName; status=$_.Status; class=$_.Class} })
            @{available=$true; devices=$rows; truncated=($all.Count -gt 64); note='Present Bluetooth-class devices may include adapters and services, not only paired peripherals.'} | ConvertTo-Json -Compress -Depth 4
            """;
        var script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; [Console]::OutputEncoding=[Text.UTF8Encoding]::new($false); try { " +
            body + " } catch { @{available=$false; reason=$_.FullyQualifiedErrorId} | ConvertTo-Json -Compress }";
        var executable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell", "v1.0", "powershell.exe");
        if (!File.Exists(executable)) return "{\"available\":false,\"reason\":\"Windows PowerShell is unavailable\"}";
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true,
            RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
        };
        foreach (var value in new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-EncodedCommand",
                     Convert.ToBase64String(Encoding.Unicode.GetBytes(script)) }) start.ArgumentList.Add(value);
        start.Environment["PSModulePath"] = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell", "v1.0", "Modules");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        using var process = new Process { StartInfo = start };
        process.Start();
        var output = process.StandardOutput.ReadToEndAsync(deadline.Token);
        var error = process.StandardError.ReadToEndAsync(deadline.Token);
        try
        {
            await process.WaitForExitAsync(deadline.Token);
            var evidence = (await output).Trim(); _ = await error;
            if (process.ExitCode != 0 || evidence.Length is 0 or > 32768)
                throw new IOException("Bluetooth provider returned invalid bounded output.");
            using var document = JsonDocument.Parse(evidence);
            return document.RootElement.ValueKind == JsonValueKind.Object ? evidence :
                throw new InvalidDataException("Bluetooth provider returned invalid evidence.");
        }
        finally
        {
            deadline.Cancel();
            if (!process.HasExited) { try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { } }
            try { await Task.WhenAll(output, error); } catch (OperationCanceledException) { }
        }
    }
}
