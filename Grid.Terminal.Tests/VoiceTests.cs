using Grid.Communication;
using Grid.Core;
using Grid.Simulation;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class VoiceTests
{
    private sealed class Provider : ILocalVoiceProvider
    {
        public byte[] Audio = [1, 2, 3, 4];
        public string Transcript = "who are you";
        public string? Spoken;
        public VoiceProfile? Profile;
        public int Microphone;
        public string? Language;
        public bool Fail, Wait, Interrupt;
        public Func<Task>? BeforeTranscription;
        public Task<string> DescribeAsync(CancellationToken token) => Task.FromResult("Offline test devices");
        public Task<byte[]> RecordAsync(int microphone, TextWriter output, CancellationToken token) { Microphone = microphone; return Task.FromResult(Audio); }
        public async Task<string> TranscribeAsync(byte[] wave, string language, CancellationToken token)
        {
            Language = language;
            if (BeforeTranscription is not null) await BeforeTranscription();
            if (Wait) await Task.Delay(Timeout.Infinite, token);
            if (Interrupt) throw new OperationCanceledException("Esc");
            if (Fail) throw new IOException("No recognizer"); return Transcript;
        }
        public Task SpeakAsync(string text, VoiceProfile? profile, CancellationToken token) { Spoken = text; Profile = profile; return Task.CompletedTask; }
    }
    private readonly TerminalSessionRegistry registry = new();
    private readonly TerminalIdentitySession identity = new(() => "voice-owner");
    private readonly GridSimulationEngine engine;
    private readonly CommandDispatcher commands;
    private readonly ChatCommands chat;
    private readonly Provider provider = new();
    private int saves;
    public VoiceTests()
    {
        registry.Add(identity); engine = new(GridWorldFactory.CreateGenesis(), editAuthorizer: registry); identity.Attach(engine); identity.Login();
        commands = DefaultCommands.Create(engine); chat = ChatCommands.Register(commands, engine, identity);
        VoiceCommands.Register(commands, engine, identity, chat, provider, () => saves++); commands.UseIdentity(identity);
    }
    private Task<CommandResult> Run(string command) => commands.Execute(command).Voice!.Run(TextWriter.Null, CancellationToken.None);
    [Fact]
    public async Task VoiceRequiresReviewAndStoresMicrophoneOriginThroughSameDialogue()
    {
        commands.Execute("connect"); var c = chat.ActiveConversation!.Value;
        Assert.Contains("UNSENT", (await Run("voice listen")).Text);
        Assert.Empty(engine.GetConversation(c).Messages); Assert.All(provider.Audio, b => Assert.Equal(0, b));
        commands.Execute("voice edit help"); Assert.Contains("help", commands.Execute("voice review").Text);
        var spoken = commands.Execute("voice send"); Assert.Contains("SIMULATED DIALOGUE", spoken.Text);
        commands.Execute("talk help");
        var messages = engine.GetConversation(c).Messages;
        Assert.Equal(MessageOrigin.Microphone, messages[0].Origin); Assert.Equal(MessageOrigin.Terminal, messages[2].Origin);
        Assert.Equal(messages[1].Content, messages[3].Content); Assert.Contains("No voice draft", commands.Execute("voice send").Text);
    }
    [Fact]
    public async Task RecognizedCommandsAreOnlyConversationData()
    {
        commands.Execute("connect"); provider.Transcript = "nge"; await Run("voice listen");
        var result = commands.Execute("voice send"); Assert.False(result.NullificationRequested);
        Assert.Equal("nge", engine.GetConversation(chat.ActiveConversation!.Value).Messages[0].Content);
        Assert.Contains("No task has been executed", result.Text);
    }
    [Fact]
    public async Task CancelDiscardsDraftAndFailedRecognitionClearsAudio()
    {
        commands.Execute("connect"); await Run("voice listen"); commands.Execute("voice cancel");
        Assert.Contains("No voice draft", commands.Execute("voice send").Text);
        provider.Audio = [1, 2]; provider.Fail = true; await Assert.ThrowsAsync<IOException>(() => Run("voice listen"));
        Assert.All(provider.Audio, b => Assert.Equal(0, b)); Assert.Empty(engine.GetConversation(chat.ActiveConversation!.Value).Messages);
    }
    [Fact]
    public async Task CancellationDuringTranscriptionClearsAudioAndCreatesNoDraft()
    {
        commands.Execute("connect"); provider.Wait = true;
        using var cancel = new CancellationTokenSource(); cancel.CancelAfter(30);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => commands.Execute("voice listen").Voice!.Run(TextWriter.Null, cancel.Token));
        Assert.All(provider.Audio, b => Assert.Equal(0, b)); Assert.Contains("No voice draft", commands.Execute("voice review").Text);
    }
    [Fact]
    public async Task IdentityChangeWhileTranscribingDiscardsResult()
    {
        commands.Execute("connect"); provider.BeforeTranscription = () => { identity.Logout(); identity.Login(); return Task.CompletedTask; };
        Assert.Contains("expired", (await Run("voice listen")).Text);
        Assert.Empty(engine.GetSnapshot().Communications.Conversations.Single().Messages);
        Assert.Contains("No voice draft", commands.Execute("voice review").Text);
    }
    [Fact]
    public async Task ConversationChangeCannotRedirectDraft()
    {
        commands.Execute("connect"); await Run("voice listen"); commands.Execute("disconnect"); commands.Execute("connect");
        Assert.Contains("discarded", commands.Execute("voice send").Text);
        Assert.All(engine.GetSnapshot().Communications.Conversations, c => Assert.Empty(c.Messages));
    }
    [Fact]
    public async Task PlaybackStartsMutedAndUsesSavedProfileOnlyOnExplicitSpeak()
    {
        commands.Execute("connect"); var agent = engine.EnsureCoordinator(); engine.RegisterAgentIdentity(agent.Id);
        Assert.Contains("saved", commands.Execute("voice profile MCP \"Test Voice\" -2 70").Text); Assert.Equal(1, saves);
        commands.Execute("talk hello"); Assert.Null(provider.Spoken);
        Assert.Contains("muted", commands.Execute("voice speak").Text);
        commands.Execute("voice mute off"); await Run("voice speak");
        Assert.Contains("SIMULATED DIALOGUE", provider.Spoken); Assert.Equal("Test Voice", provider.Profile!.VoiceName);
        identity.Logout(); identity.Login(); Assert.Contains("muted", commands.Execute("voice speak").Text);
    }
    [Fact]
    public void ProfileRejectsInvalidValuesUnregisteredAgentsAndUnauthorizedServiceCalls()
    {
        commands.Execute("connect"); var agent = engine.EnsureCoordinator();
        Assert.Contains("Register", commands.Execute("voice profile MCP test 0 80").Text);
        engine.RegisterAgentIdentity(agent.Id);
        Assert.Contains("Invalid", commands.Execute("voice profile MCP test 11 80").Text);
        Assert.Contains("Invalid", commands.Execute("voice profile MCP test 0 101").Text);
        Assert.Empty(engine.GetSnapshot().Communications.VoiceProfiles); Assert.Equal(0, saves);
        identity.Switch(agent.Id.ToString()); Assert.Contains("denied", commands.Execute("voice profile MCP test 0 80").Text);
        Assert.Throws<UnauthorizedAccessException>(() => engine.SetVoiceProfile(identity.Reference, new(agent.Id, "test")));
    }
    [Fact]
    public async Task DeviceAndLanguageSelectionReachProviderAndRequestsAreSingleUse()
    {
        commands.Execute("connect"); commands.Execute("voice microphone 3"); commands.Execute("voice language en-US");
        var operation = commands.Execute("voice listen").Voice!; await operation.Run(TextWriter.Null, CancellationToken.None);
        Assert.Equal(3, provider.Microphone); Assert.Equal("en-US", provider.Language);
        Assert.Contains("expired", (await operation.Run(TextWriter.Null, CancellationToken.None)).Text);
        Assert.Contains("Offline test devices", (await Run("voice devices")).Text);
    }
    [Theory]
    [InlineData("")]
    [InlineData("bad\u001btext")]
    public async Task InvalidRecognitionNeverEntersTranscript(string text)
    {
        commands.Execute("connect"); provider.Transcript = text;
        Assert.Contains("No valid speech", (await Run("voice listen")).Text);
        Assert.Empty(engine.GetConversation(chat.ActiveConversation!.Value).Messages);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HostHandlesMissingAudioOrEscapeAndTypedConversationContinues(bool interrupt)
    {
        provider.Fail = true; provider.Interrupt = interrupt; var output = new StringWriter();
        var host = new TerminalHost(engine, commands, new StringReader("connect\nvoice listen\ntalk hello\nexit\n"), output, interactiveConfirmation: true);
        Assert.Equal(0, await host.RunAsync()); Assert.Contains(interrupt ? "Voice cancelled" : "Voice unavailable", output.ToString()); Assert.Contains("SIMULATED DIALOGUE", output.ToString());
    }
    [Fact]
    public async Task NoninteractiveHostDoesNotRecord()
    {
        var output = new StringWriter();
        var host = new TerminalHost(engine, commands, new StringReader("connect\nvoice listen\nexit\n"), output);
        await host.RunAsync(); Assert.Contains("interactive host", output.ToString()); Assert.Equal(new byte[] { 1, 2, 3, 4 }, provider.Audio);
    }
    [Fact]
    public void AttachedVoiceIsExplicitlyHostOnly()
    {
        var child = identity.CreateAttachment(); registry.Add(child); var dispatcher = DefaultCommands.Create(engine);
        var attachedChat = ChatCommands.Register(dispatcher, engine, child); VoiceCommands.Register(dispatcher, engine, child, attachedChat, provider); dispatcher.UseIdentity(child);
        Assert.Contains("original host", dispatcher.Execute("voice listen").Text);
    }
    [Fact]
    public async Task WindowsProviderTranscribesGeneratedSpeechWithoutMicrophoneOrPlayback()
    {
        if (!OperatingSystem.IsWindows()) return;
        var provider = new WindowsVoiceProvider();
        if (!(await provider.DescribeAsync(CancellationToken.None)).Contains("en-US")) return;
        const string script = "Add-Type -AssemblyName System.Speech; $s=[System.Speech.Synthesis.SpeechSynthesizer]::new(); $m=[System.IO.MemoryStream]::new(); try {$s.SetOutputToWaveStream($m); $s.Speak('Hello this is a test of the voice system'); [Console]::Write([Convert]::ToBase64String($m.ToArray()))} finally {$s.Dispose();$m.Dispose()}";
        var start = new System.Diagnostics.ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe"))
        { UseShellExecute = false, CreateNoWindow = true, WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-EncodedCommand", Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script)) }) start.ArgumentList.Add(argument);
        using var process = System.Diagnostics.Process.Start(start)!;
        var audio = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var kill = timeout.Token.Register(() => { try { process.Kill(true); } catch (InvalidOperationException) { } });
        await process.WaitForExitAsync(timeout.Token); Assert.Equal(0, process.ExitCode); _ = await error;
        var wave = Convert.FromBase64String(await audio);
        try { Assert.False(string.IsNullOrWhiteSpace(await provider.TranscribeAsync(wave, "en-US", timeout.Token))); }
        finally { Array.Clear(wave); }
    }
    [Fact]
    public async Task RealProviderReportsSafeStageAndErrorCodeForBadAudio()
    {
        if (!OperatingSystem.IsWindows()) return;
        var provider = new WindowsVoiceProvider();
        if (!(await provider.DescribeAsync(CancellationToken.None)).Contains("en-US")) return;
        var error = await Assert.ThrowsAsync<IOException>(() => provider.TranscribeAsync([1, 2, 3], "en-US", CancellationToken.None));
        Assert.Contains("Stage: audio input", error.Message); Assert.Contains("0x", error.Message);
        Assert.DoesNotContain("AQID", error.Message);
    }
    [Fact]
    public async Task NumericMicrophoneShortcutAndConnectedDraftHint()
    {
        commands.Execute("connect"); Assert.Contains("Microphone 0 selected", commands.Execute("voice 0").Text);
        Assert.Contains("Already connected", commands.Execute("voice review").Text);
        await Run("voice listen"); Assert.Equal(0, provider.Microphone);
    }
    [Fact]
    public async Task WindowsProviderTranscribesSilentPcmThroughRealProcessInput()
    {
        if (!OperatingSystem.IsWindows()) return;
        var provider = new WindowsVoiceProvider();
        var devices = await provider.DescribeAsync(CancellationToken.None);
        if (!devices.Contains("en-US")) return;
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
        var pcm = new byte[32000];
        writer.Write("RIFF"u8); writer.Write(36 + pcm.Length); writer.Write("WAVEfmt "u8); writer.Write(16);
        writer.Write((ushort)1); writer.Write((ushort)1); writer.Write(16000); writer.Write(32000); writer.Write((ushort)2); writer.Write((ushort)16);
        writer.Write("data"u8); writer.Write(pcm.Length); writer.Write(pcm);
        Assert.Equal("", await provider.TranscribeAsync(stream.ToArray(), "en-US", CancellationToken.None));
    }
    [Fact]
    public async Task WindowsProviderInventorySmokeDoesNotRecordOrSpeak()
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            var description = await new WindowsVoiceProvider().DescribeAsync(CancellationToken.None);
            Assert.Contains("Microphones", description); Assert.Contains("Offline recognizers", description); Assert.Contains("Installed voices", description);
        }
        catch (IOException e) { Assert.Contains("talk remains available", e.Message); }
    }
}
