using Grid.Communication;
using System.Text.Json;

namespace Grid.Terminal;

public sealed record SetupHealthCheck(string Name, bool Passed, string Detail, string Repair);

/// <summary>Explicit diagnostics. Never opens a world, records audio, captures a desktop or grants access.</summary>
public static class SetupHealth
{
    public static string ValidateOcr(string json)
    {
        var result = JsonSerializer.Deserialize<ScreenObservation>(json) ?? throw new IOException("No OCR result.");
        if (!result.Available || !result.Text.Contains("COBALT", StringComparison.OrdinalIgnoreCase) || !result.Text.Contains("42", StringComparison.Ordinal))
            throw new IOException("Synthetic OCR check failed: " + result.Reason);
        return "Synthetic image recognized. No desktop captured; protected-window capture permission remains untested.";
    }
    public static string ValidateVoice(string inventory)
    {
        foreach (var label in new[] { "Offline recognizers:", "Installed voices:" })
        {
            var line = inventory.Split('\n').FirstOrDefault(l => l.StartsWith(label, StringComparison.Ordinal));
            if (line is null || string.IsNullOrWhiteSpace(line[label.Length..]))
                throw new IOException("Missing " + label.TrimEnd(':') + ". " + inventory);
        }
        return inventory + "\nInventory only; microphone capture and speech playback were not activated.";
    }
    public static SetupHealthCheck Storage(string world)
    {
        try
        {
            GuidedSetup.ValidatePath(world);
            // A unique, delete-on-close probe tests the selected directory without touching the world.
            var probe = Path.Combine(Path.GetDirectoryName(world)!, ".grid-health-" + Guid.NewGuid().ToString("N"));
            using var file = new FileStream(probe, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None,
                4096, FileOptions.DeleteOnClose);
            file.WriteByte(0x47); file.Flush(true); file.Position = 0;
            if (file.ReadByte() != 0x47) throw new IOException("Storage read-back failed.");
            if (File.Exists(world) && File.GetAttributes(world).HasFlag(FileAttributes.ReadOnly))
                throw new IOException("The selected world file is read-only.");
            return new("Storage", true, "Temporary write/read/delete probe passed; existing world was not opened. This does not prove SQLite lock availability.", "No repair needed.");
        }
        catch (Exception e) when (GuidedSetup.Expected(e))
        { return new("Storage", false, e.Message, "Choose a writable local folder/media or review Windows folder permissions; rerun --setup with a new profile. Keep the old world and profile for rollback."); }
    }

    public static async Task<IReadOnlyList<SetupHealthCheck>> RunAsync(string? runtime, string world, CancellationToken token,
        Func<CancellationToken, Task<string>>? voice = null, Func<CancellationToken, Task<string>>? screen = null,
        Func<CancellationToken, Task<string>>? ai = null, Func<string>? devices = null)
    {
        token.ThrowIfCancellationRequested();
        var checks = new List<SetupHealthCheck> { Storage(world) };
        async Task Probe(string name, Func<CancellationToken, Task<string>> action, string repair)
        {
            try { checks.Add(new(name, true, await action(token), "No repair needed.")); }
            catch (Exception e) when (GuidedSetup.Expected(e) || e is InvalidOperationException or System.ComponentModel.Win32Exception or OperationCanceledException)
            { token.ThrowIfCancellationRequested(); checks.Add(new(name, false, e.Message, repair)); }
        }
        await Probe("Voice inventory", voice ?? (async t => ValidateVoice(await new WindowsVoiceProvider().DescribeAsync(t))),
            "Install an offline Windows speech language/voice and check microphone privacy settings. Use voice devices, voice language and voice microphone after login. Typed conversation remains available.");
        await Probe("Screen OCR", screen ?? (async t => ValidateOcr(await new WindowsScreenContextProvider().VerifyOcrAsync(t))),
            "Restore the bundled Grid-ScreenContext.ps1 helper and Windows OCR language support. No desktop was captured; protected-window access requires a separate explicit capture review.");
        await Probe("Device broker", _ =>
        {
            if (devices is not null) return Task.FromResult(devices());
            var discovery = new WindowsDeviceIntegration().Discover(true);
            if (discovery.Incomplete) throw new IOException("Windows metadata discovery was incomplete; some applications were inaccessible.");
            return Task.FromResult($"Observed {discovery.Items.Count} window metadata entries. No titles read or actions authorized; live grants are still required.");
        },
            "Use an interactive Windows desktop session and device catalog after login. No app was launched, focused or inspected by this check.");
        await Probe("Local AI", ai ?? (async t =>
        {
            GuidedSetup.VerifyLite(runtime, GuidedSetup.Observe(world));
            var previous = AgentModelProfiles.ForceLite;
            try
            {
                AgentModelProfiles.ForceLite = true;
                await using var server = await BundledAiRuntime.StartAsync(runtime, t);
                if (!server.Ready) throw new IOException(server.Status);
                using var backend = new OllamaLocalBackend(endpoint: server.Endpoint);
                await using var inference = new SharedLocalInference(backend, enabled: true);
                var reply = await inference.GenerateAsync("Reply with exactly: GRID LOCAL READY", t);
                if (!reply.Available || string.IsNullOrWhiteSpace(reply.Text)) throw new IOException(reply.Reason);
                return $"Pinned local model returned text in {reply.ElapsedMilliseconds} ms. Output is not a correctness benchmark. Diagnostic child server stopped on completion.";
            }
            finally { AgentModelProfiles.ForceLite = previous; }
        }), "Review the startup cause; explicitly provision/repair managed assets with Setup-GridClone.ps1 or use a complete offline package. No global/cloud fallback. Prior world/profile retained.");
        return checks;
    }

    public static string Format(IEnumerable<SetupHealthCheck> checks) => string.Join("\n", checks.Select(c =>
        $"{c.Name}: {(c.Passed ? "PASS" : "FAIL")} — {TerminalTable.Safe(c.Detail)}\nRepair: {c.Repair}"));
}
