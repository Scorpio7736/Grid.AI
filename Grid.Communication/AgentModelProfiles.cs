namespace Grid.Communication;

public sealed record AgentModelProfile(string Agent, string Purpose, int ContextTokens = 4096, int InitialTokens = 512, int RetryTokens = 1536)
{
    public string Model => OllamaLocalBackend.Model;
    public string Fallback => "Same bundled weights, CPU-only reduced context; unavailable AI falls back to deterministic GRID. No downloads/cloud.";
}

public static class AgentModelProfiles
{
    public static IReadOnlyList<AgentModelProfile> All { get; } = Array.AsReadOnly(new[] {
        new AgentModelProfile("MCP", "Coordinate complete, explicit plans; separate proposals, approvals and recorded results."),
        new AgentModelProfile("MAXIS", "Software engineering: correct code, debugging and testable changes; never claim unrun tests passed."),
        new AgentModelProfile("JN", "Document and storage reasoning: preserve provenance; distinguish duplicate candidates from verified duplicates."),
        new AgentModelProfile("SENTINEL", "Security analysis: explain evidence and uncertainty; do not invent threats or run remediation."),
        new AgentModelProfile("VOLT", "Resource reasoning: distinguish capacity, utilization and headroom; unknown measurements stay unknown. Used percent = 100 minus available/headroom percent; free percent is not used percent."),
        new AgentModelProfile("RELAY", "Communication: concise faithful summaries; distinguish drafted messages from delivered messages.", 4096, 512, 1024)
    });
    public static AgentModelProfile Get(string agent) => All.SingleOrDefault(p => p.Agent.Equals(agent, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException("Unknown local agent profile.", nameof(agent));
    public static bool ForceLite { get; set; }
    public static bool LowMemory => ForceLite || Environment.GetEnvironmentVariable("GRID_AI_LOW_MEMORY") == "1" || GC.GetGCMemoryInfo().TotalAvailableMemoryBytes is > 0 and < 8_000_000_000;
    public static bool CpuOnly => LowMemory || Environment.GetEnvironmentVariable("GRID_AI_CPU_ONLY") == "1";
}
