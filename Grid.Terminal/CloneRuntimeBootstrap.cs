using System.Diagnostics;

namespace Grid.Terminal;

public static class CloneRuntimeBootstrap
{
    public static bool Ready(string? root)
    {
        if (root is null) return false;
        try { PortableAssets.ValidateRuntime(root, hashes: false); return true; }
        catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException or System.Text.Json.JsonException) { return false; }
    }

    public static async Task<bool> EnsureAsync(string applicationDirectory, TextReader input, TextWriter output,
        bool interactive, CancellationToken token = default)
    {
        if (Environment.GetEnvironmentVariable("GRID_AI_DISABLED") == "1") return true;
        var runtime = BundledAiRuntime.FindRoot(applicationDirectory);
        if (Ready(runtime)) return true;
        var repository = FindRepository(applicationDirectory);
        if (repository is null)
        {
            await output.WriteLineAsync("Grid.AI cannot start: its private local-AI runtime is incomplete and this is not a repairable source clone.");
            await output.WriteLineAsync("Restore or recopy the complete Grid.AI package. No world was opened.");
            return false;
        }
        var repair = runtime is not null && Directory.Exists(runtime);
        await output.WriteLineAsync("GRID.AI // FIRST-RUN LOCAL AI SETUP");
        await output.WriteLineAsync("Grid.AI requires its private offline AI runtime and model (about 6 GB for the current baseline).");
        await output.WriteLineAsync("This one-time setup downloads pinned files, verifies their hashes, builds Grid.AI, and runs a real MCP reply test before any world opens.");
        if (!interactive)
        {
            await output.WriteLineAsync("Interactive approval is required. From the repository root run:");
            await output.WriteLineAsync(repair ? ".\\Setup-GridClone.ps1 -Download -RepairRuntime" : ".\\Setup-GridClone.ps1 -Download");
            await output.WriteLineAsync("Grid.AI was not started. No world was opened.");
            return false;
        }
        await output.WriteAsync("Download and verify the required local AI assets now? [Y/n] ");
        await output.FlushAsync();
        var answer = await input.ReadLineAsync(token);
        if (answer is not null && answer.Trim().Equals("n", StringComparison.OrdinalIgnoreCase))
        {
            await output.WriteLineAsync("Setup cancelled. Grid.AI was not started and no world was opened.");
            return false;
        }
        var script = Path.Combine(repository, "Setup-GridClone.ps1");
        var start = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false,
            WorkingDirectory = repository
        };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-ExecutionPolicy");
        start.ArgumentList.Add("Bypass");
        start.ArgumentList.Add("-File");
        start.ArgumentList.Add(script);
        start.ArgumentList.Add("-Download");
        if (repair) start.ArgumentList.Add("-RepairRuntime");
        using var process = Process.Start(start) ?? throw new IOException("Grid.AI setup process could not start.");
        await process.WaitForExitAsync(token);
        if (process.ExitCode != 0 || !Ready(BundledAiRuntime.FindRoot(applicationDirectory)))
        {
            await output.WriteLineAsync("Grid.AI setup did not complete. The staged files and existing world were retained; Grid.AI was not started.");
            return false;
        }
        await output.WriteLineAsync("Grid.AI local AI setup verified. Starting Grid.AI.");
        return true;
    }

    private static string? FindRepository(string start)
    {
        for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
            if ((File.Exists(Path.Combine(directory.FullName, GridBrand.Solution)) || File.Exists(Path.Combine(directory.FullName, GridBrand.LegacySolution))) &&
                File.Exists(Path.Combine(directory.FullName, "Setup-GridClone.ps1"))) return directory.FullName;
        return null;
    }
}
