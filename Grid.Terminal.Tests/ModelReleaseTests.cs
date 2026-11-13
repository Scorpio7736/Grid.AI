using Grid.AI;
using System.Text.Json;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class ModelReleaseTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "grid-release-" + Guid.NewGuid().ToString("N"));
    private readonly DateTimeOffset now = DateTimeOffset.Parse("2026-11-13T12:00:00Z");
    private static readonly ReleasePolicy Policy = new("fixture-v1", 0.9, 1000, 1000000, 1.2, 1.2, 0.02, 30);
    public ModelReleaseTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root, true);
    private ReleaseCandidate Candidate(string id)
    {
        var assets = new[] { "model", "prompt", "adapter", "evidence" }.Select(p =>
        {
            var text = id + " " + p;
            File.WriteAllText(Path.Combine(root, id + "-" + p), text);
            return new ModelAsset(id + "-" + p, System.Text.Encoding.UTF8.GetByteCount(text), ModelAcceptance.Hash(text), p);
        }).ToArray();
        var runtime = new RuntimeCompatibility("fixture", "1", "Windows", "x64", "cpu");
        var manifest = new ModelManifest(1, "fixture-model", id, "fixture", "synthetic", "none", "fixture", "fixture", ModelOutputKind.Text, 1024, 0, [runtime], assets);
        var definition = new ReleaseDefinition(id, "fixture-cpu", manifest, new string('A', 64), new string('B', 64), new string('C', 64), runtime, 1, "host-v1");
        return new(definition, new(ReleaseEvaluation.Hash(definition), ReleaseEvaluation.Hash(Policy), now,
            ReleaseEvaluation.RequiredChecks.Select(n => new ReleaseCheck(n, true, 1, 100, 1000)).ToArray()));
    }
    private static ReleaseCandidate ChangeCheck(ReleaseCandidate candidate, Func<ReleaseCheck, ReleaseCheck> change) =>
        candidate with { Evidence = candidate.Evidence with { Checks = candidate.Evidence.Checks.Select(change).ToArray() } };

    [Fact]
    public async Task PromotionRollbackAndRetryPreserveWorldAndRegistry()
    {
        File.WriteAllText(Path.Combine(root, "world.db"), "world sentinel");
        File.WriteAllText(Path.Combine(root, "model-registry.json"), "registry sentinel");
        var catalog = new ModelReleaseCatalog(root);
        var first = Candidate("v1");
        var second = Candidate("v2");
        Assert.True((await catalog.PromoteAsync(new(Policy, null, first), now)).Passed);
        var request = new ReleaseRequest(Policy, "v1", second);
        Assert.True((await catalog.PromoteAsync(request, now)).Passed);
        Assert.True((await catalog.PromoteAsync(request, now)).Passed);
        Assert.Equal(2, catalog.Read()!.Releases.Length);
        Assert.Equal("v1", await catalog.RollbackAsync("v2"));
        Assert.Equal("world sentinel", File.ReadAllText(Path.Combine(root, "world.db")));
        Assert.Equal("registry sentinel", File.ReadAllText(Path.Combine(root, "model-registry.json")));
    }

    [Theory]
    [InlineData("failed")]
    [InlineData("score")]
    [InlineData("latency")]
    [InlineData("memory")]
    [InlineData("missing")]
    [InlineData("future")]
    [InlineData("stale")]
    [InlineData("policy")]
    [InlineData("binding")]
    public async Task InvalidEvidenceIsRecordedWithoutPromoting(string failure)
    {
        var catalog = new ModelReleaseCatalog(root);
        var baseline = Candidate("v1");
        Assert.True((await catalog.PromoteAsync(new(Policy, null, baseline), now)).Passed);
        var candidate = Candidate("v2");
        candidate = failure switch
        {
            "failed" => ChangeCheck(candidate, c => c with { Passed = false }),
            "score" => ChangeCheck(candidate, c => c with { Score = 0.95 }),
            "latency" => ChangeCheck(candidate, c => c with { LatencyMilliseconds = 121 }),
            "memory" => ChangeCheck(candidate, c => c with { PeakMemoryBytes = 1201 }),
            "missing" => candidate with { Evidence = candidate.Evidence with { Checks = [] } },
            "future" => candidate with { Evidence = candidate.Evidence with { RecordedUtc = now.AddDays(1) } },
            "stale" => candidate with { Evidence = candidate.Evidence with { RecordedUtc = now.AddDays(-31) } },
            "policy" => candidate with { Evidence = candidate.Evidence with { PolicySha256 = new string('F', 64) } },
            _ => candidate with { Evidence = candidate.Evidence with { DefinitionSha256 = new string('F', 64) } }
        };
        Assert.False((await catalog.PromoteAsync(new(Policy, "v1", candidate), now)).Passed);
        var state = catalog.Read()!;
        Assert.Equal("v1", state.Active);
        Assert.Single(state.Releases);
        Assert.False(state.Attempts.Last().Passed);
        Assert.Equal("v2", state.Attempts.Last().Candidate.Definition.Id);
    }

    [Fact]
    public async Task RejectedFirstCandidateLeavesAuditableEmptyTrack()
    {
        var catalog = new ModelReleaseCatalog(root);
        var candidate = ChangeCheck(Candidate("bad"), c => c with { Passed = false });
        Assert.False((await catalog.PromoteAsync(new(Policy, null, candidate), now)).Passed);
        Assert.Null(catalog.Read()!.Active);
        Assert.Single(catalog.Read()!.Attempts);
        Assert.True((await catalog.PromoteAsync(new(Policy, null, Candidate("good")), now)).Passed);
    }

    [Fact]
    public async Task AssetTamperingAndMissingRollbackAreRefused()
    {
        var catalog = new ModelReleaseCatalog(root);
        await catalog.PromoteAsync(new(Policy, null, Candidate("v1")), now);
        var candidate = Candidate("v2");
        File.AppendAllText(Path.Combine(root, "v2-adapter"), "tampered");
        Assert.False((await catalog.PromoteAsync(new(Policy, "v1", candidate), now)).Passed);
        candidate = Candidate("v3");
        Assert.True((await catalog.PromoteAsync(new(Policy, "v1", candidate), now)).Passed);
        File.Delete(Path.Combine(root, "v1-model"));
        await Assert.ThrowsAnyAsync<IOException>(() => catalog.RollbackAsync("v3"));
        Assert.Equal("v3", catalog.Read()!.Active);
    }

    [Fact]
    public async Task StaleBaselineAndChangedPolicyCannotPromote()
    {
        var catalog = new ModelReleaseCatalog(root);
        await catalog.PromoteAsync(new(Policy, null, Candidate("v1")), now);
        Assert.False((await catalog.PromoteAsync(new(Policy, null, Candidate("v2")), now)).Passed);
        Assert.False((await catalog.PromoteAsync(new(Policy with { MinimumScore = 0 }, "v1", Candidate("v3")), now)).Passed);
        Assert.Equal("v1", catalog.Read()!.Active);
    }

    [Fact]
    public async Task CompatibilityChangesRequireSeparateTrack()
    {
        var catalog = new ModelReleaseCatalog(root);
        await catalog.PromoteAsync(new(Policy, null, Candidate("v1")), now);
        var candidate = Candidate("v2");
        var definition = candidate.Definition with { WorldSchema = 2 };
        candidate = candidate with { Definition = definition, Evidence = candidate.Evidence with { DefinitionSha256 = ReleaseEvaluation.Hash(definition) } };
        Assert.False((await catalog.PromoteAsync(new(Policy, "v1", candidate), now)).Passed);
    }

    [Fact]
    public async Task CancellationKeepsPreviousPointer()
    {
        var catalog = new ModelReleaseCatalog(root);
        await catalog.PromoteAsync(new(Policy, null, Candidate("v1")), now);
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => catalog.PromoteAsync(new(Policy, "v1", Candidate("v2")), now, stop.Token));
        Assert.Equal("v1", catalog.Read()!.Active);
    }

    [Fact]
    public void MalformedCheckValuesAndCatalogArtifactsAreRejected()
    {
        var candidate = Candidate("v1");
        Assert.False(ReleaseEvaluation.Evaluate(Policy, ChangeCheck(candidate, c => c with { Score = double.NaN }), null, now).Passed);
        Assert.False(ReleaseEvaluation.Evaluate(Policy, ChangeCheck(candidate, c => c with { LatencyMilliseconds = 0 }), null, now).Passed);
        var manifest = candidate.Definition.Manifest with { Assets = [new("model-release-catalog.json", 0, new string('A', 64), "prompt")] };
        Assert.Throws<InvalidDataException>(() => ReleaseEvaluation.ValidateDefinition(candidate.Definition with { Manifest = manifest }));
    }

    [Fact]
    public async Task ConcurrentPromotionsKeepOneConsistentPointer()
    {
        var catalog = new ModelReleaseCatalog(root);
        await catalog.PromoteAsync(new(Policy, null, Candidate("v1")), now);
        var requests = new[] { new ReleaseRequest(Policy, "v1", Candidate("v2")), new ReleaseRequest(Policy, "v1", Candidate("v3")) };
        var outcomes = await Task.WhenAll(requests.Select(async request =>
        {
            try { return (await new ModelReleaseCatalog(root).PromoteAsync(request, now)).Passed; }
            catch (IOException) { return false; }
        }));
        Assert.Single(outcomes, passed => passed);
        var state = catalog.Read()!;
        Assert.Equal(2, state.Releases.Length);
        Assert.Contains(state.Active, new[] { "v2", "v3" });
        Assert.Equal("v1", await catalog.RollbackAsync(state.Active!));
    }

    [Fact]
    public async Task CommandsEvaluatePromoteAndRollback()
    {
        var first = Candidate("v1");
        var second = Candidate("v2");
        // Command evidence uses the actual execution time.
        first = first with { Evidence = first.Evidence with { RecordedUtc = DateTimeOffset.UtcNow } };
        second = second with { Evidence = second.Evidence with { RecordedUtc = DateTimeOffset.UtcNow } };
        var file = Path.Combine(root, "request.json");
        File.WriteAllText(file, JsonSerializer.Serialize(new ReleaseRequest(Policy, null, first)));
        Assert.Equal(0, await ReleaseCommands.RunAsync(["--release-check", root, file]));
        Assert.False(File.Exists(Path.Combine(root, "model-release-catalog.json")));
        Assert.Equal(0, await ReleaseCommands.RunAsync(["--release-promote", root, file]));
        File.WriteAllText(file, JsonSerializer.Serialize(new ReleaseRequest(Policy, "v1", second)));
        Assert.Equal(0, await ReleaseCommands.RunAsync(["--release-promote", root, file]));
        Assert.Equal(0, await ReleaseCommands.RunAsync(["--release-rollback", root, "v2"]));
        Assert.Equal(2, await ReleaseCommands.RunAsync(["--release-check"]));
    }
}
