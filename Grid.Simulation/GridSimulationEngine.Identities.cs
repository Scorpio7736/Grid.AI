using Grid.Communication;
using Grid.Core;
namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    private LocalIdentityState identities = LocalIdentityState.Empty;
    private readonly Dictionary<Guid, ActiveChallengeSession> activeChallengeSessions = [];
    private readonly Dictionary<Guid, ActiveChallengeClaim> activeChallengeClaims = [];
    private readonly Dictionary<Guid, PairedChallengeEndpoint> pairedChallengeEndpoints = [];
    private sealed record ActiveChallengeSession(ChallengeSessionBinding Binding, IReadOnlyList<ChallengeCredential> Selected,
        int Position, DateTimeOffset ExpiresUtc);
    private sealed record ActiveChallengeClaim(ChallengeSessionBinding Binding, DateTimeOffset ExpiresUtc);
    private static readonly TimeSpan ChallengeSessionLifetime = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ChallengeClaimLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ChallengeFailureWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ChallengeCooldown = TimeSpan.FromMinutes(15);
    private const int ChallengeFailureLimit = 3;
    private long challengeAuthorityGeneration;
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
            InvalidateChallengeAuthority();
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
            InvalidateChallengeAuthority();
            LocalIdentityState.Validate(identities);
            RecordEvent("CredentialConfigured", LocalOperator.Id, null,
                "Local password and challenge credentials configured; no credential material recorded in audit.");
        }
    }
    public bool DisableLocalChallenges()
    {
        lock (gate)
        {
            if (identities.ChallengeCredentials is null) return false;
            identities = identities with { ChallengeCredentials = null };
            InvalidateChallengeAuthority();
            LocalIdentityState.Validate(identities);
            RecordEvent("ChallengeCredentialsDisabled", LocalOperator.Id, null,
                "Local challenge credentials disabled; password credential retained. No challenge material recorded in audit.");
            return true;
        }
    }
    public OfflineRecoveryCredential? SetOfflineRecoveryCredential(Grid.Simulation.Editing.EditSessionReference session,
        OfflineRecoveryCredential credential)
    {
        using var lease = editAuthorizer.Acquire(session, world.InstanceId);
        if (!IsAuthorized(lease, session) || lease!.Identity.AgentId is not null || !lease.Identity.IsOriginalHost)
            throw new UnauthorizedAccessException("Original-host Master Control is required to enroll offline recovery.");
        lock (gate)
        {
            OfflineRecoveryCredential.Validate(credential);
            if (identities.Credential is null) throw new InvalidOperationException("Configure a local password before offline recovery.");
            var previous = identities.OfflineRecovery;
            identities = identities with { OfflineRecovery = credential };
            LocalIdentityState.Validate(identities);
            RecordEvent("OfflineRecoveryRotated", LocalOperator.Id, null,
                "User-held offline recovery verifier enrolled; plaintext key and file path were not recorded.");
            return previous;
        }
    }

    public void RestoreOfflineRecoveryCredential(Grid.Simulation.Editing.EditSessionReference session,
        OfflineRecoveryCredential expected, OfflineRecoveryCredential? previous)
    {
        using var lease = editAuthorizer.Acquire(session, world.InstanceId);
        if (!IsAuthorized(lease, session) || lease!.Identity.AgentId is not null || !lease.Identity.IsOriginalHost)
            throw new UnauthorizedAccessException("Original-host Master Control is required.");
        lock (gate)
        {
            if (identities.OfflineRecovery != expected) throw new InvalidOperationException("Recovery credential changed; rollback refused.");
            identities = identities with { OfflineRecovery = previous };
            LocalIdentityState.Validate(identities);
            RecordEvent("OfflineRecoveryRollback", LocalOperator.Id, null,
                "Failed recovery-file enrollment rolled back; no secret or path recorded.");
        }
    }
    public PairedChallengeEndpoint PairChallengeEndpoint(Grid.Simulation.Editing.EditSessionReference session,
        string endpointId, TimeSpan? lifetime = null)
    {
        using var lease = editAuthorizer.Acquire(session, world.InstanceId);
        if (!IsAuthorized(lease, session))
            throw new UnauthorizedAccessException("Authenticated Master Control is required to grant a challenge endpoint.");
        lock (gate)
        {
            var duration = lifetime ?? TimeSpan.FromHours(8);
            if (duration < TimeSpan.FromMinutes(1) || duration > TimeSpan.FromDays(30))
                throw new ArgumentOutOfRangeException(nameof(lifetime));
            var now = timeProvider.GetUtcNow();
            PurgeExpiredChallengeAuthority(now);
            if (pairedChallengeEndpoints.Count >= 256) throw new InvalidOperationException("Challenge endpoint grant limit reached.");
            var grant = new PairedChallengeEndpoint(Guid.NewGuid(), ChallengeLabel(endpointId, 128), world.InstanceId,
                world.Id, challengeAuthorityGeneration, now.Add(duration));
            pairedChallengeEndpoints.Add(grant.GrantId, grant);
            RecordEvent("IdentitySession", LocalOperator.Id, null, "ChallengeEndpointGranted; content-free endpoint grant issued.",
                System.Text.Json.JsonSerializer.Serialize(new { action = "ChallengeEndpointGranted", grantId = grant.GrantId,
                    worldId = world.Id, generation = challengeAuthorityGeneration, expiresUtc = grant.ExpiresUtc }));
            return grant;
        }
    }
    public void RevokeChallengeEndpoint(Grid.Simulation.Editing.EditSessionReference session, Guid grantId)
    {
        using var lease = editAuthorizer.Acquire(session, world.InstanceId);
        if (!IsAuthorized(lease, session))
            throw new UnauthorizedAccessException("Authenticated Master Control is required to revoke a challenge endpoint.");
        lock (gate)
        {
            var removed = pairedChallengeEndpoints.Remove(grantId);
            var endpoint = grantId.ToString("N");
            foreach (var id in activeChallengeSessions.Where(p => p.Value.Binding.EndpointId == endpoint).Select(p => p.Key).ToArray())
                activeChallengeSessions.Remove(id);
            foreach (var id in activeChallengeClaims.Where(p => p.Value.Binding.EndpointId == endpoint).Select(p => p.Key).ToArray())
                activeChallengeClaims.Remove(id);
            if (removed) RecordEvent("IdentitySession", LocalOperator.Id, null,
                "ChallengeEndpointRevoked; active attempts and claims for the grant invalidated.",
                System.Text.Json.JsonSerializer.Serialize(new { action = "ChallengeEndpointRevoked", grantId }));
        }
    }
    public ChallengeSessionResult BeginRemoteChallenge(PairedChallengeEndpoint endpoint, string contactId, string purpose)
    {
        lock (gate)
        {
            var now = timeProvider.GetUtcNow();
            PurgeExpiredChallengeAuthority(now);
            var set = identities.ChallengeCredentials;
            if (set is null || identities.Credential is null) return new(ChallengeSessionStatus.Cancelled);
            if (endpoint is null || !pairedChallengeEndpoints.TryGetValue(endpoint.GrantId, out var issuedEndpoint) ||
                issuedEndpoint != endpoint || endpoint.ExpiresUtc <= now || endpoint.HostInstanceId != world.InstanceId ||
                endpoint.WorldId != world.Id || endpoint.WorldGeneration != challengeAuthorityGeneration)
                return new(ChallengeSessionStatus.Cancelled);
            var binding = new ChallengeSessionBinding(world.InstanceId, world.Id, challengeAuthorityGeneration,
                identities.Credential.Username, endpoint.GrantId.ToString("N"), ChallengeLabel(contactId, 128), ChallengeLabel(purpose, 128));
            if (ChallengeLocked(binding.EndpointId, now))
            {
                AuditChallenge("ChallengeSessionLocked", binding, false);
                return new(ChallengeSessionStatus.Locked);
            }
            var selected = set.Challenges.OrderBy(_ => System.Security.Cryptography.RandomNumberGenerator.GetInt32(int.MaxValue)).Take(2).ToArray();
            var id = Guid.NewGuid();
            var expires = now.Add(ChallengeSessionLifetime);
            activeChallengeSessions.Add(id, new(binding, selected, 0, expires));
            AuditChallenge("ChallengeSessionIssued", binding, true);
            return new(ChallengeSessionStatus.Pending, new(id, selected[0].Id, selected[0].Prompt, expires), Binding: binding);
        }
    }
    public ChallengeSessionResult SubmitRemoteChallenge(Guid sessionId, ChallengeSessionBinding binding, Guid challengeId, string response)
    {
        lock (gate)
        {
            var now = timeProvider.GetUtcNow();
            if (!activeChallengeSessions.Remove(sessionId, out var session)) return new(ChallengeSessionStatus.Expired);
            if (now >= session.ExpiresUtc)
            {
                AuditChallenge("ChallengeSessionExpired", session.Binding, false);
                return new(ChallengeSessionStatus.Expired);
            }
            if (session.Binding != binding || binding.HostInstanceId != world.InstanceId || binding.WorldId != world.Id ||
                binding.WorldGeneration != challengeAuthorityGeneration)
            {
                AuditChallenge("ChallengeSessionCancelled", session.Binding, false);
                return new(ChallengeSessionStatus.Cancelled);
            }
            if (ChallengeLocked(binding.EndpointId, now))
            {
                AuditChallenge("ChallengeSessionLocked", binding, false);
                return new(ChallengeSessionStatus.Locked);
            }
            var expected = session.Selected[session.Position];
            var verified = expected.Id == challengeId;
            try { verified &= expected.Verify(response); }
            catch (ArgumentException) { verified = false; }
            if (!verified)
            {
                RecordChallengeFailure(binding.EndpointId, now);
                AuditChallenge("ChallengeSessionFailed", binding, false);
                return new(ChallengeSessionStatus.Failed);
            }
            var nextPosition = session.Position + 1;
            if (nextPosition < session.Selected.Count)
            {
                activeChallengeSessions.Add(sessionId, session with { Position = nextPosition });
                var next = session.Selected[nextPosition];
                return new(ChallengeSessionStatus.Pending, new(sessionId, next.Id, next.Prompt, session.ExpiresUtc), Binding: binding);
            }
            var claimId = Guid.NewGuid();
            var claimExpires = now.Add(ChallengeClaimLifetime);
            activeChallengeClaims.Add(claimId, new(binding, claimExpires));
            AuditChallenge("ChallengeSessionPassed", binding, true);
            return new(ChallengeSessionStatus.Passed, ClaimId: claimId, ClaimExpiresUtc: claimExpires, Binding: binding);
        }
    }
    public bool ValidateRemoteChallengeClaim(Guid claimId, ChallengeSessionBinding binding)
    {
        lock (gate)
        {
            var now = timeProvider.GetUtcNow();
            PurgeExpiredChallengeAuthority(now);
            return activeChallengeClaims.TryGetValue(claimId, out var claim) && claim.Binding == binding &&
                binding.HostInstanceId == world.InstanceId && binding.WorldId == world.Id && binding.WorldGeneration == challengeAuthorityGeneration;
        }
    }
    public void CancelRemoteChallenge(Guid sessionId)
    {
        lock (gate)
            if (activeChallengeSessions.Remove(sessionId, out var session)) AuditChallenge("ChallengeSessionCancelled", session.Binding, false);
    }
    private static string ChallengeLabel(string value, int maximum)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        if (value.Length > maximum || value.Any(char.IsControl)) throw new ArgumentException("Invalid challenge binding label.");
        return value;
    }
    private bool ChallengeLocked(string endpoint, DateTimeOffset now) =>
        identities.ChallengeRateLimits.Buckets.Any(b => (b.Scope == "account" || b.Scope == "endpoint:" + endpoint) && b.LockedUntilUtc > now);
    private void RecordChallengeFailure(string endpoint, DateTimeOffset now)
    {
        var scopes = new[] { "account", "endpoint:" + endpoint };
        var buckets = identities.ChallengeRateLimits.Buckets
            .Where(b => b.LockedUntilUtc > now || b.FailuresUtc.Any(t => t > now.Subtract(ChallengeFailureWindow)) || b.LockoutLevel > 0)
            .ToDictionary(b => b.Scope, StringComparer.Ordinal);
        foreach (var scope in scopes)
        {
            var recent = buckets.TryGetValue(scope, out var old)
                ? old.FailuresUtc.Where(t => t > now.Subtract(ChallengeFailureWindow)).Append(now).TakeLast(ChallengeFailureLimit).ToArray()
                : [now];
            var level = old?.LockoutLevel ?? 0;
            if (recent.Length >= ChallengeFailureLimit) level = Math.Min(level + 1, 8);
            buckets[scope] = new(scope, Array.AsReadOnly(recent), recent.Length >= ChallengeFailureLimit
                ? now.Add(TimeSpan.FromTicks(ChallengeCooldown.Ticks * Math.Min(level, 4))) : old?.LockedUntilUtc)
                { LockoutLevel = level };
        }
        if (buckets.Count > 65)
            foreach (var stale in buckets.Values.Where(b => b.Scope != "account" && !scopes.Contains(b.Scope))
                         .OrderBy(b => b.LockedUntilUtc ?? b.FailuresUtc.LastOrDefault()).Take(buckets.Count - 65).ToArray())
                buckets.Remove(stale.Scope);
        identities = identities with { ChallengeRateLimits = new(Array.AsReadOnly(buckets.Values.OrderBy(b => b.Scope).ToArray())) };
        LocalIdentityState.Validate(identities);
    }
    private void PurgeExpiredChallengeAuthority(DateTimeOffset now)
    {
        foreach (var item in activeChallengeSessions.Where(p => p.Value.ExpiresUtc <= now).ToArray())
        {
            activeChallengeSessions.Remove(item.Key);
            AuditChallenge("ChallengeSessionExpired", item.Value.Binding, false);
        }
        foreach (var item in activeChallengeClaims.Where(p => p.Value.ExpiresUtc <= now).ToArray())
        {
            activeChallengeClaims.Remove(item.Key);
            AuditChallenge("ChallengeClaimExpired", item.Value.Binding, false);
        }
        foreach (var id in pairedChallengeEndpoints.Where(p => p.Value.ExpiresUtc <= now).Select(p => p.Key).ToArray()) pairedChallengeEndpoints.Remove(id);
    }
    private void InvalidateChallengeAuthority()
    {
        activeChallengeSessions.Clear();
        activeChallengeClaims.Clear();
        pairedChallengeEndpoints.Clear();
        challengeAuthorityGeneration++;
        identities = identities with { ChallengeRateLimits = ChallengeRateLimitState.Empty };
    }
    private void AuditChallenge(string action, ChallengeSessionBinding binding, bool allowed) =>
        RecordEvent("IdentitySession", LocalOperator.Id, null,
            $"{action}; endpoint {binding.EndpointId}; purpose {binding.Purpose}; {(allowed ? "allowed" : "denied")}",
            System.Text.Json.JsonSerializer.Serialize(new { action, endpointId = binding.EndpointId, contactId = binding.ContactId,
                purpose = binding.Purpose, worldId = binding.WorldId, generation = binding.WorldGeneration, allowed }));
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
