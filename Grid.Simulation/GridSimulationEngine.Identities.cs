using Grid.Communication;
using Grid.Core;
namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    private LocalIdentityState identities = LocalIdentityState.Empty;
    // Trusted identity-provider boundaries. These do not authenticate a caller-supplied subject.
    public bool BindLocalOwner(string verifiedSubject)
    {
        lock (gate)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(verifiedSubject);
            LocalIdentityState.Validate(new(verifiedSubject, identities.RegisteredAgents));
            if (identities.OwnerSubject is { } owner) return owner == verifiedSubject;
            identities = identities with { OwnerSubject = verifiedSubject };
            RecordEvent("OwnerBound", LocalOperator.Id, "Master Control", "Local world owner bound to verified OS identity.");
            return true;
        }
    }
    public void SetLocalCredential(PasswordCredential credential)
    {
        lock (gate)
        {
            PasswordCredential.Validate(credential);
            identities = identities with { OwnerSubject = identities.OwnerSubject ?? "local-credential-v1", Credential = credential, ChallengeCredentials = null };
            RecordEvent("CredentialConfigured", LocalOperator.Id, null, "Local password credential configured; no credential material recorded in audit.");
        }
    }
    public void SetLocalCredentials(PasswordCredential credential, ChallengeCredentialSet challenges)
    {
        lock (gate)
        {
            PasswordCredential.Validate(credential);
            ChallengeCredentialSet.Validate(challenges);
            identities = identities with
            {
                OwnerSubject = identities.OwnerSubject ?? "local-credential-v1",
                Credential = credential,
                ChallengeCredentials = challenges
            };
            LocalIdentityState.Validate(identities);
            RecordEvent("CredentialConfigured", LocalOperator.Id, null,
                "Local password and challenge credentials configured; no credential material recorded in audit.");
        }
    }
    public void RegisterAgentIdentity(Guid id)
    {
        lock (gate)
        {
            if (identities.OwnerSubject is null) throw new InvalidOperationException("Bind the local operator first.");
            if (world.GetProgram(id).Kind != ResidentKind.Agent) throw new ArgumentException("Only Agent residents can be registered.");
            if (identities.RegisteredAgents.Contains(id)) return;
            var next = identities with { RegisteredAgents = Array.AsReadOnly(identities.RegisteredAgents.Append(id).ToArray()) };
            LocalIdentityState.Validate(next); identities = next;
            RecordEvent("AgentIdentityRegistered", id, world.GetProgram(id).Name, "Registered agent session identity; no admin capabilities granted.");
        }
    }
    public void AuditIdentity(string action, Guid? agent, long generation, bool allowed)
    {
        lock (gate) RecordEvent("IdentitySession", agent ?? LocalOperator.Id, null,
            $"{action}; generation {generation}; {(allowed ? "allowed" : "denied")}",
            System.Text.Json.JsonSerializer.Serialize(new { action, operatorId = LocalOperator.Id, agentId = agent, generation, allowed }));
    }
}
