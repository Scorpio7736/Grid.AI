using System.Diagnostics;
using System.Text;
using System.Text.Json;
namespace Grid.Terminal;

/// <summary>Local Windows dialog. The fixed helper script never contains user-supplied code.</summary>
public static class TransferFolderPicker
{
    public static Task<string?> PickAsync(CancellationToken cancellationToken) => RunScriptAsync(Script, cancellationToken);

    internal const string Script = """
            $ErrorActionPreference = 'Stop'
            $ProgressPreference = 'SilentlyContinue'
            Add-Type -AssemblyName System.Windows.Forms
            $dialog = New-Object System.Windows.Forms.FolderBrowserDialog
            $dialog.Description = 'GRID7736 // GRID will create a new GRID7736 folder inside this location.'
            try {
                $files = $null
                if ($dialog.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) { $files = $dialog.SelectedPath }
                [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
                if ($null -eq $files) { [Console]::WriteLine('null') }
                else { ConvertTo-Json -InputObject $files -Compress }
            } finally { $dialog.Dispose() }
            """;
    internal static async Task<string?> RunScriptAsync(string script, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows()) throw new IOException("Folder picker requires Windows; use grid -copyto or grid -transferto with an absolute destination.");
        var executable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe");
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8 };
        foreach (var argument in new[] { "-NoProfile", "-STA", "-NonInteractive", "-EncodedCommand", Convert.ToBase64String(Encoding.Unicode.GetBytes(script)) }) start.ArgumentList.Add(argument);
        try
        {
            using var process = Process.Start(start) ?? throw new IOException("Folder picker could not start.");
            using var registration = cancellationToken.Register(() => { try { if (!process.HasExited) process.Kill(true); } catch (InvalidOperationException) { } });
            var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            await error; // Drain stderr, but never render PowerShell CLIXML or helper internals in the terminal.
            if (process.ExitCode != 0) throw new IOException($"Folder picker failed (exit {process.ExitCode}); use grid -copyto or grid -transferto with an absolute destination.");
            var folder = JsonSerializer.Deserialize<string>(await output);
            if (folder is not null && (folder.Length > 4096 || !Path.IsPathFullyQualified(folder))) throw new IOException("Select an absolute folder path.");
            return folder;
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or JsonException) { throw new IOException("Folder picker unavailable; use grid -copyto or grid -transferto with an absolute destination.", e); }
    }
}
