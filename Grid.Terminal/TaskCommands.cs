using Grid.Simulation;
using Grid.Simulation.Tasks;
namespace Grid.Terminal;

public static class TaskCommands
{
    public static void Register(CommandDispatcher commands, GridSimulationEngine engine, TerminalIdentitySession identity, Func<CancellationToken, Task>? save)
    {
        commands.Register(new("mcp", "mcp <request|plans|inspect|brief> ...", "Split explicit requests into specialist proposals and report verified broker results.", args =>
        {
            string Plans(string? reference = null)
            {
                _ = engine.CoordinationBrief(identity.Reference); // Enforce coordinator scope before reading plans.
                var state = engine.TasksFor(identity.Reference);
                var id = reference is null ? (Guid?)null : TaskDisplay.Resolve(reference, state.Plans.Select(p => p.Id), "plan");
                return TaskDisplay.Plans(state, id, reference is null && (args.Count == 0 || args[0] == "plans"));
            }
            try
            {
                if (args.Count == 0 || args.Count == 1 && args[0] is "plans" or "brief") return new(Plans());
                if (args.Count == 2 && args[0] is "inspect" or "brief") return new(Plans(args[1]));
                if (args.Count is 2 or 3 && args[0] == "request")
                {
                    if (save is null) return new("Coordination requires a saved world.");
                    var plan = engine.Coordinate(identity.Reference, args[1], args.Count == 3 ? args[2] : Guid.NewGuid().ToString("N"));
                    save(CancellationToken.None).GetAwaiter().GetResult();
                    return new(Plans(plan.Id.ToString()));
                }
                return new("Usage: mcp request \"MAXIS: demo hello; JN: demo inventory\" [request-key] | mcp plans | mcp inspect <plan-id> | mcp brief [plan-id]. All subtasks require human approval. MAXIS workspace actions and SENTINEL security observations are real.");
            }
            catch (Exception e) when (Expected(e)) { return Error(e); }
        }) { Capability = "session" });
        commands.Register(new("agents", "agents [initialize]", "List system-agent roles or explicitly initialize the six system agents.", args =>
        {
            try
            {
                if (args.Count == 1 && args[0] == "initialize")
                {
                    if (save is null) return new("System-agent initialization requires a saved world.");
                    engine.InitializeSystemAgents(identity.Reference); save(CancellationToken.None).GetAwaiter().GetResult();
                }
                else if (args.Count != 0) return new("Usage: agents [initialize]");
                return new(TerminalTable.Render(["Role", "Capabilities"], engine.TasksFor(identity.Reference).Agents.Select(a => new[] {
                    a.Role.ToString(), TaskCapabilities.For(a.Role) + (a.Role == AgentRole.MAXIS ? ", maxis.delay, inspect, build, test, suggest" : a.Role == AgentRole.SENTINEL ? ", defender, firewall, events" : "") })) + "\n\ndemo/delay are demonstrations. MAXIS workspace actions and SENTINEL read-only security queries are real and require approval.");
            }
            catch (Exception e) when (Expected(e)) { return Error(e); }
        }) { Capability = "session" });
        commands.Register(new("tasks", "tasks <list|inspect|propose|approve|cancel|retry|run> ...", "Durable reviewed tasks. MAXIS workspace actions require explicit human approval.", args =>
        {
            try
            {
                if (args.Count == 0 || args.Count == 1 && args[0] == "list")
                    return new("TASKS\n" + TaskDisplay.Tasks(engine.TasksFor(identity.Reference)) + "\n\nDetails: tasks inspect <task-reference>\nShort references (8+ characters) or full IDs are accepted.");
                if (args.Count == 2 && args[0] == "inspect")
                {
                    var state = engine.TasksFor(identity.Reference);
                    var id = TaskDisplay.Resolve(args[1], state.Records.Select(t => t.Id), "task");
                    return new(TaskDisplay.Inspect(state, state.Records.Single(t => t.Id == id)));
                }
                if (save is null) return new("Tasks require a saved world; no ephemeral execution.");
                if (args.Count is 4 or 5 && args[0] == "propose")
                {
                    var task = engine.ProposeTask(identity.Reference, engine.ResolveProgram(args[1]).Id, args[2], args[3], args.Count == 5 ? args[4] : Guid.NewGuid().ToString("N"));
                    save(CancellationToken.None).GetAwaiter().GetResult();
                    return new(TaskDisplay.Inspect(engine.TasksFor(identity.Reference), task));
                }
                if (args.Count == 2 && args[0] is "approve" or "cancel" or "retry")
                {
                    var id = TaskDisplay.Resolve(args[1], engine.TasksFor(identity.Reference).Records.Select(t => t.Id), "task");
                    switch (args[0])
                    {
                        case "approve": engine.ApproveTask(identity.Reference, id); break;
                        case "cancel": engine.CancelTask(identity.Reference, id); break;
                        case "retry": engine.RetryTask(identity.Reference, id); break;
                        default: return new(Usage);
                    }
                    save(CancellationToken.None).GetAwaiter().GetResult(); return new("Task updated and saved.\n\n" + TaskDisplay.Inspect(engine.TasksFor(identity.Reference), engine.TasksFor(identity.Reference).Records.Single(t => t.Id == id)));
                }
                if (args.Count == 1 && args[0] == "run")
                {
                    if (identity.IsAttachment) return new("Run the broker from the original host terminal. Attached terminals may propose, inspect, approve or cancel within their authority.");
                    if (!identity.IsMaster) return new("Access denied. Human Master Control dispatches approved tasks.");
                    var reference = identity.Reference;
                    return new("", BrokerOperation: async token =>
                    {
                        try { return new(await engine.RunNextTaskAsync(reference, save, token)); }
                        catch (Exception e) when (Expected(e)) { return Error(e); }
                    });
                }
                return new(Usage);
            }
            catch (Exception e) when (Expected(e)) { return Error(e); }
        }) { Capability = "session" });
    }
    private static bool Expected(Exception e) => e is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException or System.Data.Common.DbException;
    private static CommandResult Error(Exception e) => new("Task error: " + e.Message + (e is IOException or System.Data.Common.DbException ? " In-memory changes may remain; resolve persistence before continuing." : ""));
    public const string Usage = "Usage: tasks list | inspect <id> | propose <agent-id|name> <capability> \"argument\" [request-key] | approve <id> | cancel <id> | retry <id> | run. Run dispatches one approved task. MAXIS build/test executes project-defined code.";
}
