using Grid.Communication;
using Grid.Core;
using Grid.Simulation.Editing;
namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    private ContextState context = ContextState.Empty;
    private readonly Dictionary<Guid, (ContextDocument Document, Guid Session)> temporaryContext = new();
    public void AddContext(EditSessionReference session, ContextDocument document, bool persistent)
        => AddContexts(session, [document], persistent);
    public void AddContexts(EditSessionReference session, IReadOnlyList<ContextDocument> documents, bool persistent)
    {
        using var lease = editAuthorizer.Acquire(session, world.InstanceId);
        if (!IsAuthorized(lease, session)) throw new UnauthorizedAccessException("Master Control is required to import context.");
        lock (gate)
        {
            foreach (var document in documents)
                if (document.GrantedAgentId is { } agent && (!identities.RegisteredAgents.Contains(agent) || world.GetProgram(agent).Kind != ResidentKind.Agent))
                    throw new ArgumentException("Context can only be granted to a registered Agent.");
            ContextState.Validate(new(context.Documents.Concat(temporaryContext.Values.Select(t => t.Document)).Concat(documents).ToArray()));
            foreach (var document in documents)
            {
                if (persistent) context = new(Array.AsReadOnly(context.Documents.Append(document).ToArray()));
                else temporaryContext.Add(document.Id, (document, session.SessionId));
                RecordEvent("ContextImported", document.Id, null, $"Captured {(persistent ? "persistent" : "temporary")} context; source/body excluded from audit.");
            }
        }
    }
    // Trusted identity adapters supply the active actor/session; untrusted commands never supply an actor ID.
    public IReadOnlyList<ContextDocument> ContextFor(Guid? agent, Guid session)
    {
        lock (gate)
        {
            var documents = context.Documents.Concat(temporaryContext.Values.Where(t => agent is not null || t.Session == session).Select(t => t.Document));
            return Array.AsReadOnly(documents.Where(d => agent is null || d.GrantedAgentId == agent && identities.RegisteredAgents.Contains(agent.Value)).ToArray());
        }
    }
    public void ChangeContext(EditSessionReference session, Guid id, bool remove)
    {
        using var lease = editAuthorizer.Acquire(session, world.InstanceId);
        if (!IsAuthorized(lease, session)) throw new UnauthorizedAccessException("Master Control is required to revoke or remove context.");
        lock (gate)
        {
            var document = context.Documents.SingleOrDefault(d => d.Id == id);
            if (document is not null) context = new(Array.AsReadOnly(context.Documents.Where(d => !remove || d.Id != id).Select(d => d.Id == id ? d with { GrantedAgentId = null } : d).ToArray()));
            else if (temporaryContext.TryGetValue(id, out var temporary) && temporary.Session == session.SessionId)
            {
                if (remove) temporaryContext.Remove(id); else temporaryContext[id] = (temporary.Document with { GrantedAgentId = null }, temporary.Session);
            }
            else throw new ArgumentException("Context not found in this scope.");
            RecordEvent(remove ? "ContextRemoved" : "ContextRevoked", id, null, "Context access changed; no source/body recorded.");
        }
    }
    public void ClearTemporaryContext(Guid session)
    {
        lock (gate) foreach (var id in temporaryContext.Where(p => p.Value.Session == session).Select(p => p.Key).ToArray()) temporaryContext.Remove(id);
    }
}
