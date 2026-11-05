using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Grid.AI;

public sealed record AcceptanceConfiguration(string ManifestHash, string RuntimeHash, string DeviceHash,
    string Suite, string Mode, int ContextTokens, int OutputTokens);
public sealed record AcceptanceCase(string Id, bool Passed, bool Complete, long ElapsedMilliseconds,
    string Answer, string Failure, long? OutputTokens = null, long? GenerationNanoseconds = null);
public sealed record ModelAcceptanceReport(int SchemaVersion, string ModelId, AcceptanceConfiguration Configuration,
    DateTimeOffset RecordedUtc, AcceptanceCase[] Cases, bool StreamCompleted, long? FirstTextMilliseconds,
    bool Cancelled, bool Recovered, bool Unloaded, long? ObservedAllocationBytes, long? ObservedGpuBytes,
    bool NetworkIsolationVerified, string[] Limitations)
{
    public long? SampledProcessFamilyWorkingSetPeakBytes { get; init; }
    public double? SampledProcessFamilyCpuMilliseconds { get; init; }
    public long? SampledModelAllocationPeakBytes { get; init; }
    public long? SampledGpuAllocationPeakBytes { get; init; }
    public string ResourceAttribution { get; init; } = "Not sampled";
}
public sealed record AcceptanceDecision(bool Passed, string Reason);

/// <summary>Scoped smoke-test evidence. Passing never grants tools or enables an untested model.</summary>
public static class ModelAcceptance
{
    public const string TextSuite = "grid-text-smoke-v2";
    public static IReadOnlyList<string> RequiredCases { get; } = Array.AsReadOnly(new[]
        { "cold-schema", "warm-schema", "code", "context", "delegation", "untrusted-context" });

    public static string HashManifest(ModelManifest manifest)
    {
        ModelRegistry.ValidateManifest(manifest);
        return Hash(JsonSerializer.Serialize(manifest));
    }
    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public static AcceptanceDecision Evaluate(ModelAcceptanceReport report, AcceptanceConfiguration expected,
        DateTimeOffset now, bool requireNetworkIsolation = false)
    {
        static bool Digest(string? value) => value is { Length: 64 } && value.All(Uri.IsHexDigit);
        if (!Digest(expected.ManifestHash) || !Digest(expected.RuntimeHash) || !Digest(expected.DeviceHash) ||
            expected.Suite != TextSuite || expected.Mode is not ("cpu" or "automatic") ||
            expected.ContextTokens is < 256 or > 131072 || expected.OutputTokens is < 1 or > 8192)
            return new(false, "Unsupported evaluation configuration.");
        if (report.SchemaVersion != 1 || report.ModelId != "qwen3-4b" || report.Configuration != expected)
            return new(false, "Model, runtime, device, mode, suite or token settings changed; reevaluate.");
        if (report.RecordedUtc > now || now - report.RecordedUtc > TimeSpan.FromDays(30))
            return new(false, "Evaluation evidence is future-dated or older than 30 days.");
        if (report.Cases is null || report.Cases.Length != RequiredCases.Count ||
            !report.Cases.Select(c => c?.Id).Order().SequenceEqual(RequiredCases.Order()))
            return new(false, "Required evaluation cases are missing or duplicated.");
        if (report.Cases.Any(c => !c.Passed || !c.Complete || c.ElapsedMilliseconds < 0 || c.ElapsedMilliseconds > (c.Id == "cold-schema" ? TextBenchmarkSuite.Baseline.ColdLimitMilliseconds : TextBenchmarkSuite.Baseline.WarmLimitMilliseconds)))
            return new(false, "A correctness, completion or versioned latency threshold failed.");
        if (!report.StreamCompleted || report.FirstTextMilliseconds is null or < 0 || report.FirstTextMilliseconds > TextBenchmarkSuite.Baseline.FirstTextLimitMilliseconds ||
            !report.Cancelled || !report.Recovered || !report.Unloaded || report.ObservedAllocationBytes is null or <= 0 || report.ObservedAllocationBytes > TextBenchmarkSuite.Baseline.AllocationLimitBytes ||
            report.SampledModelAllocationPeakBytes > TextBenchmarkSuite.Baseline.AllocationLimitBytes ||
            report.ObservedGpuBytes is null or < 0 || expected.Mode == "cpu" && (report.ObservedGpuBytes != 0 || report.SampledGpuAllocationPeakBytes > 0))
            return new(false, "Streaming, cancellation, recovery, allocation or unload acceptance is incomplete.");
        if (requireNetworkIsolation && !report.NetworkIsolationVerified)
            return new(false, "A network-blocked acceptance run has not been recorded.");
        return new(true, "This exact text smoke-test configuration passed. No specialist, image, low-RAM-device or network-isolation certification is implied.");
    }
}
