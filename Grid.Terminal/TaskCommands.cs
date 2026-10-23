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
                return new("Usage: mcp request \"MAXIS: demo hello; JN: demo inventory\" [request-key] | mcp plans | mcp inspect <plan-id> | mcp brief [plan-id]. All subtasks require human approval. Specialist capabilities perform real work; speech requires explicit approval.");
            }
            catch (Exception e) when (Expected(e)) { return Error(e); }
        }) { Capability = "session" });
        commands.Register(new("agents", "agents [initialize]", "List saved system-agent roles. Use grid -start to initialize/resume agents.", args =>
        {
            try
            {
                if (args.Count == 1 && args[0] == "initialize")
                {
                    return new("Agent initialization is now part of grid -start. Use grid -start as Master Control; agents only lists registered roles.");
                }
                else if (args.Count != 0) return new("Usage: agents [initialize]");
                return new((engine.IsRunning ? "GRID AGENTS: active" : "GRID AGENTS: paused; saved identities retained") + "\n" + TerminalTable.Render(["Role", "Capabilities"], engine.TasksFor(identity.Reference).Agents.Select(a => new[] {
                    a.Role.ToString(), TaskCapabilities.For(a.Role) + (a.Role == AgentRole.MAXIS ? ", maxis.delay, inspect, build, test, suggest" : a.Role == AgentRole.SENTINEL ? ", defender, firewall, events" : a.Role == AgentRole.JN ? ", inventory, duplicates, cleanup, storage, quota" : a.Role == AgentRole.VOLT ? ", status, throttle" : a.Role == AgentRole.RELAY ? ", notify, brief, speak" : "") })) + "\n\ndemo/delay are demonstrations. Specialist capabilities are real and require approval; relay.speak produces audio.");
            }
            catch (Exception e) when (Expected(e)) { return Error(e); }
        }) { Capability = "session" });
        commands.Register(new("tasks", "tasks [-completed|-list] [-AGENT] | tasks <list|inspect|propose|approve|deny|cancel|retry|run|clear> ...", "Durable reviewed tasks. MAXIS workspace actions require explicit human approval.", args =>
        {
            try
            {
                if (args.Count == 0 || args[0].StartsWith('-') || args[0].Equals("list", StringComparison.OrdinalIgnoreCase))
                    return ListTasks(engine.TasksFor(identity.Reference), args.Count > 0 && args[0].Equals("list", StringComparison.OrdinalIgnoreCase) ? args.Skip(1).ToArray() : args);
                if (args[0].Equals("clear", StringComparison.OrdinalIgnoreCase) || args.Count > 1 && args[1].StartsWith('-') && args[0].ToLowerInvariant() is "approve" or "run")
                    return Batch(engine, identity, save, args);
                if (args.Count == 2 && args[0] == "inspect")
                {
                    var state = engine.TasksFor(identity.Reference);
                    var id = TaskDisplay.Resolve(args[1], state.Records.Select(t => t.Id), "task");
                    return new(TaskDisplay.Inspect(state, state.Records.Single(t => t.Id == id)));
                }
                if (save is null) return new("Tasks require a saved world; no ephemeral execution.");
                if (args.Count == 3 && args[0].Equals("deny", StringComparison.OrdinalIgnoreCase))
                {
                    var id = TaskDisplay.Resolve(args[1], engine.TasksFor(identity.Reference).Records.Select(t => t.Id), "task");
                    engine.DenyTask(identity.Reference, id, args[2]);
                    save(CancellationToken.None).GetAwaiter().GetResult();
                    return new("Task denied and saved.\n\n" + TaskDisplay.Inspect(engine.TasksFor(identity.Reference), engine.TasksFor(identity.Reference).Records.Single(t => t.Id == id)));
                }
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
    private static Guid? AgentFilter(TaskState state, IReadOnlyList<string> flags)
    {
        if (flags.Count == 0) return null;
        if (flags.Count != 1 || !flags[0].StartsWith('-')) throw new ArgumentException("Use one agent switch, for example -MAXIS.");
        return state.Agents.SingleOrDefault(a => a.Role.ToString().Equals(flags[0][1..], StringComparison.OrdinalIgnoreCase))?.Id
            ?? throw new ArgumentException("Unknown or uninitialized agent switch. Use agents.");
    }
    private static CommandResult ListTasks(TaskState state, IReadOnlyList<string> flags)
    {
        var view = flags.Where(f => f.Equals("-completed", StringComparison.OrdinalIgnoreCase) || f.Equals("-list", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (view.Length > 1) throw new ArgumentException("Choose one task view: -completed or -list.");
        var archiveFlags = flags.Count(f => f.Equals("-archived", StringComparison.OrdinalIgnoreCase));
        if (archiveFlags > 1) throw new ArgumentException("Use -archived only once.");
        var agent = AgentFilter(state, flags.Where(f => !view.Contains(f) && !f.Equals("-archived", StringComparison.OrdinalIgnoreCase)).ToArray());
        var rows = state.Records.Where(t => (archiveFlags == 0 || t.Archived) && (agent is null || t.AgentId == agent));
        if (view.Length == 1) rows = view[0].Equals("-completed", StringComparison.OrdinalIgnoreCase)
            ? rows.Where(t => t.Status == Grid.Simulation.Tasks.TaskStatus.Completed)
            : rows.Where(t => t.Status is not (Grid.Simulation.Tasks.TaskStatus.Completed or Grid.Simulation.Tasks.TaskStatus.Denied));
        return new((archiveFlags == 1 ? "ARCHIVED TASKS" : "TASKS") + "\n" + TaskDisplay.Tasks(state, rows) +
            "\n\nDetails: tasks inspect <task-reference> | Views: tasks -completed | tasks -list | tasks list -archived [-AGENT]");
    }

    private static CommandResult Batch(GridSimulationEngine engine, TerminalIdentitySession identity, Func<CancellationToken, Task>? save, IReadOnlyList<string> args)
    {
        if (save is null) return new("Tasks require a saved world; no ephemeral execution.");
        if (!identity.IsMaster) return new("Access denied. Human Master Control owns bulk task operations.");
        if (identity.IsAttachment) return new("Use the original host terminal for bulk task approval, clearing and dispatch.");
        var action = args[0].ToLowerInvariant();
        if (args.Skip(1).Count(f => f.Equals("-all", StringComparison.OrdinalIgnoreCase)) != 1)
            throw new ArgumentException("Use tasks " + action + " -all [-AGENT].");
        var state = engine.TasksFor(identity.Reference);
        var agent = AgentFilter(state, args.Skip(1).Where(f => !f.Equals("-all", StringComparison.OrdinalIgnoreCase)).ToArray());
        var selected = state.Records.Where(t => !t.Archived && (agent is null || t.AgentId == agent) &&
            (action == "approve" ? t.Status == Grid.Simulation.Tasks.TaskStatus.AwaitingApproval : action != "run" || t.Status == Grid.Simulation.Tasks.TaskStatus.Queued)).ToArray();
        if (selected.Length == 0) return new("No eligible tasks in this batch.");
        if (action == "clear" && selected.Any(t => t.Status == Grid.Simulation.Tasks.TaskStatus.Running))
            return new("Cannot clear a batch containing running tasks. Cancel and wait for completion, then preview again.");
        var reference = identity.Reference;
        var used = 0;
        if (action == "run") return new("", BrokerOperation: async token =>
        {
            if (Interlocked.Exchange(ref used, 1) != 0) return new("Batch already consumed; no duplicate dispatch.");
            var rows = new List<string[]>();
            var completed = 0; var unsuccessful = 0; var skipped = 0;
            var halted = false;
            foreach (var task in selected)
            {
                var taskRef = TaskDisplay.Ref(task.Id, state.Records.Select(t => t.Id));
                if (halted || token.IsCancellationRequested) { rows.Add([taskRef, "Skipped", "Batch stopped before dispatch"]); skipped++; continue; }
                try
                {
                    var outcome = await engine.RunNextTaskAsync(reference, save, token, task);
                    var status = outcome.Split(':')[0];
                    rows.Add([taskRef, status, outcome]);
                    if (status == "Completed") completed++;
                    else { unsuccessful++; halted = true; }
                }
                catch (Exception e) when (Expected(e) || e is OperationCanceledException)
                {
                    rows.Add([taskRef, "Stopped", "Outcome not confirmed: " + e.Message]);
                    unsuccessful++; halted = true;
                }
            }
            return new("BATCH RESULTS\n" + TerminalTable.Render(["Task", "Outcome", "Detail"], rows) +
                $"\nSelected: {selected.Length} | Completed: {completed} | Failed/interrupted/unconfirmed: {unsuccessful} | Skipped: {skipped}\nUse tasks inspect <reference> for full retained results. Skipped tasks remain queued; no automatic resume.");
        });
        var preview = $"BULK {action.ToUpperInvariant()} | {selected.Length} tasks | " + (agent is null ? "all agents" : state.Agents.Single(a => a.Id == agent).Role.ToString()) +
            (action == "clear" ? "\nArchive selected records; cancel pending work; retain audit, plan and RELAY links. Running work is refused. This does not free the 256-record history capacity.\n" : "\nApprove exactly these immutable requests for one attempt each. Approval does not run them.\n") +
            string.Join("\n\n", selected.Select(t => TaskDisplay.Inspect(state, t)));
        return new(preview, ConfirmTaskBatch: () =>
        {
            if (Interlocked.Exchange(ref used, 1) != 0) return new("Batch confirmation already consumed. Nothing changed.");
            try
            {
                engine.UpdateTaskBatch(reference, selected, action == "clear");
                save(CancellationToken.None).GetAwaiter().GetResult();
                return new($"{selected.Length} tasks {(action == "clear" ? "archived" : "approved")} and saved. Newly added tasks were not included.");
            }
            catch (Exception e) when (Expected(e)) { return Error(e); }
        });
    }
    private static bool Expected(Exception e) => e is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException or System.Data.Common.DbException;
    private static CommandResult Error(Exception e) => new("Task error: " + e.Message + (e is IOException or System.Data.Common.DbException ? " In-memory changes may remain; resolve persistence before continuing." : ""));
    public const string Usage = "Usage: tasks [-completed|-list] [-AGENT] | tasks list [-AGENT] [-archived] | deny <id> \"reason\" | approve -all [-AGENT] | run -all [-AGENT] | clear -all [-AGENT] | inspect <id> | propose <agent-id|name> <capability> \"argument\" [request-key] | approve <id> | cancel <id> | retry <id> | run. Run without -all dispatches one approved task; bulk run stops at the first unsuccessful attempt. MAXIS build/test executes project-defined code.";
}
