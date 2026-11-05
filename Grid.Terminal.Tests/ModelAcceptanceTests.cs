using Grid.AI;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class ModelAcceptanceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);
    private static AcceptanceConfiguration Config => new(new('A', 64), new('B', 64), new('C', 64), ModelAcceptance.TextSuite, "cpu", 4096, 256);
    private static ModelAcceptanceReport Report => new(1, "qwen3-4b", Config, Now,
        ModelAcceptance.RequiredCases.Select(id => new AcceptanceCase(id, true, true, 100, "fixture", "")).ToArray(),
        true, 20, true, true, true, 4_000_000_000, 0, false, ["test fixture"]);

    [Fact]
    public void PassingSmokeEvidenceDoesNotCertifyNetworkIsolation()
    {
        Assert.True(ModelAcceptance.Evaluate(Report, Config, Now).Passed);
        Assert.False(ModelAcceptance.Evaluate(Report, Config, Now, requireNetworkIsolation: true).Passed);
    }

    [Theory]
    [InlineData("model")]
    [InlineData("runtime")]
    [InlineData("device")]
    [InlineData("suite")]
    [InlineData("mode")]
    [InlineData("context")]
    [InlineData("output")]
    public void ChangedConfigurationCannotReuseEvidence(string changed)
    {
        var expected = changed switch
        {
            "model" => Config with { ManifestHash = new('D', 64) },
            "runtime" => Config with { RuntimeHash = new('D', 64) },
            "device" => Config with { DeviceHash = new('D', 64) },
            "suite" => Config with { Suite = "different" },
            "mode" => Config with { Mode = "automatic" },
            "context" => Config with { ContextTokens = 8192 },
            _ => Config with { OutputTokens = 512 }
        };
        Assert.False(ModelAcceptance.Evaluate(Report, expected, Now).Passed);
    }

    [Theory]
    [InlineData(-31)]
    [InlineData(1)]
    public void ExpiredOrFutureEvidenceIsRejected(int days)
        => Assert.False(ModelAcceptance.Evaluate(Report with { RecordedUtc = Now.AddDays(days) }, Config, Now).Passed);

    [Fact]
    public void FailedTruncatedSlowMissingAndDuplicateCasesCannotPass()
    {
        foreach (var item in new[] { Report.Cases[0] with { Passed = false }, Report.Cases[0] with { Complete = false },
                     Report.Cases[0] with { ElapsedMilliseconds = 120001 }, Report.Cases[0] with { Id = Report.Cases[1].Id } })
            Assert.False(ModelAcceptance.Evaluate(Report with { Cases = Report.Cases.Skip(1).Prepend(item).ToArray() }, Config, Now).Passed);
        Assert.False(ModelAcceptance.Evaluate(Report with { Cases = [] }, Config, Now).Passed);
    }

    [Fact]
    public void LifecycleAndMeasurementGapsCannotPass()
    {
        foreach (var report in new[] { Report with { StreamCompleted = false }, Report with { FirstTextMilliseconds = null },
                     Report with { Cancelled = false }, Report with { Recovered = false }, Report with { Unloaded = false },
                     Report with { ObservedAllocationBytes = null }, Report with { ObservedGpuBytes = null }, Report with { ObservedGpuBytes = 100 },
                     Report with { ModelId = "flux.2-klein-4b" } })
            Assert.False(ModelAcceptance.Evaluate(report, Config, Now).Passed);
    }

    [Fact]
    public async Task RuntimeFingerprintChangesForSameSizedContentAndAddedDependencies()
    {
        var root = Path.Combine(Path.GetTempPath(), "grid-evidence-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try
        {
            var file = Path.Combine(root, "runtime.dll"); await File.WriteAllTextAsync(file, "aaaa");
            var first = await ModelAcceptanceVerification.HashRuntimeAsync(root, default);
            await File.WriteAllTextAsync(file, "bbbb"); var second = await ModelAcceptanceVerification.HashRuntimeAsync(root, default);
            Assert.NotEqual(first, second);
            await File.WriteAllTextAsync(Path.Combine(root, "backend.dll"), "backend");
            Assert.NotEqual(second, await ModelAcceptanceVerification.HashRuntimeAsync(root, default));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void EvaluationReportsRemainMutableWhileWeightsStaySealed()
    {
        Assert.Equal("mutable", PackageBudget.Category("runtime/model-evaluations/qwen3-4b-cpu.json"));
        Assert.Equal("base", PackageBudget.Category("runtime/models/weights"));
    }
}
