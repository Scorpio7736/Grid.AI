using Grid.Communication;
using Grid.Core;
using System.Security.Cryptography;
using System.Text;
namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    // Stable per-world local operator handle; this is not a login or an administrator grant.
    public Recipient LocalOperator => new(ParticipantKind.Human, DialogueId("local-operator-v1"), "Local operator");
    private Guid DialogueId(string key) => new(SHA256.HashData(Encoding.UTF8.GetBytes($"{world.Id:D}:{key}")).AsSpan(0, 16));

    public Recipient EnsureCoordinator()
    {
        lock (gate)
        {
            RequireAgentActivity();
            var matches = world.Programs.Values.Where(p => p.Name.Equals("MCP", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length > 1 || matches.Any(p => p.Kind != ResidentKind.Agent))
                throw new InvalidOperationException("MCP name is ambiguous or belongs to a non-agent; use an explicit recipient ID.");
            if (matches.Length == 1) return ResolveProgram(matches[0].Id.ToString());
            // Keep the original identity seed so existing MCP/legacy coordinator references remain stable.
            var id = DialogueId("archon-v1");
            if (world.Programs.ContainsKey(id)) throw new InvalidOperationException("Coordinator identity already exists under another name; use its ID.");
            var district = world.City?.Read().Districts.Single(d => d.Category == Grid.Core.Generation.HardwareKind.Motherboard).Id
                ?? world.Districts.Keys.Order().FirstOrDefault();
            if (district == Guid.Empty) throw new InvalidOperationException("A district is required before creating MCP.");
            world.AddProgram(new GridProgram { Id = id, Name = "MCP", Kind = ResidentKind.Agent, Occupation = "Local coordinator" }, district);
            RecordEvent("CoordinatorCreated", id, "MCP", "Created persistent deterministic dialogue coordinator; no authority or host execution granted.");
            return ResolveProgram(id.ToString());
        }
    }

    /// <summary>Atomic deterministic local dialogue pair. Does not execute requests or call an external model.</summary>
    public IReadOnlyList<GridMessage> Talk(Guid conversationId, Recipient human, string text, CancellationToken token = default, MessageOrigin origin = MessageOrigin.Terminal)
        => TalkCore(conversationId, human, text, token, origin);

    private IReadOnlyList<GridMessage> TalkCore(Guid conversationId, Recipient human, string text, CancellationToken token, MessageOrigin origin, string? generated = null)
    {
        lock (gate)
        {
            RequireAgentActivity();
            var c = GetConversation(conversationId); human = Canonical(human);
            if (!c.Participants.Any(p => CommunicationState.SameIdentity(p, human)))
                throw new ArgumentException("Dialogue requires the active participant identity.");
            if (c.Status != ConversationStatus.Open) throw new InvalidOperationException("Conversation has ended; connect to start another.");
            if (origin is not (MessageOrigin.Terminal or MessageOrigin.Microphone)) throw new ArgumentException("Invalid dialogue origin.");
            if (!CommunicationState.ValidContent(text)) throw new ArgumentException("Message must contain 1..4096 characters of valid text.");
            var selection = DialogueResponder(c, human, text);
            var other = selection.Recipient;
            if (other.Kind != ParticipantKind.Program) throw new ArgumentException("Dialogue requires a program recipient.");
            other = Canonical(other);
            if (communications.Conversations.Sum(x => x.Messages.Count) > CommunicationState.MaxMessages - 2)
                throw new InvalidOperationException("Two message slots are required; no part of this turn was stored.");
            var snapshot = GetSnapshot(); var program = snapshot.Programs.Single(p => p.Id == other.Id);
            var query = text.Trim().ToLowerInvariant();
            var reply = generated ?? (c.IsMeeting ? "[LOCAL FALLBACK] " + selection.Reason + ". Local AI is unavailable; I cannot provide an AI answer. Ask a focused question when AI is available, or use GRID commands for verified details. No task was executed." : query switch {
                "status" when human.Kind == ParticipantKind.Program => $"[VERIFIED OWN STATE at tick {snapshot.TickCount}] Active identity {human.Name} ({human.Id}); no global history or task visibility is granted.",
                "status" => $"[VERIFIED GRID STATE at tick {snapshot.TickCount}] Clock: {(snapshot.IsRunning ? "running" : "paused")}; elapsed: {snapshot.ElapsedSimulationTime}; districts: {snapshot.Districts.Count}; residents: {snapshot.Programs.Count}.",
                "who are you" or "who are you?" => $"[VERIFIED GRID STATE at tick {snapshot.TickCount}] {program.Name}; resident ID {program.Id}; kind {program.Kind}; occupation {program.Occupation}. This reply is produced by GRID's deterministic adapter, not the host application.",
                "context" => $"[VERIFIED CONTEXT AVAILABILITY] {ContextFor(other.Id, Guid.Empty).Count} captured files are explicitly shared with this recipient. No file contents were executed or copied into this transcript. Use context list/read as the permitted identity; file ingestion into AI dialogue is not enabled.",
                "help" => "[SIMULATED DIALOGUE] Ask status or who are you for verified GRID facts. I can provide local coordination guidance; use explicit task commands for reviewed execution. This is the deterministic fallback adapter.",
                _ => "[SIMULATED DIALOGUE] Your message is recorded. No task has been executed or delegated. Ask status, who are you, or help."
            });
            if (!CommunicationState.ValidContent(reply)) throw new ArgumentException("Invalid reply text.");
            var utc = timeProvider.GetUtcNow().ToUniversalTime();
            var pair = new[] { new GridMessage(Guid.NewGuid(), c.Messages.Count + 1, human, text, origin, clock.TickCount, utc),
                new GridMessage(Guid.NewGuid(), c.Messages.Count + 2, other, reply, MessageOrigin.SystemEvent, clock.TickCount, utc) };
            token.ThrowIfCancellationRequested();
            ReplaceConversation(c with { Messages = Array.AsReadOnly(c.Messages.Concat(pair).ToArray()) });
            RecordEvent("DialogueTurnStored", conversationId, null, "Input and reply stored atomically; no host action.");
            return Array.AsReadOnly(pair);
        }
    }
}
