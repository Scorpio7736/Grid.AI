using System.Diagnostics;
using System.Text;
namespace Grid.Simulation.Tasks;

/// <summary>Trusted host adapter. Working copies are source protection, not an OS security boundary.</summary>
public sealed class MaxisRuntime(string artifactsRoot, string? bundledDotnetRoot = null)
{
    private readonly string artifacts = Path.GetFullPath(artifactsRoot);
    public string EditArtifactsRoot => DevelopmentPolicy.CheckedPath(artifacts, "workspace-edits");
    public string AttemptDirectory(TaskRecord task) => Path.Combine(artifacts, task.Id.ToString("N") + "-" + (task.Attempts + 1));
    public void CheckWorkspace(DevelopmentWorkspace workspace)
    {
        if (!workspace.Enabled || workspace.Machine != Environment.MachineName) throw new InvalidOperationException("Workspace revoked or registered on another machine. Register a new local workspace alias.");
        var root = DevelopmentPolicy.CheckedPath(workspace.Root);
        if (!Directory.Exists(root) || !File.Exists(DevelopmentPolicy.CheckedPath(root, workspace.Project))) throw new IOException("Registered workspace/project is missing.");
        var prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var outputPrefix = artifacts.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (artifacts.Equals(root, StringComparison.OrdinalIgnoreCase) || artifacts.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || root.StartsWith(outputPrefix, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Workspace and GRID task artifacts must be separate directories.");
    }
    public async Task<string> ExecuteAsync(DevelopmentWorkspace workspace, TaskRecord task, CancellationToken token)
    {
        CheckWorkspace(workspace); token.ThrowIfCancellationRequested();
        DevelopmentPolicy.CheckedPath(artifacts);
        var output = DevelopmentPolicy.CheckedPath(AttemptDirectory(task));
        if (Directory.Exists(output)) throw new IOException("Attempt output already exists; refusing to overwrite evidence.");
        Directory.CreateDirectory(output);
        var parts = task.Argument.Split('|');
        var report = Path.Combine(output, "report.txt");
        if (task.Capability is "maxis.inspect" or "maxis.suggest")
        {
            var path = DevelopmentPolicy.CheckedPath(workspace.Root, parts[1]);
            var extensions = new[] { ".cs", ".csproj", ".sln", ".slnx", ".props", ".targets", ".md", ".txt", ".json", ".xml", ".yml", ".yaml" };
            if (!extensions.Contains(Path.GetExtension(path).ToLowerInvariant()) || new FileInfo(path).Length > 1024 * 1024)
                throw new IOException("Only supported text source files up to 1 MiB can be inspected/staged.");
            var original = await File.ReadAllTextAsync(path, new UTF8Encoding(false, true), token);
            if (original.Contains('\0')) throw new IOException("Binary content is not supported.");
            if (task.Capability == "maxis.inspect")
            {
                await File.WriteAllTextAsync(report, original, token);
                return "Source inspected (" + original.Length + " characters). Full source report: " + report;
            }
            var index = original.IndexOf(parts[2], StringComparison.Ordinal);
            if (index < 0 || original.IndexOf(parts[2], index + parts[2].Length, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException("Suggestion requires exactly one matching literal; no files changed.");
            var candidate = original[..index] + parts[3] + original[(index + parts[2].Length)..];
            var working = Path.Combine(output, "working-copy"); Directory.CreateDirectory(working);
            var staged = DevelopmentPolicy.CheckedPath(working, parts[1]); Directory.CreateDirectory(Path.GetDirectoryName(staged)!);
            await File.WriteAllTextAsync(staged, candidate, token);
            await File.WriteAllTextAsync(Path.Combine(output, "original.txt"), original, token);
            await File.WriteAllTextAsync(report, $"EXPLICIT REPLACEMENT SUGGESTION - original source unchanged.\nSource: {path}\nWorking copy: {staged}\nReplace: {parts[2]}\nWith: {parts[3]}\nNo automatic apply or generated correctness claim.", token);
            return "Suggested replacement staged; original source unchanged. Review: " + report;
        }
        var dotnet = bundledDotnetRoot is null ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "dotnet.exe") : Path.Combine(bundledDotnetRoot,"dotnet.exe");
        if (!File.Exists(dotnet)) throw new IOException("The configured .NET SDK is missing; provision GRID runtime assets before running MAXIS builds/tests.");
        var start = new ProcessStartInfo(dotnet) { WorkingDirectory = workspace.Root, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        if (bundledDotnetRoot is not null)
        {
            var root=Path.GetDirectoryName(bundledDotnetRoot)!;
            start.Environment["DOTNET_ROOT"]=bundledDotnetRoot;
            start.Environment["DOTNET_CLI_HOME"]=Path.Combine(root,"home");
            start.Environment["NUGET_PACKAGES"]=Path.Combine(root,"nuget");
            start.Environment["PATH"]=bundledDotnetRoot + Path.PathSeparator + start.Environment["PATH"];
        }
        foreach (var arg in new[] { task.Capability == "maxis.build" ? "build" : "test", DevelopmentPolicy.CheckedPath(workspace.Root,workspace.Project), "--no-restore", "--configuration", "Debug", "--verbosity", "minimal", "--disable-build-servers" }) start.ArgumentList.Add(arg);
        if (task.Capability == "maxis.test") { start.ArgumentList.Add("--logger"); start.ArgumentList.Add("trx;LogFileName=tests.trx"); start.ArgumentList.Add("--results-directory"); start.ArgumentList.Add(output); }
        start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"; start.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
        await File.WriteAllTextAsync(report, $"Capability: {task.Capability}\nProject: {workspace.Project}\nStarted UTC: {DateTimeOffset.UtcNow:O}\nProject-defined code runs as the current OS user; this is not a security sandbox.\n", token);
        using var process = new Process { StartInfo = start };
        token.ThrowIfCancellationRequested(); process.Start();
        using var drainStop = new CancellationTokenSource();
        async Task Drain(StreamReader reader, string name)
        {
            await using var writer = new StreamWriter(Path.Combine(output,name)); var buffer = new char[4096]; var kept = 0; int count;
            while ((count = await reader.ReadAsync(buffer.AsMemory(), drainStop.Token)) > 0)
            {
                var take = Math.Min(count, Math.Max(0, 1024 * 1024 - kept));
                if (take > 0) { await writer.WriteAsync(buffer.AsMemory(0,take)); kept += take; }
            }
            if (kept == 1024 * 1024) await writer.WriteLineAsync("\n[Capture limit reached; remaining output discarded.]");
        }
        var stdout = Drain(process.StandardOutput,"stdout.txt"); var stderr = Drain(process.StandardError,"stderr.txt");
        try { await process.WaitForExitAsync(token); }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree:true);
            await process.WaitForExitAsync(CancellationToken.None);
            await File.AppendAllTextAsync(report, "Attempt cancelled/timed out; process tree termination requested.\n", CancellationToken.None);
            throw;
        }
        finally
        {
            // Descendants may inherit pipe handles; do not block shutdown indefinitely on their EOF.
            drainStop.CancelAfter(TimeSpan.FromSeconds(2));
            try { await Task.WhenAll(stdout,stderr); }
            catch (OperationCanceledException) { await File.AppendAllTextAsync(report,"Log drain timed out; captured output may be incomplete.\n",CancellationToken.None); }
        }
        await File.AppendAllTextAsync(report, $"Exit code: {process.ExitCode}\nFinished UTC: {DateTimeOffset.UtcNow:O}\n", CancellationToken.None);
        if (process.ExitCode != 0) throw new IOException("Build/test exited " + process.ExitCode + "; report: " + report);
        return "Process exited 0. Verify diagnostics/test counts in artifacts; no claim that tests were discovered. Report: " + report;
    }
}
