using System.Text.Json;

namespace Grid.AI;

public sealed record ReleasePolicy(string Version, double MinimumScore, long MaximumLatencyMilliseconds, long MaximumMemoryBytes,
    double MaximumLatencyRatio, double MaximumMemoryRatio, double MaximumScoreDrop, int EvidenceDays);
public sealed record ReleaseDefinition(string Id, string Scope, ModelManifest Manifest, string DatasetSha256, string SuiteSha256, string DeviceSha256,
    RuntimeCompatibility Runtime, int WorldSchema, string HostApi);
public sealed record ReleaseCheck(string Name, bool Passed, double Score, long LatencyMilliseconds, long PeakMemoryBytes);
public sealed record ReleaseEvidence(string DefinitionSha256, string PolicySha256, DateTimeOffset RecordedUtc,
    ReleaseCheck[] Checks);
public sealed record ReleaseCandidate(ReleaseDefinition Definition, ReleaseEvidence Evidence);
public sealed record ReleaseRequest(ReleasePolicy Policy, string? ExpectedActive, ReleaseCandidate Candidate);
public sealed record ReleaseDecision(bool Passed, string Reason);
public sealed record AcceptedRelease(ReleaseCandidate Candidate, string? PreviousId);
public sealed record ReleaseAttempt(ReleaseCandidate Candidate, string CandidateSha256, string? BaselineId,
    string PolicySha256, DateTimeOffset RecordedUtc, bool Passed, string Reason);
public sealed record ReleaseCatalogState(int Schema, ReleasePolicy Policy, string? Active,
    AcceptedRelease[] Releases, ReleaseAttempt[] Attempts);

/// <summary>Deterministic evidence comparison. Test results are operator-supplied attestations.</summary>
public static class ReleaseEvaluation
{
    public static IReadOnlyList<string> RequiredChecks { get; } = Array.AsReadOnly(new[] { "functional", "adversarial", "regression", "performance", "compatibility", "migration", "rollback" });
    public static string Hash<T>(T value) => ModelAcceptance.Hash(JsonSerializer.Serialize(value));

    public static void ValidatePolicy(ReleasePolicy policy)
    {
        if (policy is null || !Label(policy.Version) || policy.MaximumLatencyMilliseconds <= 0 || policy.MaximumMemoryBytes <= 0 ||
            !double.IsFinite(policy.MinimumScore) || policy.MinimumScore is < 0 or > 1 ||
            !double.IsFinite(policy.MaximumLatencyRatio) || policy.MaximumLatencyRatio < 1 ||
            !double.IsFinite(policy.MaximumMemoryRatio) || policy.MaximumMemoryRatio < 1 ||
            !double.IsFinite(policy.MaximumScoreDrop) || policy.MaximumScoreDrop is < 0 or > 1 || policy.EvidenceDays is < 1 or > 365)
            throw new InvalidDataException("Invalid release thresholds. Record criteria before candidate evaluation.");
    }

