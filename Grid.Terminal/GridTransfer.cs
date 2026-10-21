using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
namespace Grid.Terminal;

public sealed record TransferRequest(string Destination, bool Move);
public sealed record TransferFile(string Source, string Relative, long Length, string Hash = "");
public sealed record TransferPlan(string Destination, bool Move, string ApplicationRoot, string WorldPath, TransferFile[] Files);
public sealed record TransferReceipt(TransferPlan Plan, int ParentId, long ParentStarted);

/// <summary>Transfers a stopped, saved installation. Source cleanup is deferred until the parent exits.</summary>
public sealed class GridTransfer(string applicationRoot, string worldPath)
{
    public const string Marker = "grid-package.json";
    public static void Register(CommandDispatcher commands)
    {
        foreach (var name in new[] { "tranexist", "copexist" })
            commands.Register(new(name, name + " \"location\"", "Master Control: " + (name == "tranexist" ? "move" : "copy") + " GRID to a new folder; confirm, save and close this terminal.",
                a => a.Count != 1 || string.IsNullOrWhiteSpace(a[0]) ? new("Usage: " + name + " \"location\"") : new("", Transfer: new(a[0], name == "tranexist"))));
    }
    public static string? PackageRoot(string app) => File.Exists(Path.Combine(app, "..", Marker)) ? Path.GetFullPath(Path.Combine(app, "..")) : null;
    public static string Hash(string path) { using var stream = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(stream)); }
    public static bool Within(string path, string root) => path.Equals(root, StringComparison.OrdinalIgnoreCase) || path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    public static void CheckPath(string path)
    {
        for (var current = Path.GetFullPath(path); current is not null; current = Path.GetDirectoryName(current))
            if ((File.Exists(current) || Directory.Exists(current)) && File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
                throw new IOException("Symbolic links/junctions are not supported: " + current);
    }
    public TransferPlan Prepare(TransferRequest request)
    {
        var app = Path.TrimEndingDirectorySeparator(Path.GetFullPath(applicationRoot));
        var world = Path.GetFullPath(worldPath);
        var destination = Path.TrimEndingDirectorySeparator(Path.GetFullPath(request.Destination));
        if (!Path.IsPathFullyQualified(request.Destination) || destination == Path.GetPathRoot(destination))
            throw new IOException("Choose a new absolute destination folder, not a filesystem root.");
        CheckPath(app); CheckPath(world); CheckPath(destination);
        if (Directory.Exists(destination) || File.Exists(destination)) throw new IOException("Destination must not already exist; existing files are never overwritten.");
        var sourceRoot = PackageRoot(app) ?? app;
        if (Within(destination, sourceRoot) || Within(sourceRoot, destination) || Within(world, destination) || Within(destination, Path.GetDirectoryName(world)!))
            throw new IOException("Destination overlaps the application or world source.");
        if (!Directory.Exists(Path.GetDirectoryName(destination))) throw new IOException("Destination parent folder must exist.");
        var files = new List<TransferFile>();
        void Add(string source, string relative)
        {
            CheckPath(source);
            if (source.EndsWith(".lock", StringComparison.OrdinalIgnoreCase)) return;
            files.Add(new(source, relative, new FileInfo(source).Length));
        }
        void Tree(string folder, string relative)
        {
            CheckPath(folder);
            foreach (var file in Directory.GetFiles(folder)) Add(file, Path.Combine(relative, Path.GetFileName(file)));
            foreach (var child in Directory.GetDirectories(folder)) Tree(child, Path.Combine(relative, Path.GetFileName(child)));
        }
        Tree(app, "app");
        // Transfer package-owned models/config/assets/logs too; data is inventoried below as a complete package directory.
        if (sourceRoot != app)
        {
            foreach (var folder in Directory.GetDirectories(sourceRoot).Where(p => !p.Equals(app, StringComparison.OrdinalIgnoreCase)))
                Tree(folder, Path.GetFileName(folder));
            foreach (var file in Directory.GetFiles(sourceRoot).Where(p => Path.GetFileName(p) is not Marker and not "launch.cmd" && !Path.GetFileName(p).StartsWith("transfer-"))) Add(file, Path.GetFileName(file));
            if (!world.Equals(Path.Combine(sourceRoot, "data", "world.db"), StringComparison.OrdinalIgnoreCase)) throw new IOException("Package transfer requires its own data/world.db; custom external worlds must be transferred from an installed session.");
        }
        else
        {
            var dir = Path.GetDirectoryName(world)!; var name = Path.GetFileName(world);
            foreach (var file in Directory.GetFiles(dir).Where(f => Path.GetFileName(f) == name || Path.GetFileName(f).StartsWith(name + ".", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(f).StartsWith(name + "-", StringComparison.OrdinalIgnoreCase)))
                Add(file, Path.Combine("data", "world.db" + Path.GetFileName(file)[name.Length..]));
        }
        if (!files.Any(f => f.Source.Equals(world, StringComparison.OrdinalIgnoreCase))) throw new IOException("World save is missing.");
        if (files.Select(f => f.Relative).Distinct(StringComparer.OrdinalIgnoreCase).Count() != files.Count) throw new IOException("Transfer file mapping collides.");
        var drive = new DriveInfo(Path.GetPathRoot(destination)!);
        if (drive.AvailableFreeSpace < files.Sum(f => f.Length) + 16L * 1024 * 1024) throw new IOException("Insufficient destination space.");
        return new(destination, request.Move, app, world, files.ToArray());
    }
    public static string Preview(TransferPlan p) => $"{(p.Move ? "MOVE" : "COPY")} GRID\nApplication: {p.ApplicationRoot}\nWorld: {p.WorldPath}\nDestination: {p.Destination}\n{p.Files.Length} files; {p.Files.Sum(f => f.Length):N0} bytes before final save.\n" +
        "Scope: application and bundled files, selected save/backups/archives; existing package folders if present.\nUnrelated host files, source repository, other installed worlds and user PATH launcher are excluded.\nBoth commands save and close this terminal. Destination uses launch.cmd, requires the installed .NET runtime if not bundled, and requires login.\n" +
        (p.Move ? "Move cleanup runs after exit; consult transfer-status.txt at the destination. Existing GRID-managed PATH launcher is retargeted only if it points to this installation.\n" : "Original files remain. The copy is an independent saved world.\n") + "Continue? [y/N]";

    public string Execute(TransferPlan confirmed, Func<bool> authorized, Action<string>? launchCleanup = null)
    {
        if (!authorized()) throw new UnauthorizedAccessException("Transfer authorization expired or was revoked.");
        var current = Prepare(new(confirmed.Destination, confirmed.Move));
        if (!current.Files.Select(f => (f.Source, f.Relative)).SequenceEqual(confirmed.Files.Select(f => (f.Source, f.Relative))))
            throw new IOException("File inventory changed since confirmation; retry after reviewing the new scope.");
        var stage = current.Destination + ".staging-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(stage);
        try
        {
            var verified = new List<TransferFile>();
            foreach (var file in current.Files)
            {
                CheckPath(file.Source);
                var target = Path.Combine(stage, file.Relative); Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file.Source, target, false);
                var hash = Hash(target);
                if (hash != Hash(file.Source)) throw new IOException("Source changed during transfer: " + file.Source);
                verified.Add(file with { Hash = hash, Length = new FileInfo(target).Length });
            }
            // Validate the copied save through the real loader before publishing a launchable package.
            using (var store = new Grid.Persistence.WorldStore(Path.Combine(stage, "data", "world.db"))) store.Load();
            File.Delete(Path.Combine(stage, "data", "world.db.lock"));
            File.WriteAllText(Path.Combine(stage, Marker), "{\"version\":1}");
            File.WriteAllText(Path.Combine(stage, "launch.cmd"), "@echo off\r\nsetlocal DisableDelayedExpansion\r\ndotnet \"%~dp0app\\Grid.Terminal.dll\" %*\r\nexit /b %errorlevel%\r\n");
            var receipt = new TransferReceipt(current with { Files = verified.ToArray() }, Environment.ProcessId, Process.GetCurrentProcess().StartTime.ToUniversalTime().Ticks);
            File.WriteAllText(Path.Combine(stage, "transfer-manifest.json"), JsonSerializer.Serialize(receipt, new JsonSerializerOptions { WriteIndented = true }));
            File.WriteAllText(Path.Combine(stage, "transfer-status.txt"), current.Move ? "Verified destination. Source cleanup pending parent exit. Do not start destination until cleanup completes." : "Copy complete. Original retained.");
            if (!authorized()) throw new UnauthorizedAccessException("Transfer authorization revoked before publication.");
            CheckPath(current.Destination); Directory.Move(stage, current.Destination);
            if (current.Move)
            {
                try { (launchCleanup ?? LaunchCleanup)(current.Destination); }
                catch (Exception e) { File.WriteAllText(Path.Combine(current.Destination, "transfer-status.txt"), "Move incomplete: source retained; cleanup helper could not start. " + e.Message); throw; }
            }
            return current.Move ? "Destination verified; move cleanup pending after this process exits. See " + Path.Combine(current.Destination, "transfer-status.txt") : "Copy complete: " + current.Destination;
        }
        catch
        {
            // Keep failed staging for diagnosis; never erase source on failed verification.
            if (Directory.Exists(stage)) File.WriteAllText(Path.Combine(stage, "transfer-status.txt"), "Incomplete transfer; source retained. This staging folder is not a completed package.");
            throw;
        }
    }
    private static void LaunchCleanup(string destination)
    {
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden };
        start.ArgumentList.Add(Path.Combine(destination, "app", "Grid.Terminal.dll")); start.ArgumentList.Add("--finish-move");
        using var process = Process.Start(start) ?? throw new IOException("Cannot start move cleanup helper.");
    }
    public static async Task<int> FinishMoveAsync(string app)
    {
        var root = PackageRoot(app) ?? throw new IOException("Move helper requires a transferred package.");
        var status = Path.Combine(root, "transfer-status.txt");
        try
        {
            var receipt = JsonSerializer.Deserialize<TransferReceipt>(File.ReadAllText(Path.Combine(root, "transfer-manifest.json"))) ?? throw new IOException("Missing receipt.");
            if (!receipt.Plan.Move || !root.Equals(receipt.Plan.Destination, StringComparison.OrdinalIgnoreCase)) throw new IOException("Receipt destination mismatch.");
            try
            {
                using var parent = Process.GetProcessById(receipt.ParentId);
                if (parent.StartTime.ToUniversalTime().Ticks != receipt.ParentStarted) throw new IOException("Parent process identity changed.");
                await parent.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(2));
            }
            catch (ArgumentException) { } // Parent already exited.
            Cleanup(receipt.Plan);
            RetargetLauncher(receipt.Plan);
            File.WriteAllText(status, "Move complete. Source inventory removed; empty source directories may remain. Launch this package with launch.cmd.");
            return 0;
        }
        catch (Exception e) { File.WriteAllText(status, "Move cleanup incomplete; destination retained. Remaining source files retained. " + e.Message); return 1; }
    }
    public static void Cleanup(TransferPlan plan)
    {
        // Reacquire the original world lease so a restarted source GRID prevents cleanup.
        CheckPath(plan.WorldPath); CheckPath(plan.ApplicationRoot); CheckPath(plan.Destination);
        using var lease = new FileStream(plan.WorldPath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var handles = new List<FileStream>();
        try
        {
            foreach (var file in plan.Files)
            {
                var target = Path.GetFullPath(Path.Combine(plan.Destination, file.Relative));
                var sourceAllowed = Within(file.Source, PackageRoot(plan.ApplicationRoot) ?? plan.ApplicationRoot) ||
                    file.Source.Equals(plan.WorldPath, StringComparison.OrdinalIgnoreCase) || file.Source.StartsWith(plan.WorldPath + ".", StringComparison.OrdinalIgnoreCase) || file.Source.StartsWith(plan.WorldPath + "-", StringComparison.OrdinalIgnoreCase);
                if (!Within(target, plan.Destination) || Within(file.Source, plan.Destination) || !sourceAllowed)
                    throw new IOException("Unsafe source/destination manifest boundary.");
                CheckPath(file.Source); CheckPath(target);
                // Acquire every file before deleting any: another active process/writer aborts cleanup.
                var source = new FileStream(file.Source, FileMode.Open, FileAccess.Read, FileShare.Delete); handles.Add(source);
                var copy = new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.Read); handles.Add(copy);
                if (Convert.ToHexString(SHA256.HashData(source)) != file.Hash || Convert.ToHexString(SHA256.HashData(copy)) != file.Hash)
                    throw new IOException("Source or destination changed; cleanup refused: " + file.Source);
            }
            foreach (var file in plan.Files) { CheckPath(file.Source); File.Delete(file.Source); }
        }
        finally { foreach (var handle in handles) handle.Dispose(); }
    }
    private static void RetargetLauncher(TransferPlan plan)
    {
        if (!OperatingSystem.IsWindows() || PackageRoot(plan.ApplicationRoot) is not null) return;
        var oldExe = Path.Combine(plan.ApplicationRoot, "Grid.Terminal.exe");
        var target = Path.Combine(plan.Destination, "app", "Grid.Terminal.exe");
        if (!File.Exists(target)) return;
        if (Environment.GetEnvironmentVariable(TerminalEnvironment.VariableName, EnvironmentVariableTarget.User)?.Equals(oldExe, StringComparison.OrdinalIgnoreCase) != true) return;
        Environment.SetEnvironmentVariable(TerminalEnvironment.VariableName, target, EnvironmentVariableTarget.User);
        var launcher = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GRID", "bin", "Grid7736.cmd");
        if (File.Exists(launcher) && File.ReadAllText(launcher).StartsWith("@rem GRID7736 managed launcher\r\n") && File.ReadAllText(launcher).Contains(oldExe.Replace("%", "%%"), StringComparison.OrdinalIgnoreCase))
            File.WriteAllText(launcher, TerminalEnvironment.LauncherScript(target));
    }
}
