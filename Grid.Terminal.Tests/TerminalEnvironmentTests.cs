using Xunit;
namespace Grid.Terminal.Tests;

public sealed class TerminalEnvironmentTests
{
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
        Assert.Contains("& ${env:Grid.AI}", message);
        Assert.Equal("Grid7736", TerminalEnvironment.LegacyVariableName);
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
