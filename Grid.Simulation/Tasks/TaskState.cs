// Compatibility facade: one definition in Grid.Agent, no duplicate state types.
using System.Runtime.CompilerServices;
[assembly: TypeForwardedTo(typeof(Grid.Simulation.Tasks.AgentRole))]
[assembly: TypeForwardedTo(typeof(Grid.Simulation.Tasks.TaskStatus))]
[assembly: TypeForwardedTo(typeof(Grid.Simulation.Tasks.SystemAgent))]
[assembly: TypeForwardedTo(typeof(Grid.Simulation.Tasks.TaskAudit))]
[assembly: TypeForwardedTo(typeof(Grid.Simulation.Tasks.TaskRecord))]
namespace Grid.Simulation.Tasks;

public sealed record TaskState(IReadOnlyList<SystemAgent> Agents, IReadOnlyList<TaskRecord> Records)
{
    public IReadOnlyList<CoordinationPlan> Plans { get; init; } = Array.Empty<CoordinationPlan>();
    public IReadOnlyList<DevelopmentWorkspace> Workspaces { get; init; } = Array.Empty<DevelopmentWorkspace>();
    public StorageState Storage { get; init; } = new();
    public string VoltMode { get; init; } = "normal";
    public RelayState Relay { get; init; } = new();
    public static TaskState Empty => new(Array.Empty<SystemAgent>(), Array.Empty<TaskRecord>());
    public static TaskState Copy(TaskState state) => new(Array.AsReadOnly(state.Agents.ToArray()),
        Array.AsReadOnly(state.Records.Select(t => t with { Audit = Array.AsReadOnly(t.Audit.ToArray()) }).ToArray())) { VoltMode=state.VoltMode, Relay=state.Relay.Copy(), Storage = state.Storage.Copy(), Workspaces = Array.AsReadOnly(state.Workspaces.ToArray()), Plans = Array.AsReadOnly(state.Plans.Select(p => p with { TaskIds = Array.AsReadOnly(p.TaskIds.ToArray()) }).ToArray()) };
    public static TaskState NormalizeAgentBrand(TaskState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var tron = state.Agents.Where(agent => agent.Role == AgentRole.TRON).Select(agent => agent.Id).ToHashSet();
        var legacy = string.Concat("SEN", "TINEL");
        string Text(string? value) => value is null ? "" : System.Text.RegularExpressions.Regex.Replace(value, legacy, "TRON", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        string Capability(TaskRecord task)
        {
            if (!tron.Contains(task.AgentId)) return task.Capability;
            var separator = task.Capability.IndexOf('.');
            if (separator < 0) return task.Capability;
            var suffix = task.Capability[(separator + 1)..];
            return suffix is "demo" or "defender" or "firewall" or "events" ? "tron." + suffix : task.Capability;
        }
        return state with
        {
            Records = Array.AsReadOnly(state.Records.Select(task => task with
            {
                RequestKey = Text(task.RequestKey),
                Capability = Capability(task),
                Result = task.Result is null ? null : Text(task.Result),
                Audit = Array.AsReadOnly(task.Audit.Select(audit => audit with { Detail = Text(audit.Detail) }).ToArray())
            }).ToArray()),
            Plans = Array.AsReadOnly(state.Plans.Select(plan => plan with
            {
                RequestKey = Text(plan.RequestKey),
                Request = Text(plan.Request),
                TaskIds = Array.AsReadOnly(plan.TaskIds.ToArray())
            }).ToArray())
        };
    }
    public static bool Label(string? value, int max) => Grid.Agent.TaskWorkflow.Label(value, max);
    public static void Validate(TaskState state)
    {
        if (state is null) throw new InvalidDataException("Invalid system-agent registry.");
        Grid.Agent.TaskWorkflow.Validate(state.Agents, state.Records, TaskCapabilities.Valid);
        CoordinationPlan.Validate(state);
        DevelopmentPolicy.Validate(state.Workspaces);
        StorageState.Validate(state.Storage,state);
        if(state.VoltMode is not ("normal" or "eco" or "auto")) throw new InvalidDataException("Invalid VOLT pacing mode.");
        RelayState.Validate(state.Relay,state);
    }
}

/// <summary>Role-specific allowlist. Real adapters are dispatched separately through the approved broker.</summary>
public static class TaskCapabilities
{
    public static string For(AgentRole role) => role.ToString().ToLowerInvariant() + ".demo";
    public static bool Valid(AgentRole role, string capability, string argument) => TaskState.Label(argument, 256) &&
        (VoltRelayPolicy.Valid(role,capability,argument) || role == AgentRole.JN && StoragePolicy.Valid(capability,argument) || role == AgentRole.TRON && TronPolicy.Valid(capability,argument) || role == AgentRole.MAXIS && DevelopmentPolicy.IsReal(capability) && DevelopmentPolicy.Parameters(capability,argument) || capability == For(role) || role == AgentRole.MAXIS && capability == "maxis.delay" && int.TryParse(argument, out var delay) && delay is >= 0 and <= 5000);
    internal static async Task<string> ExecuteAsync(AgentRole role, string capability, string argument, CancellationToken token)
    {
        if (!Valid(role, capability, argument)) throw new ArgumentException("Capability or argument is not allowed for this agent.");
        if (capability == "maxis.delay") await Task.Delay(int.Parse(argument), token);
        token.ThrowIfCancellationRequested();
        return $"[DEMONSTRATION ONLY] {role}: {argument}. No host action was performed.";
    }
}
