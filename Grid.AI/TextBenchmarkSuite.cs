using System.Text.Json;

namespace Grid.AI;

public sealed record TextBenchmarkCase(string Id, string Prompt, string Expected);
public sealed record TextBenchmarkSuite(string Version, long ColdLimitMilliseconds, long WarmLimitMilliseconds,
    long FirstTextLimitMilliseconds, long AllocationLimitBytes, IReadOnlyList<TextBenchmarkCase> Cases)
{
    public static TextBenchmarkSuite Baseline { get; } = Load();
    private static TextBenchmarkSuite Load()
    {
        using var stream = typeof(TextBenchmarkSuite).Assembly.GetManifestResourceStream("Grid.AI.Benchmarks.grid-text-smoke-v2.json")
            ?? throw new InvalidOperationException("Bundled benchmark dataset missing.");
        var suite = JsonSerializer.Deserialize<TextBenchmarkSuite>(stream) ?? throw new InvalidDataException("Invalid benchmark dataset.");
        if (suite.Version != ModelAcceptance.TextSuite || suite.Cases is null || suite.Cases.Count != 6 ||
            suite.Cases.Select(c => c.Id).Distinct().Count() != 6 || suite.ColdLimitMilliseconds <= 0 || suite.WarmLimitMilliseconds <= 0 ||
            suite.FirstTextLimitMilliseconds <= 0 || suite.AllocationLimitBytes <= 0) throw new InvalidDataException("Invalid benchmark definition.");
        return suite with { Cases = Array.AsReadOnly(suite.Cases.ToArray()) };
    }
}
