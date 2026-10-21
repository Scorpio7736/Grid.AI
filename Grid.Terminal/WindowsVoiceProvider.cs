using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Grid.Communication;
namespace Grid.Terminal;

public interface ILocalVoiceProvider
{
    Task<string> DescribeAsync(CancellationToken token);
    Task<byte[]> RecordAsync(int microphone, TextWriter output, CancellationToken token);
    Task<string> TranscribeAsync(byte[] wave, string language, CancellationToken token);
    Task SpeakAsync(string text, VoiceProfile? profile, CancellationToken token);
}

/// <summary>Installed offline Windows SAPI engines. No downloads, network API, scripts from input or audio files.</summary>
public sealed class WindowsVoiceProvider : ILocalVoiceProvider
{
    public Task<byte[]> RecordAsync(int microphone, TextWriter output, CancellationToken token) => WindowsMicrophone.RecordAsync(microphone, output, token);
    public async Task<string> DescribeAsync(CancellationToken token) => WindowsMicrophone.Describe() + "\n" + await RunAsync(new { Mode = "list" }, token);
    public Task<string> TranscribeAsync(byte[] wave, string language, CancellationToken token) => RunAsync(new { Mode = "recognize", Audio = Convert.ToBase64String(wave), Language = language }, token);
    public async Task SpeakAsync(string text, VoiceProfile? profile, CancellationToken token) =>
        _ = await RunAsync(new { Mode = "speak", Text = text, Voice = profile?.VoiceName, Rate = profile?.Rate ?? 0, Volume = profile?.Volume ?? 80 }, token);

