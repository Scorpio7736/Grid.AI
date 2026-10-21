using System.Collections.Concurrent;
using Grid.Simulation.Editing;
namespace Grid.Terminal;

public sealed class TerminalSessionRegistry : IProgramEditAuthorizer
{
    private readonly ConcurrentDictionary<Guid, TerminalIdentitySession> sessions = new();
    public void Add(TerminalIdentitySession session) => sessions[session.SessionId] = session;
    public void Remove(TerminalIdentitySession session) => sessions.TryRemove(session.SessionId, out _);
    public IProgramEditLease? AcquireTask(EditSessionReference reference, Guid world) =>
        sessions.TryGetValue(reference.SessionId, out var session) ? session.AcquireTask(reference, world) : null;
    public IProgramEditLease? Acquire(EditSessionReference reference, Guid world) =>
        sessions.TryGetValue(reference.SessionId, out var session) ? session.Acquire(reference, world) : null;
}
