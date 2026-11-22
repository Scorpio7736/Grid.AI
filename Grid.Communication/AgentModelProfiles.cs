namespace Grid.Communication;

public sealed record AgentModelProfile(string Agent, string Purpose, int ContextTokens = 4096, int InitialTokens = 512, int RetryTokens = 1536)
{
    public string Model => OllamaLocalBackend.Model;
    public string Fallback => "Same bundled weights, CPU-only reduced context; unavailable AI falls back to deterministic GRID. No downloads/cloud.";
}

public static class AgentModelProfiles
{
    public static IReadOnlyList<AgentModelProfile> All { get; } = Array.AsReadOnly(new[] {
        new AgentModelProfile("MCP", Grid.Agent.AgentPersonas.Purpose("MCP"), InitialTokens: 1024),
        new AgentModelProfile("MAXIS", Grid.Agent.AgentPersonas.Purpose("MAXIS")),
        new AgentModelProfile("JN", Grid.Agent.AgentPersonas.Purpose("JN")),
        new AgentModelProfile("TRON", Grid.Agent.AgentPersonas.Purpose("TRON")),
        new AgentModelProfile("VOLT", Grid.Agent.AgentPersonas.Purpose("VOLT")),
        new AgentModelProfile("RELAY", Grid.Agent.AgentPersonas.Purpose("RELAY"), 4096, 512, 1024)
    });
    public static AgentModelProfile Get(string agent) => All.SingleOrDefault(p => p.Agent.Equals(agent, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException("Unknown local agent profile.", nameof(agent));
    public static bool ForceLite { get; set; }
    public static bool LowMemory => ForceLite || Environment.GetEnvironmentVariable("GRID_AI_LOW_MEMORY") == "1" || GC.GetGCMemoryInfo().TotalAvailableMemoryBytes is > 0 and < 8_000_000_000;
    public static bool CpuOnly => LowMemory || Environment.GetEnvironmentVariable("GRID_AI_CPU_ONLY") == "1";
}
