namespace Grid.Communication;

public sealed record LocalIdentityState(string? OwnerSubject, IReadOnlyList<Guid> RegisteredAgents)
{
    public PasswordCredential? Credential { get; init; }
    public ChallengeCredentialSet? ChallengeCredentials { get; init; }
    public OfflineRecoveryCredential? OfflineRecovery { get; init; }
    public ChallengeRateLimitState ChallengeRateLimits { get; init; } = ChallengeRateLimitState.Empty;
    public static LocalIdentityState Empty => new(null, []);
    public static void Validate(LocalIdentityState state)
    {
        if (state is null || state.RegisteredAgents is null || state.RegisteredAgents.Count > 256 ||
            state.RegisteredAgents.Any(id => id == Guid.Empty) || state.RegisteredAgents.Distinct().Count() != state.RegisteredAgents.Count ||
            state.OwnerSubject is null && state.RegisteredAgents.Count != 0 ||
            state.OwnerSubject is { } owner && (string.IsNullOrWhiteSpace(owner) || owner.Length > 256 || owner.Any(char.IsControl)))
            throw new InvalidDataException("Invalid local identity registry.");
        if (state.Credential is { } credential) { if (state.OwnerSubject is null) throw new InvalidDataException("Credential has no owner."); PasswordCredential.Validate(credential); }
        if (state.ChallengeCredentials is { } challenges)
        {
            if (state.Credential is null) throw new InvalidDataException("Challenge credentials require a password credential.");
            ChallengeCredentialSet.Validate(challenges);
        }
        if (state.OfflineRecovery is { } recovery)
        {
            if (state.Credential is null) throw new InvalidDataException("Offline recovery requires a password credential.");
            OfflineRecoveryCredential.Validate(recovery);
        }
        ChallengeRateLimitState.Validate(state.ChallengeRateLimits);
    }
}
