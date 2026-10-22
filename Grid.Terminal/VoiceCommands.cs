using Grid.Communication;
using Grid.Simulation;
namespace Grid.Terminal;

public sealed record VoiceOperation(Func<TextWriter, CancellationToken, Task<CommandResult>> Run, Func<bool> IsValid);

/// <summary>Transient reviewed draft bound to one identity generation and conversation. Never dispatches recognized shell commands.</summary>
public sealed class VoiceCommands
{
    private string? draft;
    private Guid? draftConversation;
    private int microphone = -1;
    private string language = System.Globalization.CultureInfo.CurrentUICulture.Name;
    private bool muted = true;
    private readonly GridSimulationEngine engine;
    private readonly TerminalIdentitySession identity;
    private readonly ChatCommands chat;
    private readonly ILocalVoiceProvider provider;
    private readonly Action? save;
    private VoiceCommands(GridSimulationEngine engine, TerminalIdentitySession identity, ChatCommands chat, ILocalVoiceProvider provider, Action? save)
    {
        this.engine = engine; this.identity = identity; this.chat = chat; this.provider = provider; this.save = save;
        identity.Changed += () => { draft = null; draftConversation = null; muted = true; };
    }
    public static void Register(CommandDispatcher commands, GridSimulationEngine engine, TerminalIdentitySession identity, ChatCommands chat,
        ILocalVoiceProvider? provider = null, Action? save = null)
    {
        var voice = new VoiceCommands(engine, identity, chat, provider ?? new WindowsVoiceProvider(), save);
        commands.Register(new("voice", "voice <status|devices|microphone|language|listen|review|edit|send|cancel|mute|speak|profile> ...", "Offline hold-to-talk, reviewed transcripts and explicitly requested speech.", voice.Execute) { Capability = "chat" });
    }
    private CommandResult Deferred(Func<TextWriter, CancellationToken, Task<CommandResult>> operation, Guid? conversation = null)
    {
        var generation = identity.Generation; var used = 0;
        bool Valid() => identity.Generation == generation && identity.Allows("chat") && (conversation is null || chat.ActiveConversation == conversation);
        return new("", Voice: new(async (output, token) =>
        {
            if (Interlocked.Exchange(ref used, 1) != 0 || !Valid()) return new("Voice request expired; try again.");
            return await operation(output, token);
        }, Valid));
    }
    private string Review() => draft is null ? (chat.ActiveConversation is null ? "No voice draft. Use connect MCP -inline, then voice listen." : "No voice draft. Already connected; use voice listen.") :
        $"UNSENT VOICE TRANSCRIPT // conversation {draftConversation}\n{draft}\nReview carefully. voice edit <text> corrects it; voice send stores it as a message; voice cancel discards it.";
    private CommandResult Execute(IReadOnlyList<string> args)
    {
        if (identity.IsAttachment) return new("Voice hardware is owned by the original host terminal. Use voice there; typed talk/resume remain available here.");
        if (args.Count == 0 || args.Count == 1 && args[0] == "status")
            return new($"Offline Windows voice | microphone {microphone} | language {language} | playback {(muted ? "muted" : "enabled on explicit voice speak")}\nNo background listening. Use voice devices, voice listen, voice review, voice send, voice mute off, voice speak. Esc interrupts audio.\n" + Review());
        if (args.Count == 1 && int.TryParse(args[0], out var microphoneId) && microphoneId >= -1)
        { microphone = microphoneId; return new($"Microphone {microphoneId} selected. Use voice listen after connect MCP -inline."); }
        switch (args[0].ToLowerInvariant())
        {
            case "devices" when args.Count == 1:
                return Deferred(async (_, token) => new(await provider.DescribeAsync(token)));
            case "microphone" when args.Count == 2 && int.TryParse(args[1], out var device) && device >= -1:
                microphone = device; return new($"Microphone {device} selected for this terminal; availability is checked when recording.");
            case "language" when args.Count == 2:
                language = System.Globalization.CultureInfo.GetCultureInfo(args[1]).Name;
                return new("Recognition language: " + language + ". An installed offline recognizer is required.");
            case "listen" when args.Count == 1:
                if (chat.ActiveConversation is not { } conversation) return new("Use connect -inline or resume -inline before recording.");
                draft = null; draftConversation = null;
                var generation = identity.Generation;
                return Deferred(async (output, token) =>
                {
                    var wave = await provider.RecordAsync(microphone, output, token);
                    string transcript;
                    try { await output.WriteLineAsync("TRANSCRIBING LOCALLY // Esc cancels; nothing is sent yet."); transcript = await provider.TranscribeAsync(wave, language, token); }
                    finally { Array.Clear(wave); }
                    token.ThrowIfCancellationRequested();
                    return identity.Execute("chat", () =>
                    {
                        if (identity.Generation != generation || chat.ActiveConversation != conversation) return new("Voice request expired; transcript discarded.");
                        if (!CommunicationState.ValidContent(transcript)) return new("No valid speech recognized (1..4096 characters required). Nothing sent; try again or use talk.");
                        draft = transcript; draftConversation = conversation; return new(Review());
                    }, "voice-review");
                }, conversation);
            case "review" when args.Count == 1: return new(Review());
            case "edit" when args.Count >= 2:
                if (draft is null || draftConversation != chat.ActiveConversation) return new("No current voice draft to edit.");
                var replacement = string.Join(" ", args.Skip(1));
                if (!CommunicationState.ValidContent(replacement)) return new("Transcript must contain 1..4096 valid text characters.");
                draft = replacement; return new(Review());
            case "send" when args.Count == 1:
                if (draft is null || draftConversation is not { } target) return new("No voice draft to send.");
                if (target != chat.ActiveConversation) { draft = null; draftConversation = null; return new("Conversation changed; voice draft discarded. Record again."); }
                var result = chat.SendVoice(target, draft); draft = null; draftConversation = null; return result;
            case "cancel" when args.Count == 1:
                draft = null; draftConversation = null; return new("Voice draft discarded. During recording/transcription/playback, press Esc to interrupt.");
            case "mute" when args.Count == 2 && args[1] is "on" or "off":
                muted = args[1] == "on"; return new(muted ? "Speech playback muted." : "Speech playback enabled only on explicit voice speak. No automatic/background speech.");
            case "speak" when args.Count == 1:
                if (muted) return new("Speech is muted. Use voice mute off before voice speak.");
                if (chat.ActiveConversation is not { } current) return new("Use connect -inline or resume -inline first.");
                var c = engine.GetConversation(current);
                var reply = c.Messages.LastOrDefault(m => m.Origin == MessageOrigin.SystemEvent && m.Sender.Kind == ParticipantKind.Program);
                if (reply is null) return new("No agent reply to speak yet.");
                var profile = engine.GetSnapshot().Communications.VoiceProfiles.SingleOrDefault(p => p.AgentId == reply.Sender.Id);
                return Deferred(async (output, token) =>
                {
                    await output.WriteLineAsync("SPEAKING LOCALLY // Esc stops playback.");
                    await provider.SpeakAsync(reply.Content, profile, token); return new("Speech finished.");
                }, current);
            case "profile" when args.Count == 1:
                var profiles = engine.GetSnapshot().Communications.VoiceProfiles;
                return new(TerminalTable.Render(["Agent ID", "Windows voice", "Rate", "Volume"], profiles.Where(p => identity.IsMaster || p.AgentId == identity.CurrentRecipient.Id)
                    .Select(p => new[] { p.AgentId.ToString(), p.VoiceName, p.Rate.ToString(), p.Volume.ToString() })));
            case "profile" when args.Count == 5 && int.TryParse(args[3], out var rate) && int.TryParse(args[4], out var volume):
                if (!identity.IsMaster) return new("Access denied. Master Control sets voice profiles.");
                if (save is null) return new("Voice profiles require a saved world.");
                var agent = engine.ResolveProgram(args[1]);
                engine.SetVoiceProfile(identity.Reference, new(agent.Id, args[2], rate, volume));
                try { save(); }
                catch (Exception e) when (e is IOException or System.Data.Common.DbException or UnauthorizedAccessException or InvalidOperationException)
                { return new("Voice profile changed in memory but save failed. Resolve before restart: " + e.Message); }
                return new("Voice profile saved. Installed voice availability is checked on playback; use voice devices to see names.");
            default: return new("Usage: voice status | devices | microphone <-1|id> | language <culture> | listen | review | edit <text> | send | cancel | mute <on|off> | speak | profile [agent-id|name \"voice name\" rate(-10..10) volume(0..100)]");
        }
    }
}
