using System.Diagnostics;
using System.Text;
using System.Text.Json;
namespace Grid.Terminal;

/// <summary>Local Windows dialog. The fixed helper script never contains user-supplied code.</summary>
public static class ContextFilePicker
{
    public static async Task<string[]> PickAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows()) throw new IOException("File picker requires Windows; use context add with an absolute file path.");
        var executable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe");
        const string script = """
            $ErrorActionPreference = 'Stop'
            Add-Type -AssemblyName System.Windows.Forms
            $dialog = New-Object System.Windows.Forms.OpenFileDialog
            $dialog.Title = 'Grid.AI // Select context files'
            $dialog.Multiselect = $true
            $dialog.CheckFileExists = $true
            $dialog.Filter = 'Context files|*.txt;*.md;*.markdown;*.pdf;*.docx;*.cs;*.csproj;*.sln;*.py;*.js;*.ts;*.tsx;*.jsx;*.json;*.xml;*.yaml;*.yml;*.toml;*.ini;*.csv;*.sql;*.html;*.css;*.cpp;*.c;*.h;*.java;*.rs;*.go;*.ps1;*.sh|All files|*.*'
            try {
                $files = @()
                if ($dialog.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) { $files = @($dialog.FileNames) }
                [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
                ConvertTo-Json -InputObject $files -Compress
            } finally { $dialog.Dispose() }
            """;
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8 };
        foreach (var argument in new[] { "-NoProfile", "-STA", "-NonInteractive", "-EncodedCommand", Convert.ToBase64String(Encoding.Unicode.GetBytes(script)) }) start.ArgumentList.Add(argument);
        try
        {
            using var process = Process.Start(start) ?? throw new IOException("File picker could not start.");
            using var registration = cancellationToken.Register(() => { try { if (!process.HasExited) process.Kill(true); } catch (InvalidOperationException) { } });
            var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            if (process.ExitCode != 0) throw new IOException("File picker failed; use context add with an absolute file path. " + await error);
            var files = JsonSerializer.Deserialize<string[]>(await output) ?? [];
            if (files.Length > 64 || files.Any(p => p is null || p.Length > 4096)) throw new IOException("Select at most 64 files with valid paths.");
            return files;
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or JsonException) { throw new IOException("File picker unavailable; use context add with an absolute file path.", e); }
    }
}
