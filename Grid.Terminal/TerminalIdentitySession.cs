using Grid.Communication;
using Grid.Simulation;
using Grid.Simulation.Editing;
using System.Security.Principal;
namespace Grid.Terminal;

/// <summary>Local Windows owner session. Serializes commands, switching and edit leases in authorization-before-engine order.</summary>
public sealed class TerminalIdentitySession(Func<string?> verifiedOsSubject, TimeProvider? clock = null) : IProgramEditAuthorizer
{
    private readonly object gate = new();
    private GridSimulationEngine? engine;
    private readonly Guid sessionId = Guid.NewGuid();
    private long generation;
    private bool authenticated;
    private DateTimeOffset expiresAt;
    public Action? PersistIdentity { get; set; }
    private Guid? agent;
    public event Action? Changed;
    public event Action? LoginSucceeded;
    public void Attach(GridSimulationEngine value) { lock (gate) engine = value; }
    private GridSimulationEngine Engine => engine ?? throw new InvalidOperationException("Identity session is not attached.");
    public static string? WindowsSubject() => OperatingSystem.IsWindows() ? WindowsIdentity.GetCurrent().User?.Value : null;
    private bool OwnerMatches() => authenticated && (clock ?? TimeProvider.System).GetUtcNow() < expiresAt && verifiedOsSubject() is { } subject && Engine.GetSnapshot().Identities.OwnerSubject == subject;
    public bool IsMaster { get { lock (gate) return OwnerMatches() && agent is null; } }
    public long Generation { get { lock (gate) return generation; } }
    public EditSessionReference Reference { get { lock (gate) return new(sessionId, generation); } }
    public Recipient CurrentRecipient
    {
        get { lock (gate) {
            if (!OwnerMatches()) throw new InvalidOperationException("Login required.");
            return agent is { } id ? Engine.ResolveProgram(id.ToString()) : Engine.LocalOperator;
        } }
    }
    public string Prompt { get { lock (gate) return !OwnerMatches() ? "grid[locked]> " : agent is { } id ? $"grid[agent:{id.ToString()[..8]}]> " : "grid[Master Control]> "; } }
    public bool Allows(string capability)
    {
        lock (gate)
        {
            if (capability == "public") return true;
            if (!OwnerMatches()) return false;
            if (agent is null) return true;
            if (!Engine.GetSnapshot().Identities.RegisteredAgents.Contains(agent.Value)) return false;
            return capability is "session" or "chat" or "self";
        }
    }
    public CommandResult Execute(string capability, Func<CommandResult> action, string commandName = "command")
    {
        lock (gate)
        {
            if (!Allows(capability)) { Engine.AuditIdentity("Denied:" + capability, agent, generation, false); return new("Access denied. Use login master; this action requires an authorized role."); }
            try { var result = action(); Engine.AuditIdentity("CommandAllowed:" + commandName, agent, generation, true); return result; }
            catch { Engine.AuditIdentity("CommandFailed:" + commandName, agent, generation, false); throw; }
        }
    }
    public string Login()
    {
        lock (gate)
        {
            if (OwnerMatches()) return "Already logged in. Use switch master or switch <agent-id|name>, or logout.";
            if (authenticated) { authenticated = false; agent = null; Reset(); }
            var subject = verifiedOsSubject();
            if (string.IsNullOrWhiteSpace(subject) || !Engine.BindLocalOwner(subject))
            { Engine.AuditIdentity("Login", null, generation, false); return "Login denied: current OS identity does not match the world owner, or OS authentication is unavailable."; }
            PersistIdentity?.Invoke();
            expiresAt = (clock ?? TimeProvider.System).GetUtcNow().AddHours(8);
            authenticated = true; agent = null; Reset(); Engine.AuditIdentity("Login", null, generation, true); LoginSucceeded?.Invoke();
            return "Logged in as Master Control using the verified Windows account. No Windows elevation is granted.";
        }
    }
    public string Switch(string target)
    {
        lock (gate)
        {
            if (!OwnerMatches()) return "Login required.";
            Guid? next = null;
            if (!target.Equals("master", StringComparison.OrdinalIgnoreCase))
            {
                next = Engine.ResolveProgram(target).Id;
                if (!Engine.GetSnapshot().Identities.RegisteredAgents.Contains(next.Value))
                { Engine.AuditIdentity("Switch", next, generation, false); return "Agent identity is not registered. Master Control must use register-agent first."; }
            }
            agent = next; Reset(); Engine.AuditIdentity("Switch", agent, generation, true);
            return agent is null ? "Active identity: Master Control." : $"Active agent: {Engine.ResolveProgram(agent.Value.ToString()).Name}. Admin capabilities are disabled.";
        }
    }
    public string Logout()
    {
        lock (gate) { Engine.AuditIdentity("Logout", agent, generation, authenticated); authenticated = false; agent = null; Reset(); return "Logged out. Session context and pending previews cleared."; }
    }
    private void Reset() { generation = checked(generation + 1); Changed?.Invoke(); }
    public string Describe()
    {
        lock (gate) return !OwnerMatches() ? "Not logged in. Use login master." :
            $"Operator: {Engine.LocalOperator.Id}; active identity: {(agent is null ? "Master Control" : CurrentRecipient.Name)}; session {sessionId}; generation {generation}; capabilities: {(agent is null ? "world.admin, programs.edit, session, chat, self" : "session, chat, self")}.";
    }
    public IProgramEditLease? Acquire(EditSessionReference session, Guid worldInstanceId)
    {
        Monitor.Enter(gate);
        try
        {
            if (!IsMaster || session.SessionId != sessionId || session.Generation != generation || worldInstanceId != Engine.WorldInstanceId)
            { Monitor.Exit(gate); return null; }
            return new Lease(gate, new(Engine.LocalOperator.Id, sessionId, generation, worldInstanceId, true, null, 1));
        }
        catch { Monitor.Exit(gate); throw; }
    }
    private sealed class Lease(object gate, ProgramEditIdentity identity) : IProgramEditLease
    {
        public ProgramEditIdentity Identity => identity;
        public void Dispose() => Monitor.Exit(gate);
    }
}
