using System.Diagnostics;

namespace Grid.Terminal;

public sealed record SetupInstallPlan(string Root, bool Download, long ExistingBytes, long RequiredFreeBytes, long? FreeBytes)
{
    public bool HasSpace => FreeBytes >= RequiredFreeBytes;
    public override string ToString() => $"Installer: {Path.Combine(Root, "Setup-GridClone.ps1")}\n" +
        $"Network: {(Download ? "explicit online downloads of pinned dependencies and NuGet packages" : "offline cached assets only; package sources cleared")}\n" +
        $"Existing managed runtime/app/data/staging: {ExistingBytes:N0} bytes (retained).\n" +
        $"Required additional free staging/build space: {RequiredFreeBytes:N0} bytes; available: {FreeBytes?.ToString("N0") ?? "unknown"}.\n" +
        "The 20 GB staging allowance includes archives, extracted SDK/runtime/model, build/cache growth and reserve; it is a conservative admission budget, not a download-size promise.\n" +
        "No existing world is imported or deleted. Failed staging is retained. No automatic repair/replacement of an existing runtime.";
}

public static class SetupInstallation
{
    public const long StagingBudget = 20_000_000_000;
    public static string? FindClone(string start)
    {
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "Grid.AI.slnx")) && File.Exists(Path.Combine(dir.FullName, "Setup-GridClone.ps1")))
                return dir.FullName;
        return null;
    }
    public static SetupInstallPlan Plan(string root, bool download)
    {
        root = Path.GetFullPath(root); GridTransfer.CheckPath(root);
        if (!File.Exists(Path.Combine(root, "Setup-GridClone.ps1"))) throw new IOException("Clone installer not found. Portable packages must be provisioned before transfer.");
        long bytes = 0;
        foreach (var directory in Directory.EnumerateDirectories(root).Where(p =>
            Path.GetFileName(p) is ".grid-runtime" or ".grid-app" or ".grid-data" || Path.GetFileName(p).StartsWith(".grid-install-", StringComparison.Ordinal)))
            bytes = checked(bytes + PackageBudget.Measure(directory).TotalBytes);
        return new(root, download, bytes, StagingBudget, GuidedSetup.Observe(root).FreeDisk);
    }
    public static ProcessStartInfo Command(SetupInstallPlan plan)
    {
        var start = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe"))
        { UseShellExecute = false, WorkingDirectory = plan.Root };
        foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-File", Path.Combine(plan.Root, "Setup-GridClone.ps1") }) start.ArgumentList.Add(argument);
        if (plan.Download) start.ArgumentList.Add("-Download");
        start.Environment.Remove("PSModulePath");
        return start;
    }
    public static async Task<bool> ReviewAsync(SetupInstallPlan plan, TextReader input, TextWriter output,
        Func<ProcessStartInfo, Task<int>>? execute = null)
    {
        await output.WriteLineAsync(plan.ToString());
        if (!plan.HasSpace) throw new IOException("Installation refused: insufficient or unknown staging space. Existing files retained.");
        await output.WriteLineAsync("Type install to run this exact plan; anything else skips installation.");
        if (input.ReadLine()?.Trim() != "install") return false;
        // Recheck volume/path state after review, before invoking a script with side effects.
        GridTransfer.CheckPath(plan.Root);
        if (execute is null && !(GuidedSetup.Observe(plan.Root).FreeDisk >= plan.RequiredFreeBytes))
            throw new IOException("Free space changed since review; installation cancelled.");
        var code = await (execute ?? RunAsync)(Command(plan));
        if (code != 0) throw new IOException($"Installer failed (exit {code}). Previous worlds retained; inspect the installer output. No profile saved. Offline failures never retry online.");
        return true;
    }
    private static async Task<int> RunAsync(ProcessStartInfo start)
    {
        using var process = Process.Start(start) ?? throw new IOException("Installer could not start.");
        await process.WaitForExitAsync(); return process.ExitCode;
    }
    public static ProcessStartInfo LaunchCommand(string profile)
    {
        var executable = Environment.ProcessPath ?? throw new IOException("Executable path unavailable.");
        var start = new ProcessStartInfo(executable) { UseShellExecute = false };
        if (Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            start.ArgumentList.Add(typeof(GuidedSetup).Assembly.Location);
        start.ArgumentList.Add("--configured"); start.ArgumentList.Add(profile);
        return start;
    }
    public static Task<int> LaunchAsync(string profile) => RunAsync(LaunchCommand(profile));
}