    private const string Script = """
        $ErrorActionPreference = 'Stop'
        $ProgressPreference = 'SilentlyContinue'
        $stage = 'initialize'
        try {
        [Console]::InputEncoding = New-Object System.Text.UTF8Encoding($false)
        [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
        Add-Type -AssemblyName System.Speech
        $request = [Console]::In.ReadToEnd() | ConvertFrom-Json
        $stage = [string]$request.Mode
        if ($request.Mode -eq 'list') {
            $recognizers = @([System.Speech.Recognition.SpeechRecognitionEngine]::InstalledRecognizers() | ForEach-Object { $_.Culture.Name + ' : ' + $_.Name })
            $speaker = New-Object System.Speech.Synthesis.SpeechSynthesizer
            try {
                $voices = @($speaker.GetInstalledVoices() | Where-Object Enabled | ForEach-Object { $_.VoiceInfo.Name })
                [Console]::Write(('Offline recognizers: ' + ($recognizers -join '; ') + [Environment]::NewLine + 'Installed voices: ' + ($voices -join '; ')))
            } finally { $speaker.Dispose() }
        } elseif ($request.Mode -eq 'recognize') {
            $info = [System.Speech.Recognition.SpeechRecognitionEngine]::InstalledRecognizers() | Where-Object { $_.Culture.Name -eq $request.Language } | Select-Object -First 1
            if ($null -eq $info) { throw 'No installed offline recognizer for this language. Use voice devices, voice language, or typed talk.' }
            $stage = 'recognizer initialization'
            $recognizer = [System.Speech.Recognition.SpeechRecognitionEngine]::new($info)
            $bytes = [Convert]::FromBase64String($request.Audio)
            $stream = [System.IO.MemoryStream]::new([byte[]]$bytes)
            try {
                $recognizer.LoadGrammar((New-Object System.Speech.Recognition.DictationGrammar))
                $stage = 'audio input'
                $recognizer.SetInputToWaveStream($stream)
                $stage = 'transcription'
                Add-Type -ReferencedAssemblies @('System.dll', [System.Speech.Recognition.SpeechRecognitionEngine].Assembly.Location) -TypeDefinition @'
        using System;
        using System.Collections.Generic;
        using System.Speech.Recognition;
        using System.Threading;
        public static class GridOfflineRecognition {
            public static string Read(SpeechRecognitionEngine engine) {
                var parts = new List<string>();
                Exception failure = null;
                bool cancelled = false;
                using (var done = new ManualResetEventSlim(false)) {
                    EventHandler<SpeechRecognizedEventArgs> recognized = (sender, args) => { parts.Add(args.Result.Text); };
                    EventHandler<RecognizeCompletedEventArgs> completed = (sender, args) => { failure = args.Error; cancelled = args.Cancelled; done.Set(); };
                    engine.SpeechRecognized += recognized;
                    engine.RecognizeCompleted += completed;
                    try {
                        engine.RecognizeAsync(RecognizeMode.Multiple);
                        if (!done.Wait(TimeSpan.FromSeconds(45))) {
                            engine.RecognizeAsyncCancel();
                            // Keep the completion signal alive until the recognizer stops.
                            if (!done.Wait(TimeSpan.FromSeconds(2))) engine.Dispose();
                            throw new TimeoutException("Offline recognition timed out.");
                        }
                        if (failure != null) throw failure;
                        if (cancelled) throw new OperationCanceledException();
                        var text = string.Join(" ", parts);
                        if (text.Length > 4096) throw new InvalidOperationException("Transcript exceeds 4096 characters.");
                        return text;
                    } finally {
                        engine.SpeechRecognized -= recognized;
                        engine.RecognizeCompleted -= completed;
                    }
                }
            }
        }
        '@
                [Console]::Write([GridOfflineRecognition]::Read($recognizer))
            } finally { $recognizer.Dispose(); $stream.Dispose(); [Array]::Clear($bytes, 0, $bytes.Length) }
        } elseif ($request.Mode -eq 'speak') {
            $speaker = New-Object System.Speech.Synthesis.SpeechSynthesizer
            try {
                if ($request.Voice) { $speaker.SelectVoice($request.Voice) }
                $speaker.Rate = $request.Rate
                $speaker.Volume = $request.Volume
                $speaker.Speak([string]$request.Text)
            } finally { $speaker.Dispose() }
        } else { throw 'Invalid voice operation.' }
        } catch {
            $failure = $_.Exception.GetBaseException()
            [Console]::Error.Write('GRID_VOICE_ERROR|' + $stage + '|' + $failure.GetType().Name + '|0x' + $failure.HResult.ToString('X8'))
            exit 1
        }
        """;
    private static async Task<string> RunAsync(object request, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) throw new IOException("Offline Windows voice is unavailable on this platform; typed talk remains available.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token); timeout.CancelAfter(TimeSpan.FromSeconds(60));
        var executable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe");
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, StandardInputEncoding = new UTF8Encoding(false), StandardOutputEncoding = Encoding.UTF8 };
        foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-EncodedCommand", Convert.ToBase64String(Encoding.Unicode.GetBytes(Script)) }) start.ArgumentList.Add(argument);
        try
        {
            using var process = Process.Start(start) ?? throw new IOException("Voice helper could not start.");
            using var registration = timeout.Token.Register(() => { try { if (!process.HasExited) process.Kill(true); } catch (InvalidOperationException) { } });
            var output = ReadBounded(process.StandardOutput, timeout.Token);
            var error = ReadBounded(process.StandardError, timeout.Token);
            await process.StandardInput.WriteAsync(JsonSerializer.Serialize(request).AsMemory(), timeout.Token); process.StandardInput.Close();
            await process.WaitForExitAsync(timeout.Token);
            var text = await output; var diagnostic = await error;
            if (process.ExitCode != 0)
            {
                // Only expose our structured stage/type/code, never raw stderr (which can contain input).
                var match = System.Text.RegularExpressions.Regex.Match(diagnostic, @"GRID_VOICE_ERROR\|([a-z ]{1,40})\|([A-Za-z]{1,80})\|(0x[0-9A-F]{8})");
                var detail = match.Success ? $" Stage: {match.Groups[1].Value}; {match.Groups[2].Value}; {match.Groups[3].Value}." : $" Helper exit code: {process.ExitCode}.";
                throw new IOException("Offline speech failed." + detail + " Check voice devices and voice language; typed talk remains available.");
            }
            return text;
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { throw new IOException("Voice operation timed out after 60 seconds; typed talk remains available."); }
        catch (System.ComponentModel.Win32Exception e) { throw new IOException("Windows speech helper unavailable; typed talk remains available.", e); }
    }
    private static async Task<string> ReadBounded(StreamReader reader, CancellationToken token)
    {
        var result = new StringBuilder(); var buffer = new char[1024]; int count;
        while ((count = await reader.ReadAsync(buffer.AsMemory(), token)) != 0)
        {
            if (result.Length + count > 65536) throw new IOException("Voice helper output exceeded its limit.");
            result.Append(buffer, 0, count);
        }
        return result.ToString();
    }
}
