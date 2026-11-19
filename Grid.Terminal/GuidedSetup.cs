using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Grid.Communication;

namespace Grid.Terminal;

public sealed record SetupProfile(int Version, string Machine, string Mode, string WorldPath, string AiMode);
public sealed record SetupCapacity(long? AvailableRam, long? FreeDisk, int CpuThreads, bool SupportedHost);

/// <summary>Explicit local setup. Profiles contain launch preferences, never identity or capability grants.</summary>
public static class GuidedSetup
{
    public const long MinimumRam = 8_000_000_000;
    public const long DiskReserve = 256_000_000;
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GRID", "setup.json");
    public const string Permissions = "Identity: create/login with the existing secure local credential prompt; never put passwords in launch arguments. Store your password in your own password manager. Setup profiles contain no recovery key. Offline owner rescue requires separately enrolled user-held material. --recover restores the world backup, not a forgotten password.\n" +
        "Optional permissions are skipped by default. Setup grants no file, microphone, screen, process or network access to agents.\n" +
        "Voice: explicit voice listen/review/send; voice cancel discards an unsent draft, voice mute on disables playback; Esc stops active recording/playback. Revoke microphone access in Windows privacy settings.\n" +
        "Screen: screen options lists displays; screen monitoring selects one; screen ask explicitly captures one frame. screen stop cancels pending work and clears selection. No background watching.\n" +
        "Files: register only chosen workspace/storage paths and review each change. Disable those registrations to revoke access.\n" +
        "Device: device catalog explains limits; device revoke <review-id> cancels a pending grant.\n" +
        "Connectivity: inference uses bundled loopback services. Internet is not required after provisioning; no cloud provider or automatic downloads.\n" +
        "Later changes: rerun --setup with a new profile filename. Rollback: launch without --configured to use the prior default world, or select your prior profile. Existing worlds are not moved or deleted.";

