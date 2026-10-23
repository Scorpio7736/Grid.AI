using Xunit;
namespace Grid.Terminal.Tests;

public sealed class TransferFolderPickerTests
{
    [Fact]
    public async Task ActualWindowsDialogSetupSupportsItsPropertiesAndCancelReturnsNull()
    {
        if (!OperatingSystem.IsWindows()) return;
        // Execute the production helper using the real Windows Forms object. Suppress only
        // the modal interaction so this regression cannot block an unattended test run.
        var script = TransferFolderPicker.Script.Replace("$dialog.ShowDialog()", "[System.Windows.Forms.DialogResult]::Cancel");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        Assert.Null(await TransferFolderPicker.RunScriptAsync(script, timeout.Token));
    }
    [Fact]
    public async Task ActualWindowsHelperReturnsSelectedPathAsJson()
    {
        if (!OperatingSystem.IsWindows()) return;
        var selected = Path.Combine(Path.GetTempPath(), "GRID selected folder " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(selected);
        try
        {
            var script = TransferFolderPicker.Script.Replace("$dialog.ShowDialog()", "[System.Windows.Forms.DialogResult]::OK")
                .Replace("$files = $dialog.SelectedPath", "$dialog.SelectedPath = '" + selected.Replace("'", "''") + "'; $files = $dialog.SelectedPath");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            Assert.Equal(selected, await TransferFolderPicker.RunScriptAsync(script, timeout.Token));
        }
        finally { Directory.Delete(selected); }
    }
    [Fact]
    public async Task HelperFailureDoesNotExposePowerShellXml()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var error = await Assert.ThrowsAsync<IOException>(() => TransferFolderPicker.RunScriptAsync("$ErrorActionPreference='Stop'; throw 'PRIVATE HELPER DIAGNOSTIC'", timeout.Token));
        Assert.Contains("Folder picker failed", error.Message);
        Assert.Contains("absolute destination", error.Message);
        Assert.DoesNotContain("CLIXML", error.Message);
        Assert.DoesNotContain("PRIVATE", error.Message);
    }
}
