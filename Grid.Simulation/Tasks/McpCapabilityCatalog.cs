namespace Grid.Simulation.Tasks;

public sealed record McpCapability(AgentRole Agent, string Capability, string Argument, string Purpose);

/// <summary>Planning metadata, never an executable grant. TaskCapabilities remains the execution allowlist.</summary>
public static class McpCapabilityCatalog
{
    public const int Version = 1;
    public static bool ReadOnlyRetry(string capability) => capability is "volt.status" or "tron.defender" or "tron.firewall" or
        "tron.events" or "maxis.inspect" or "jn.inventory" or "jn.duplicates" or "jn.storage";
    public static string Effect(string capability) => ReadOnlyRetry(capability) ? "Read-only observation; explicit retry can be reviewed" :
        capability.EndsWith(".demo", StringComparison.Ordinal) ? "Demonstration only" : "May write outputs or change state; conversational retry is not automatic";
    public static IReadOnlyList<McpCapability> Entries { get; } = Array.AsReadOnly(new[] {
        new McpCapability(AgentRole.VOLT, "volt.status", "current", "Read host resource observations"),
        new McpCapability(AgentRole.VOLT, "volt.throttle", "normal/eco/auto", "Adjust GRID pacing only"),
        new McpCapability(AgentRole.TRON, "tron.defender", "status", "Read Defender status"),
        new McpCapability(AgentRole.TRON, "tron.firewall", "status", "Read firewall status"),
        new McpCapability(AgentRole.TRON, "tron.events", "recent", "Read security event metadata"),
        new McpCapability(AgentRole.MAXIS, "maxis.build", "workspace", "Build registered project"),
        new McpCapability(AgentRole.MAXIS, "maxis.test", "workspace", "Test registered project"),
        new McpCapability(AgentRole.MAXIS, "maxis.inspect", "workspace|relative-file", "Inspect source"),
        new McpCapability(AgentRole.MAXIS, "maxis.suggest", "workspace|relative-file|old-text|new-text", "Stage replacement suggestion"),
        new McpCapability(AgentRole.JN, "jn.inventory", "storage-scope", "Inventory registered storage"),
        new McpCapability(AgentRole.JN, "jn.duplicates", "storage-scope", "Find possible duplicates"),
        new McpCapability(AgentRole.JN, "jn.cleanup", "storage-scope", "Propose cleanup without deletion"),
        new McpCapability(AgentRole.JN, "jn.storage", "status", "Measure managed storage"),
        new McpCapability(AgentRole.RELAY, "relay.notify", "message", "Record a local notification")
    });
    public static string Prompt => string.Join("; ", Entries.Select(e => $"{e.Capability} => {e.Argument}"));
    public static string Reason(string capability) => Entries.SingleOrDefault(e => e.Capability == capability)?.Purpose ?? "Explicit registered specialist capability";
    public const string Authority = "MCP = Master Control Program, your digital helper.\n" +
        "Inspect/propose/coordinate: registered capabilities and explicitly granted context only.\n" +
        "Execute: original-host human requests mcp run <plan>; every immutable child must already have human approval. One attempt per task; stop on failure/revocation.\n" +
        "Files: separate reviewed MAXIS file previews and exact-hash approval/apply workflow.\n" +
        "Reserved human controls: task/edit approval, credentials, identity and agent grants, context grants, NGE, copy/transfer, quota approval and GRID start/pause.\n" +
        "MCP cannot self-approve, impersonate Master Control, expand capabilities or elevate Windows permissions.\n" +
        "Revoke: mcp cancel <plan>, tasks cancel <task>, revoke workspace/storage grants, pause or log out. No automatic retry or replay.";
}