    public static SetupCapacity Observe(string location)
    {
        long? disk = null;
        try { disk = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(location))!).AvailableFreeSpace; }
        catch (Exception e) when (e is InvalidDataException or IOException or UnauthorizedAccessException or ArgumentException) { }
        return new(WindowsHostTelemetryProvider.AvailablePhysicalMemory(), disk, Environment.ProcessorCount,
            OperatingSystem.IsWindows() && RuntimeInformation.ProcessArchitecture == Architecture.X64);
    }
    public static string Choices(SetupCapacity capacity) =>
        $"CPU logical threads: {capacity.CpuThreads}; available RAM: {capacity.AvailableRam?.ToString("N0") ?? "unknown"} bytes; storage free: {capacity.FreeDisk?.ToString("N0") ?? "unknown"} bytes.\n" +
        "Confidence: point-in-time OS counters, not a throughput benchmark. RAM/VRAM are not pooled.\n" +
        $"Lite: {(CanUseLite(capacity) ? "eligible for verified baseline" : "unavailable or insufficient/unknown resources")}; conservative budget 8 GB available RAM plus 256 MB free storage reserve. Model/runtime installation needs additional space.\n" +
        "Standard: unavailable until model-specific device acceptance. Performance: unavailable until model-specific GPU/runtime acceptance.\n" +
        "Only the pinned Qwen3 4B baseline can be selected here; Lite requests reduced context and CPU-only inference. Off keeps deterministic GRID available.";
    public static bool CanUseLite(SetupCapacity capacity) => capacity.SupportedHost && capacity.AvailableRam >= MinimumRam && capacity.FreeDisk >= DiskReserve;
    public static bool OfferFirstRun(string[] args, bool interactive, string world) => args.Length == 0 && interactive && !File.Exists(world);

    public static string RecoveryGuidance(string world) =>
        "Local identity: a new world asks for a username and masked password when launched. Existing worlds require their existing credentials; setup cannot reset them.\n" +
        "After login, credentials changes your password and revokes existing sessions. Keep your password outside the setup profile.\n" +
        "World recovery (not password recovery): close all sessions and use --recover --world \"" + world + "\" to restore the existing backup. This rolls back changes since that backup; it is never run automatically.\n" +
        "Preference rollback: launch with your earlier profile. Choosing another database does not move or restore the previous world.";

    public static void Validate(SetupProfile profile, string machine, string? packageRoot)
    {
        if (profile is null || profile.Version != 1 || profile.Machine != machine || profile.Mode is not ("pc" or "flash") || profile.AiMode is not ("off" or "lite"))
            throw new InvalidDataException("Unsupported setup profile or changed device. Run --setup on this device; no grants are portable.");
        if (profile.Mode == "flash")
        {
            if (packageRoot is null || profile.WorldPath != "data/world.db")
                throw new InvalidDataException("Flash setup requires a complete portable package and its relative data/world.db path.");
            GridTransfer.CheckPath(Path.Combine(packageRoot, "data", "world.db"));
        }
        else ValidatePath(profile.WorldPath);
    }
    public static void ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > 1024 || path.Any(char.IsControl) || !Path.IsPathFullyQualified(path) ||
            path.StartsWith("\\\\", StringComparison.Ordinal) || path.Contains('"') || !path.EndsWith(".db", StringComparison.OrdinalIgnoreCase) ||
            path.IndexOf(':', Math.Min(2, path.Length)) >= 0)
            throw new InvalidDataException("Choose an absolute local .db path; network paths, streams and control characters are unsupported.");
        GridTransfer.CheckPath(path);
        if (!Directory.Exists(Path.GetDirectoryName(path))) throw new IOException("Choose an existing parent directory. Setup does not create or migrate a world.");
    }
    public static SetupProfile Read(string path, string machine, string? packageRoot)
    {
        GridTransfer.CheckPath(path);
        if (new FileInfo(path).Length > 8192) throw new InvalidDataException("Setup profile exceeds 8 KiB.");
        var profile = JsonSerializer.Deserialize<SetupProfile>(File.ReadAllBytes(path), Json) ?? throw new InvalidDataException("Empty setup profile.");
        Validate(profile, machine, packageRoot);
        return profile;
    }
    public static string ResolveWorld(SetupProfile profile, string? packageRoot) => profile.Mode == "flash"
        ? Path.Combine(packageRoot ?? throw new InvalidDataException("Portable package missing."), "data", "world.db") : profile.WorldPath;

    public static void SaveNew(string path, SetupProfile profile)
    {
        if (!Path.IsPathFullyQualified(path)) throw new IOException("Use an absolute setup profile path.");
        GridTransfer.CheckPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        // CreateNew prevents an accidental edit or rollback from overwriting prior configuration.
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        JsonSerializer.Serialize(stream, profile, Json); stream.Flush(true);
    }
    public static void VerifyLite(string? runtime, SetupCapacity capacity)
    {
        if (!CanUseLite(capacity)) throw new IOException("Lite requires Windows x64, 8 GB available RAM and 256 MB free reserve. Choose off or free resources and retry.");
        if (runtime is null || !File.Exists(Path.Combine(runtime, "assets.json")) || !File.Exists(Path.Combine(runtime, "ollama", "ollama.exe")))
            throw new IOException("Managed runtime missing. Choose the reviewed installer in --setup, run Setup-GridClone.ps1 -Download explicitly, or use Prepare-GridRuntime.ps1 with selected offline assets/a complete package. Nothing downloaded.");
        PackageBudget.VerifyModel(runtime, hashes: true);
    }
    public static async Task<string> DiagnoseAsync(string? runtime, string storage, CancellationToken token)
    {
        var lines = new List<string> { "GRID SETUP HEALTH (read-only; no repairs or grants)", Choices(Observe(storage)) };
        if (runtime is null) lines.Add("AI: no managed runtime found. Repair: provision verified local assets or copy a complete offline package.");
        else
        {
            lines.Add(await ModelDeviceDiscovery.DescribeAsync(runtime, token));
            try { VerifyLite(runtime, Observe(storage)); lines.Add("AI: pinned model SHA256 verified; engine file present. Run ai test after login for actual inference. Engine executable authenticity is not established by model hashes."); }
            catch (Exception e) when (Expected(e)) { lines.Add("AI: unavailable: " + e.Message); }
            lines.Add("Development SDK: " + (File.Exists(Path.Combine(runtime, "dotnet", "dotnet.exe")) ? "file present; build/test still requires an approved workspace" : "missing; provision the managed SDK for MAXIS build/test"));
        }
        lines.Add("Storage: free-space observation only; write permission/read-only media not probed. Resolve access before saving a profile or world.");
        lines.Add("Voice: not probed; use voice devices for installed offline recognizer/device status. No recording started.");
        lines.Add("Screen: not captured; select a window explicitly. Protected windows may refuse access.");
        lines.Add("Capability broker: use device catalog after login. No capabilities enabled by diagnostics.");
        lines.Add(Permissions);
        return string.Join("\n", lines);
    }
    public static async Task<int> RunAsync(string[] args, TextReader input, TextWriter output, Func<string, Task<int>>? launch = null)
    {
        try
        {
            if (args.Length is < 1 or > 2 || args[0] is not ("--setup" or "--doctor" or "--doctor-check") || args[0] == "--doctor-check" && args.Length != 2)
                throw new ArgumentException("Usage: --setup [absolute-new-profile.json] | --doctor [profile.json] | --doctor-check <profile.json> | --configured <profile.json>");
            var runtime = BundledAiRuntime.FindRoot(AppContext.BaseDirectory);
            var package = GridTransfer.PackageRoot(AppContext.BaseDirectory);
            var path = args.Length == 2 ? args[1] : DefaultPath;
            if (args[0] is "--doctor" or "--doctor-check")
            {
                var storage = args.Length == 2 ? ResolveWorld(Read(path, Environment.MachineName, package), package) : AppContext.BaseDirectory;
                if (args[0] == "--doctor") { await output.WriteLineAsync(await DiagnoseAsync(runtime, storage, default)); return 0; }
                await output.WriteLineAsync("Explicit health checks: temporary storage probe, offline voice inventory, synthetic OCR and local inference. No world opened, desktop captured, audio recorded or grants changed. Runtime logs/cache may be written. Ctrl+C cancels.");
                using var stop = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
                Console.CancelKeyPress += cancel;
                try
                {
                    var checks = await SetupHealth.RunAsync(runtime, storage, stop.Token);
                    await output.WriteLineAsync(SetupHealth.Format(checks));
                    return checks.All(c => c.Passed) ? 0 : 1;
                }
                finally { Console.CancelKeyPress -= cancel; }
            }
            await output.WriteLineAsync("GUIDED SETUP - no world opened and no downloads unless explicitly reviewed. Enter skip at any prompt to cancel.\n" + Permissions);
            await output.WriteLineAsync("Deployment: pc (permanent PC) or flash (existing complete portable package):");
            var mode = input.ReadLine()?.Trim().ToLowerInvariant();
            if (mode is null or "skip") return 0;
            if (mode is not ("pc" or "flash")) throw new ArgumentException("Choose pc or flash.");
            var world = "data/world.db";
            if (mode == "pc")
            {
                await output.WriteLineAsync("World database: enter an absolute .db path in an existing folder. Existing databases are loaded only at launch; no migration occurs now.");
                world = input.ReadLine()?.Trim() ?? "skip";
                if (world == "skip") return 0;
            }
            var profile = new SetupProfile(1, Environment.MachineName, mode, world, "off");
            Validate(profile, Environment.MachineName, package);
            var resolved = ResolveWorld(profile, package);
            var capacity = Observe(resolved);
            await output.WriteLineAsync(Choices(capacity));
            if (runtime is not null) await output.WriteLineAsync(await ModelDeviceDiscovery.DescribeAsync(runtime, default));
            var clone = SetupInstallation.FindClone(AppContext.BaseDirectory);
            if (mode == "pc" && clone is not null)
            {
                await output.WriteLineAsync("Dependencies: keep (default, already provisioned/offline), offline (build from verified local cache), or download (review online installer). No automatic retry online.");
                var installation = input.ReadLine()?.Trim().ToLowerInvariant();
                if (installation is null or "skip") return 0;
                if (installation is not ("" or "keep" or "offline" or "download")) throw new ArgumentException("Choose keep, offline or download.");
                if (installation is "offline" or "download")
                {
                    if (Path.GetFullPath(AppContext.BaseDirectory).StartsWith(Path.Combine(clone, ".grid-app") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                        throw new IOException("Close this installed GRID before rebuilding it. Run Setup-GridClone.ps1 from PowerShell at the clone root (add -Download only for online installation), then rerun setup. No installer started.");
                    await SetupInstallation.ReviewAsync(SetupInstallation.Plan(clone, installation == "download"), input, output);
                    runtime = BundledAiRuntime.FindRoot(AppContext.BaseDirectory);
                    capacity = Observe(resolved);
                }
            }
            await output.WriteLineAsync("Local AI: off (default) or lite (verify pinned installed model now):");
            var ai = input.ReadLine()?.Trim().ToLowerInvariant();
            if (ai is null or "skip") return 0;
            if (ai == "") ai = "off";
            profile = profile with { AiMode = ai };
            Validate(profile, Environment.MachineName, package);
            if (ai == "lite") VerifyLite(runtime, capacity);
            await output.WriteLineAsync(RecoveryGuidance(resolved));
            await output.WriteLineAsync("Optional capabilities: Enter/skip leaves all grants off; type review to read permission and revoke instructions. Setup does not authorize any capability.");
            var permissions = input.ReadLine()?.Trim().ToLowerInvariant();
            if (permissions is null) return 0;
            if (permissions == "review") await output.WriteLineAsync(Permissions);
            else if (permissions is not ("" or "skip")) throw new ArgumentException("Choose review, skip or Enter.");
            await output.WriteLineAsync("REVIEW\n" + JsonSerializer.Serialize(profile, Json) + "\nProfile file: " + path + "\nOptional capabilities: skipped, no grants. Type save to create this new profile; anything else cancels.");
            if (input.ReadLine()?.Trim() != "save") return 0;
            SaveNew(path, profile);
            await output.WriteLineAsync("Saved. Launch explicitly with --configured \"" + path + "\". Use --doctor for health diagnostics. No world or credential changed.");
            await output.WriteLineAsync("Type launch to open this world and continue secure identity setup/login now; Enter finishes without launching.");
            if (input.ReadLine()?.Trim() == "launch") return await (launch ?? SetupInstallation.LaunchAsync)(path);
            return 0;
        }
        catch (Exception e) when (Expected(e)) { await output.WriteLineAsync("Setup stopped: " + e.Message + " Previous configuration/world retained."); return 1; }
        catch (OperationCanceledException) { await output.WriteLineAsync("Health checks cancelled or timed out. Existing world and grants retained."); return 1; }
    }
    public static bool Expected(Exception e) => e is InvalidDataException or IOException or UnauthorizedAccessException or ArgumentException or JsonException or NotSupportedException;
}
