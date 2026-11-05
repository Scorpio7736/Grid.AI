namespace Grid.Agent;

public enum MemoryKind { Preference, Decision, Fact, Unfinished, Outcome, Summary }
public sealed record MemoryProvenance(string Kind, Guid SourceId, string SourceHash, bool SourceWasTemporary);
public sealed record MemoryGrant(Guid AgentId, string Project, bool Revoked = false);
public sealed record MemorySource(Guid ConversationId, Guid MessageId);
/// <summary>User-confirmed context, never evidence of a host action or independently verified fact.</summary>
public sealed record ConversationMemory(Guid Id, Guid AgentId, string Project, string Key, MemoryKind Kind,
    string Content, DateTimeOffset UpdatedAtUtc, IReadOnlyList<MemorySource> Sources, bool Forgotten = false)
{
    public MemoryProvenance? Provenance { get; init; }
    public IReadOnlyList<MemoryGrant> Grants { get; init; } = Array.Empty<MemoryGrant>();
    public DateTimeOffset? ExpiresAtUtc { get; init; }
    public bool IsStale(DateTimeOffset now) => ExpiresAtUtc is { } expiry ? now >= expiry : now - UpdatedAtUtc >= TimeSpan.FromDays(30);
    public static bool ValidLabel(string? value) => value is { Length: > 0 and <= 48 } && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');
}
