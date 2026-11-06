namespace Grid.AI;

/// <summary>Host-supplied scope data, not a credential. Model text must never populate this contract.</summary>
public sealed record TrustedRequestContext(Guid WorldId, Guid UserId, Guid CallerId, Guid AgentId,
    Guid SessionId, long Generation, Guid ConversationId, string Project, string Capability);