    public static void ValidateDefinition(ReleaseDefinition definition)
    {
        if (definition is null || !Label(definition.Id) || !Label(definition.Scope) || !Label(definition.HostApi) ||
            definition.WorldSchema < 1 || !Digest(definition.DatasetSha256) || !Digest(definition.SuiteSha256) || !Digest(definition.DeviceSha256) ||
            definition.Manifest is null || definition.Runtime is null)
            throw new InvalidDataException("Incomplete release identity, dataset or compatibility scope.");
        ModelRegistry.ValidateManifest(definition.Manifest);
        if (definition.Manifest.Assets.Any(a => a.Path.Replace('\\', '/').Split('/')[0].StartsWith("model-release-catalog", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("Catalog control files cannot be release artifacts.");
        if (!ModelRegistry.Compatible(definition.Manifest, definition.Runtime) ||
            !definition.Manifest.Assets.Any(a => a.Purpose == "prompt") || !definition.Manifest.Assets.Any(a => a.Purpose == "evidence"))
            throw new InvalidDataException("Release needs an accepted runtime declaration, versioned prompt and evidence artifacts.");
    }

    public static ReleaseDecision Evaluate(ReleasePolicy policy, ReleaseCandidate candidate, ReleaseCandidate? baseline, DateTimeOffset now)
    {
        ValidatePolicy(policy);
        if (candidate is null) return new(false, "Candidate is missing.");
        ValidateDefinition(candidate.Definition);
        var evidence = candidate.Evidence;
        if (evidence is null || evidence.DefinitionSha256 != Hash(candidate.Definition) || evidence.PolicySha256 != Hash(policy))
            return new(false, "Evidence is bound to different release assets or criteria.");
        if (evidence.RecordedUtc > now || now - evidence.RecordedUtc > TimeSpan.FromDays(policy.EvidenceDays))
            return new(false, "Evidence is expired or future-dated.");
        if (evidence.Checks is null || evidence.Checks.Any(c => c is null) ||
            !evidence.Checks.Select(c => c.Name).Order(StringComparer.Ordinal).SequenceEqual(RequiredChecks.Order(StringComparer.Ordinal)))
            return new(false, "Required release checks are missing or duplicated.");
        foreach (var check in evidence.Checks)
        {
            if (!check.Passed || !double.IsFinite(check.Score) || check.Score < policy.MinimumScore || check.Score > 1 ||
                check.LatencyMilliseconds < 0 || check.LatencyMilliseconds > policy.MaximumLatencyMilliseconds ||
                check.PeakMemoryBytes < 0 || check.PeakMemoryBytes > policy.MaximumMemoryBytes ||
                check.Name == "performance" && (check.LatencyMilliseconds == 0 || check.PeakMemoryBytes == 0))
                return new(false, "A required check failed, is incomplete or exceeds absolute thresholds.");
        }
        if (baseline is null) return new(true, "Initial known-good release accepted for its declared scope.");
        var baselineDefinition = baseline.Definition;
        if (candidate.Definition.Scope != baselineDefinition.Scope || candidate.Definition.DatasetSha256 != baselineDefinition.DatasetSha256 ||
            candidate.Definition.SuiteSha256 != baselineDefinition.SuiteSha256 || candidate.Definition.DeviceSha256 != baselineDefinition.DeviceSha256 ||
            candidate.Definition.Runtime != baselineDefinition.Runtime || candidate.Definition.HostApi != baselineDefinition.HostApi ||
            candidate.Definition.WorldSchema != baselineDefinition.WorldSchema)
            return new(false, "Evaluation scope, runtime, dataset or host compatibility changed. Use a separately reviewed release track.");
        if (baseline.Evidence is null || !Evaluate(policy, baseline, null, baseline.Evidence.RecordedUtc).Passed)
            return new(false, "Baseline lacks comparable evidence.");
        foreach (var check in evidence.Checks)
        {
            var prior = baseline.Evidence.Checks.SingleOrDefault(c => c.Name == check.Name);
            if (prior is null || !prior.Passed || !double.IsFinite(prior.Score) || check.Score < prior.Score - policy.MaximumScoreDrop ||
                prior.LatencyMilliseconds > 0 && check.LatencyMilliseconds > prior.LatencyMilliseconds * policy.MaximumLatencyRatio ||
                prior.PeakMemoryBytes > 0 && check.PeakMemoryBytes > prior.PeakMemoryBytes * policy.MaximumMemoryRatio)
                return new(false, "Candidate regresses against the recorded baseline beyond allowed thresholds.");
        }
        return new(true, "Candidate meets the recorded absolute and baseline-relative release criteria.");
    }
    private static bool Label(string? text) => !string.IsNullOrWhiteSpace(text) && text.Length <= 256 && !text.Any(char.IsControl);
    private static bool Digest(string? text) => text is { Length: 64 } && text.All(Uri.IsHexDigit);
}

/// <summary>Atomic development release pointer with immutable accepted evidence and verified rollback assets.</summary>
public sealed class ModelReleaseCatalog(string assetRoot)
{
    private readonly string root = Path.GetFullPath(assetRoot);
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private string StatePath => Path.Combine(root, "model-release-catalog.json");

    public ReleaseCatalogState? Read()
    {
        CheckPath(StatePath);
        if (!File.Exists(StatePath)) return null;
        if (new FileInfo(StatePath).Length > 8 * 1024 * 1024) throw new InvalidDataException("Release catalog exceeds its metadata limit.");
        var state = JsonSerializer.Deserialize<ReleaseCatalogState>(File.ReadAllBytes(StatePath)) ?? throw new InvalidDataException("Empty release catalog.");
        ReleaseEvaluation.ValidatePolicy(state.Policy);
        if (state.Schema != 1 || state.Releases is null || state.Releases.Length > 128 || state.Attempts is null || state.Attempts.Length > 512 ||
            state.Releases.Any(r => r?.Candidate?.Definition is null) || state.Releases.Select(r => r.Candidate.Definition.Id).Distinct().Count() != state.Releases.Length ||
            (state.Active is null ? state.Releases.Length != 0 : !state.Releases.Any(r => r.Candidate.Definition.Id == state.Active)))
            throw new InvalidDataException("Invalid release catalog state.");
        var earlier = new HashSet<string>(StringComparer.Ordinal);
        foreach (var release in state.Releases)
        {
            if (release.PreviousId is not null && !earlier.Contains(release.PreviousId)) throw new InvalidDataException("Invalid rollback lineage.");
            var baseline = state.Releases.FirstOrDefault(r => r.Candidate.Definition.Id == release.PreviousId)?.Candidate;
            // Admission evidence remains historical. New promotions require fresh candidate evidence.
            if (release.Candidate.Evidence is null || !ReleaseEvaluation.Evaluate(state.Policy, release.Candidate, baseline, release.Candidate.Evidence.RecordedUtc).Passed)
                throw new InvalidDataException("Accepted release evidence is invalid.");
            earlier.Add(release.Candidate.Definition.Id);
        }
        return state;
    }

    public async Task<ReleaseDecision> CheckAsync(ReleaseRequest request, DateTimeOffset now, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request = JsonSerializer.Deserialize<ReleaseRequest>(JsonSerializer.Serialize(request))!;
        var state = Read();
        return await CheckAsync(request, state, now, token);
    }

    private async Task<ReleaseDecision> CheckAsync(ReleaseRequest request, ReleaseCatalogState? state, DateTimeOffset now, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(request);
        ReleaseEvaluation.ValidatePolicy(request.Policy);
        if (state?.Active != request.ExpectedActive) return new(false, "Active baseline changed. Reevaluate against the current release.");
        if (state is not null && ReleaseEvaluation.Hash(state.Policy) != ReleaseEvaluation.Hash(request.Policy))
            return new(false, "Promotion criteria differ from the established release track.");
        var baseline = state?.Releases.SingleOrDefault(r => r.Candidate.Definition.Id == state.Active)?.Candidate;
        var decision = ReleaseEvaluation.Evaluate(request.Policy, request.Candidate, baseline, now);
        if (!decision.Passed) return decision;
        try
        {
            if (baseline is not null) await VerifyAssetsAsync(baseline.Definition.Manifest, token);
            await VerifyAssetsAsync(request.Candidate.Definition.Manifest, token);
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException)
        { return new(false, "Release artifact verification failed: " + error.Message); }
        return decision;
    }

    public async Task<ReleaseDecision> PromoteAsync(ReleaseRequest request, DateTimeOffset now, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request = JsonSerializer.Deserialize<ReleaseRequest>(JsonSerializer.Serialize(request))!;
        var candidate = request.Candidate ?? throw new InvalidDataException("Candidate is missing.");
        ReleaseEvaluation.ValidateDefinition(candidate.Definition);
        CheckPath(root);
        Directory.CreateDirectory(root);
        using var lease = Lock();
        var state = Read();
        var existing = state?.Releases.FirstOrDefault(r => r.Candidate.Definition.Id == candidate.Definition.Id);
        if (existing is not null && state!.Active == candidate.Definition.Id &&
            ReleaseEvaluation.Hash(state.Policy) == ReleaseEvaluation.Hash(request.Policy) &&
            ReleaseEvaluation.Hash(existing.Candidate) == ReleaseEvaluation.Hash(candidate))
        {
            await VerifyAssetsAsync(candidate.Definition.Manifest, token);
            return new(true, "Exact release is already active in this catalog.");
        }
        var decision = await CheckAsync(request, state, now, token);
        if (existing is not null)
        {
            decision = new(false, "Release ID is immutable. Use a new candidate ID.");
        }
        var releases = state?.Releases ?? [];
        if (decision.Passed)
        {
            if (releases.Length >= 128) throw new IOException("Release catalog is full. Archive this track before creating another.");
            releases = [..releases, new(candidate, state?.Active)];
        }
        var attempts = (state?.Attempts ?? []).Append(new(candidate, ReleaseEvaluation.Hash(candidate), state?.Active,
            ReleaseEvaluation.Hash(request.Policy), now, decision.Passed, decision.Reason)).TakeLast(512).ToArray();
        var next = new ReleaseCatalogState(1, state?.Policy ?? request.Policy, decision.Passed ? candidate.Definition.Id : state?.Active, releases, attempts);
        Save(next, token);
        return decision;
    }

    public async Task<string> RollbackAsync(string expectedActive, CancellationToken token = default)
    {
        using var lease = Lock();
        var state = Read() ?? throw new InvalidDataException("Release catalog is empty.");
        if (state.Active != expectedActive) throw new InvalidDataException("Active release changed. Refresh before rollback.");
        var current = state.Releases.Single(r => r.Candidate.Definition.Id == state.Active);
        var previous = state.Releases.SingleOrDefault(r => r.Candidate.Definition.Id == current.PreviousId)
            ?? throw new InvalidDataException("No earlier accepted release is available.");
        await VerifyAssetsAsync(previous.Candidate.Definition.Manifest, token);
        Save(state with { Active = previous.Candidate.Definition.Id }, token);
        return previous.Candidate.Definition.Id;
    }

    private Task<RegistryVerification> VerifyAssetsAsync(ModelManifest manifest, CancellationToken token) =>
        new ModelRegistry(root).VerifyAsync(new(1, [new(manifest, null)], new() { [manifest.Id] = manifest.Revision }, false), token);
    private FileStream Lock()
    {
        var path = Path.Combine(root, "model-release-catalog.lock");
        CheckPath(path);
        return new(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }
    private void Save(ReleaseCatalogState state, CancellationToken token)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(state, Json);
        if (data.Length > 8 * 1024 * 1024) throw new IOException("Release metadata exceeds 8 MiB.");
        var temporary = StatePath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { file.Write(data); file.Flush(true); }
            token.ThrowIfCancellationRequested();
            CheckPath(StatePath);
            File.Move(temporary, StatePath, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    private static void CheckPath(string path)
    {
        for (var current = Path.GetFullPath(path); current is not null; current = Path.GetDirectoryName(current))
            if ((File.Exists(current) || Directory.Exists(current)) && File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
                throw new IOException("Release metadata requires ordinary files and directories.");
    }
}
