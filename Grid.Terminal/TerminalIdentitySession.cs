using Grid.Communication;
using Grid.Simulation;
using Grid.Simulation.Editing;
using System.Security.Principal;
namespace Grid.Terminal;

/// <summary>Local Windows owner session. Serializes commands, switching and edit leases in authorization-before-engine order.</summary>
public sealed class TerminalIdentitySession(Func<string?> verifiedOsSubject, TimeProvider? clock = null) : IProgramEditAuthorizer
{
    private object gate = new();
    private TerminalIdentitySession? parent;
    private long parentGeneration;
    private Guid? attachmentAgent;
    public bool IsAttachment => parent is not null;
    public Guid SessionId => sessionId;
    public TerminalIdentitySession CreateAttachment()
    {
        lock (gate)
        {
            if (!OwnerMatches()) throw new UnauthorizedAccessException("Login required to open a window.");
            return new TerminalIdentitySession(verifiedOsSubject, clock) { gate = gate, engine = Engine, parent = this, parentGeneration = generation, attachmentAgent = agent, agent = agent, authenticated = true, authenticatedCredential = authenticatedCredential, expiresAt = expiresAt };
        }
    }
    private GridSimulationEngine? engine;
    private readonly Guid sessionId = Guid.NewGuid();
    private long generation;
    private bool authenticated;
    private DateTimeOffset expiresAt;
    private PasswordCredential? authenticatedCredential;
    private int failedPasswords;
    private DateTimeOffset retryAfter;
    public bool NeedsSetup => Engine.GetSnapshot().Identities is { OwnerSubject: null, Credential: null };
    public bool UsesPassword => Engine.GetSnapshot().Identities.Credential is not null;
    public bool IsAuthenticated { get { lock (gate) return OwnerMatches(); } }
    public string DisplayName => UsesPassword ? Engine.GetSnapshot().Identities.Credential!.Username : "Windows owner";
    public void RevokeExpired()
    {
        lock (gate) if (authenticated && !OwnerMatches()) { authenticated = false; agent = null; authenticatedCredential = null; Reset(); Engine.AuditIdentity("Expired", null, generation, false); }
    }
    public string ConfigureCredentials(string username, string password, string confirmation)
    {
        lock (gate)
        {
            if (IsAttachment) return "Access denied. Change credentials in the original host terminal.";
            if (!NeedsSetup && !IsMaster) return "Access denied. Authenticate the existing owner as Master Control before configuring credentials.";
            if (password != confirmation) return "Error: passwords do not match.";
            var credential = PasswordCredential.Create(username, password);
            authenticated = false; agent = null; authenticatedCredential = null; Reset();
            Engine.SetLocalCredential(credential); PersistIdentity?.Invoke();
            return "Local credentials saved. Log in with your username and password; Windows-owner bypass is disabled.";
        }
    }
    public string LoginPassword(string username, string password)
    {
        lock (gate)
        {
            if (IsAttachment) return "Login denied. Reopen ntm from an authenticated terminal.";
            if (OwnerMatches()) return "Already logged in.";
            var now = (clock ?? TimeProvider.System).GetUtcNow();
            if (now < retryAfter) return "Login denied. Wait 30 seconds after repeated failures before retrying.";
            var credential = Engine.GetSnapshot().Identities.Credential;
            if (credential is null || !credential.Verify(username, password))
            {
                if (++failedPasswords >= 5) { retryAfter = now.AddSeconds(30); failedPasswords = 0; }
                Engine.AuditIdentity("PasswordLogin", null, generation, false); return "Login denied: invalid credentials.";
            }
            PersistIdentity?.Invoke(); failedPasswords = 0; retryAfter = default;
            authenticatedCredential = credential; expiresAt = now.AddHours(8); authenticated = true; agent = null; Reset();
            Engine.AuditIdentity("PasswordLogin", null, generation, true); LoginSucceeded?.Invoke();
            return "Logged in as Master Control using local credentials.";
        }
    }
    public Action? PersistIdentity { get; set; }
    private Guid? agent;
    public event Action? Changed;
    public event Action? LoginSucceeded;
    public void Attach(GridSimulationEngine value) { lock (gate) engine = value; }
    internal GridSimulationEngine Engine => engine ?? throw new InvalidOperationException("Identity session is not attached.");
    public static string? WindowsSubject() => OperatingSystem.IsWindows() ? WindowsIdentity.GetCurrent().User?.Value : null;
    private bool OwnerMatches()
    {
        if (parent is not null && (!parent.IsAuthenticated || parent.Generation != parentGeneration)) return false;
        if (!authenticated || (clock ?? TimeProvider.System).GetUtcNow() >= expiresAt) return false;
        var saved = Engine.GetSnapshot().Identities;
        return saved.Credential is { } credential ? credential == authenticatedCredential :
            verifiedOsSubject() is { } subject && saved.OwnerSubject == subject;
    }
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
            if (IsAttachment) return "Login denied. Reopen ntm from an authenticated terminal.";
            if (UsesPassword) return "Login denied: local credentials are configured; use interactive login.";
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
            if (attachmentAgent is { } ceiling && next != ceiling) return "Access denied. This window is scoped to its opening agent.";
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
            return new Lease(gate, new(Engine.LocalOperator.Id, sessionId, generation, worldInstanceId, true, null, 1) { IsOriginalHost = !IsAttachment });
        }
        catch { Monitor.Exit(gate); throw; }
    }
    public IProgramEditLease? AcquireTask(EditSessionReference session, Guid worldInstanceId)
    {
        Monitor.Enter(gate);
        try
        {
            if (!Allows("session") || session.SessionId != sessionId || session.Generation != generation || worldInstanceId != Engine.WorldInstanceId)
            { Monitor.Exit(gate); return null; }
            return new Lease(gate, new(Engine.LocalOperator.Id, sessionId, generation, worldInstanceId, agent is null, agent, 1) { IsOriginalHost = !IsAttachment });
        }
        catch { Monitor.Exit(gate); throw; }
    }
    private sealed class Lease(object gate, ProgramEditIdentity identity) : IProgramEditLease
    {
        public ProgramEditIdentity Identity => identity;
        public void Dispose() => Monitor.Exit(gate);
    }
}
