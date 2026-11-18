namespace Grid.Communication;

public enum ChallengeSessionStatus { Pending, Passed, Failed, Expired, Locked, Cancelled }

public sealed record ChallengeSessionBinding(
    Guid HostInstanceId,
    Guid WorldId,
    long WorldGeneration,
    string UserId,
    string EndpointId,
    string ContactId,
    string Purpose);

public sealed record PairedChallengeEndpoint(
    Guid GrantId,
    string EndpointId,
    Guid HostInstanceId,
    Guid WorldId,
    long WorldGeneration,
    DateTimeOffset ExpiresUtc);

public sealed record ChallengePrompt(Guid SessionId, Guid ChallengeId, string Prompt, DateTimeOffset ExpiresUtc);

public sealed record ChallengeSessionResult(
    ChallengeSessionStatus Status,
    ChallengePrompt? NextPrompt = null,
    Guid? ClaimId = null,
    DateTimeOffset? ClaimExpiresUtc = null,
    ChallengeSessionBinding? Binding = null);

public sealed record ChallengeRateLimitBucket(
    string Scope,
    IReadOnlyList<DateTimeOffset> FailuresUtc,
    DateTimeOffset? LockedUntilUtc)
{
    public int LockoutLevel { get; init; }
}

public sealed record ChallengeRateLimitState(IReadOnlyList<ChallengeRateLimitBucket> Buckets)
{
    public static ChallengeRateLimitState Empty => new([]);

    public static void Validate(ChallengeRateLimitState state)
    {
        if (state is null || state.Buckets is null || state.Buckets.Count > 65 ||
            state.Buckets.Select(b => b.Scope).Distinct(StringComparer.Ordinal).Count() != state.Buckets.Count)
            throw new InvalidDataException("Invalid challenge rate-limit state.");
        foreach (var bucket in state.Buckets)
        {
            if (string.IsNullOrWhiteSpace(bucket.Scope) || bucket.Scope.Length > 160 || bucket.Scope.Any(char.IsControl) ||
                bucket.FailuresUtc is null || bucket.FailuresUtc.Count > 8 ||
                bucket.FailuresUtc.Any(t => t.Offset != TimeSpan.Zero) ||
                bucket.LockedUntilUtc is { } locked && locked.Offset != TimeSpan.Zero || bucket.LockoutLevel is < 0 or > 8)
                throw new InvalidDataException("Invalid challenge rate-limit bucket.");
        }
    }
}
