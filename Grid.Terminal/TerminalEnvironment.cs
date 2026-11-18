namespace Grid.Terminal;

public static class TerminalEnvironment
{
    public const string VariableName = "GridAI";

    public static string? Ensure()
    {
        if (!OperatingSystem.IsWindows()) return null;
        var assemblyPath = typeof(TerminalEnvironment).Assembly.Location;
        var executable = string.IsNullOrEmpty(assemblyPath)
            ? Environment.ProcessPath
            : Path.ChangeExtension(assemblyPath, ".exe");
        try
        {
            var message = Ensure(executable,
                () => Environment.GetEnvironmentVariable(VariableName, EnvironmentVariableTarget.User),
                value => Environment.SetEnvironmentVariable(VariableName, value, EnvironmentVariableTarget.User),
                value => Environment.SetEnvironmentVariable(VariableName, value, EnvironmentVariableTarget.Process),
                File.Exists);
            var target = Environment.GetEnvironmentVariable(VariableName);
            if (string.IsNullOrWhiteSpace(target) || !File.Exists(target)) return message;
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GRID", "bin");
            Directory.CreateDirectory(folder);
            var launcher = Path.Combine(folder, "GridAI.cmd");
            const string marker = "@rem Grid.AI managed launcher";
            if (File.Exists(launcher) && !File.ReadAllText(launcher).StartsWith(marker + "\r\n", StringComparison.Ordinal))
                throw new IOException($"Existing unmanaged launcher will not be overwritten: {launcher}");
            var script = LauncherScript(target);
            var changed = !File.Exists(launcher) || File.ReadAllText(launcher) != script;
            if (changed) File.WriteAllText(launcher, script);
            var userPath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);
            var updatedPath = WithLauncherPath(userPath, folder);
            if (updatedPath != userPath)
            {
                Environment.SetEnvironmentVariable("Path", updatedPath, EnvironmentVariableTarget.User);
                changed = true;
            }
            Environment.SetEnvironmentVariable("Path", WithLauncherPath(Environment.GetEnvironmentVariable("Path"), folder));
            return changed ? (message is null ? "" : message + "\n") +
                "Installed GridAI command on your user PATH. Restart your terminal app to use: GridAI" : message;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException or System.ComponentModel.Win32Exception)
        {
            return $"Warning: could not configure {VariableName}: {ex.Message}. GRID can still run normally.";
        }
    }

    public static string LauncherScript(string executable)
    {
        if (!Path.IsPathFullyQualified(executable) || executable.IndexOfAny(['"', '\r', '\n']) >= 0)
            throw new ArgumentException("Launcher requires an absolute executable path.");
        return "@rem Grid.AI managed launcher\r\n@echo off\r\nsetlocal DisableDelayedExpansion\r\n\"" +
            executable.Replace("%", "%%") + "\" %*\r\nexit /b %errorlevel%\r\n";
    }

    public static string WithLauncherPath(string? path, string folder) =>
        (path ?? "").Split(';').Any(part => Environment.ExpandEnvironmentVariables(part.Trim().Trim('"')).TrimEnd('\\')
            .Equals(folder.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            ? path! : string.IsNullOrEmpty(path) ? folder : path.TrimEnd(';') + ";" + folder;

    // Injected environment access keeps verification independent of the user's registry.
    public static string? Ensure(string? executable, Func<string?> readUser, Action<string> writeUser,
        Action<string> writeProcess, Func<string, bool> fileExists)
    {
        var existing = readUser();
        if (!string.IsNullOrWhiteSpace(existing) && Path.IsPathFullyQualified(existing) && fileExists(existing))
        {
            writeProcess(existing);
            return null;
        }
        if (string.IsNullOrWhiteSpace(executable) || !Path.IsPathFullyQualified(executable) ||
            !executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
            Path.GetFileName(executable).Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase) || !fileExists(executable))
            return "Warning: Grid.AI launcher variable was not configured because the terminal executable is unavailable.";
        writeUser(executable);
        writeProcess(executable);
        return $"Configured user environment variable {VariableName} = {executable}\n" +
            "In a fresh PowerShell session, launch with: & $env:GridAI\n" +
            "Existing shells may need to restart their parent terminal app or sign out/in to refresh environment variables.";
    }
}
