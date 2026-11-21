using Xunit;
namespace Grid.Terminal.Tests;

public sealed class TerminalEnvironmentTests
{
    [Fact]
    public void BareLauncherCommandUsesGridAiHyphenatedBrand()
    {
        Assert.Equal("GRID-AI.cmd", TerminalEnvironment.LauncherFileName);
    }

    [Fact]
    public void ObsoleteManagedLaunchersAreRemovedButUserScriptIsPreserved()
    {
        var folder = Path.Combine(Path.GetTempPath(), "GridLauncherTest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var current = Path.Combine(folder, TerminalEnvironment.LauncherFileName);
            File.WriteAllText(current, TerminalEnvironment.LauncherScript(@"C:\Grid\Grid.Terminal.exe"));
            File.WriteAllText(Path.Combine(folder, "Grid.AI.cmd"), "@rem GRID.AI managed launcher\r\n@echo off\r\n");
            File.WriteAllText(Path.Combine(folder, "Grid7736.cmd"), "@rem GRID7736 managed launcher\r\n@echo off\r\n");
            var userScript = Path.Combine(folder, "GridAI.cmd");
            File.WriteAllText(userScript, "@echo user-owned\r\n");

            Assert.True(TerminalEnvironment.RemoveObsoleteManagedLaunchers(folder, current));
            Assert.True(File.Exists(current));
            Assert.True(File.Exists(userScript));
            Assert.False(File.Exists(Path.Combine(folder, "Grid.AI.cmd")));
            Assert.False(File.Exists(Path.Combine(folder, "Grid7736.cmd")));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void LauncherPathPreservesExistingEntriesAndDoesNotDuplicate()
    {
        var folder = @"C:\Users\Test\GRID\bin";
        Assert.Equal(@"C:\Tools;" + folder, TerminalEnvironment.WithLauncherPath(@"C:\Tools", folder));
        Assert.Equal(@"C:\Tools;" + folder, TerminalEnvironment.WithLauncherPath(@"C:\Tools;" + folder, folder.ToUpperInvariant()));
    }

    [Fact]
    public void LauncherQuotesExecutableEscapesPercentAndForwardsArgumentsAndExitCode()
    {
        var path = Path.Combine(Path.GetTempPath(), "GRID %test% & space", "Grid.Terminal.exe");
        var script = TerminalEnvironment.LauncherScript(path);
        Assert.Contains("\"" + path.Replace("%", "%%") + "\" %*", script);
        Assert.Contains("DisableDelayedExpansion", script);
        Assert.Contains("exit /b %errorlevel%", script);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("missing")]
    public void MissingOrBrokenVariableIsConfiguredWithoutEmbeddingShellQuotes(string? previous)
    {
        var target = Path.Combine(Path.GetTempPath(), "GRID Folder", "Grid.Terminal.exe");
        string? user = previous, process = null;
        var message = TerminalEnvironment.Ensure(target, () => user, value => user = value,
            value => process = value, path => path == target);
        Assert.Equal(target, user); Assert.Equal(target, process);
        Assert.Contains("& $env:GridAI", message);
        Assert.StartsWith("@rem Grid.AI managed launcher", TerminalEnvironment.LauncherScript(target));
    }

    [Fact]
    public void ExistingValidInstallationIsPreservedAcrossOtherBuildLaunches()
    {
        var installed = Path.Combine(Path.GetTempPath(), "installed.exe");
        string? process = null;
        Assert.Null(TerminalEnvironment.Ensure("different.exe", () => installed,
            _ => throw new Exception("Must not overwrite"), value => process = value, _ => true));
        Assert.Equal(installed, process);
    }

    [Fact]
    public void MissingAppHostDoesNotRegisterDotnetOrBrokenLauncher()
    {
        var target = Path.Combine(Path.GetTempPath(), "dotnet.exe");
        var message = TerminalEnvironment.Ensure(target, () => null,
            _ => throw new Exception("Must not write"), _ => throw new Exception("Must not write"), _ => true);
        Assert.Contains("unavailable", message);
    }
}
