using Grid.Core;
using Grid.Simulation.Editing;
using Grid.Simulation.Tasks;
using Status = Grid.Simulation.Tasks.TaskStatus;
namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    private TaskState tasks = TaskState.Empty;
    private bool taskExecuting;
    private IProgramEditLease TaskLease(EditSessionReference session, bool master = false)
    {
        var lease = editAuthorizer.AcquireTask(session, world.InstanceId);
        if (lease is null) throw DeniedTask(null, "No authenticated task session.");
        var a = lease.Identity;
        if (a.PolicyVersion != 1 || a.WorldInstanceId != world.InstanceId || a.SessionId != session.SessionId || a.Generation != session.Generation || a.OperatorId != LocalOperator.Id ||
            a.IsMasterControl && a.AgentId is not null || master && !a.IsMasterControl || !a.IsMasterControl && (a.AgentId is null || !identities.RegisteredAgents.Contains(a.AgentId.Value)))
        { var denied = DeniedTask(a, "Task role, session or policy is not authorized."); lease.Dispose(); throw denied; }
        return lease;
    }
    private UnauthorizedAccessException DeniedTask(ProgramEditIdentity? actor, string detail)
    {
        lock (gate) RecordEvent("TaskDenied", actor?.AgentId, null, detail,
            System.Text.Json.JsonSerializer.Serialize(new { operatorId = actor?.OperatorId, activeAgentId = actor?.AgentId, mode = "manual", allowed = false }));
        return new UnauthorizedAccessException(detail);
    }
    private TaskRecord FindTask(Guid id) => tasks.Records.SingleOrDefault(t => t.Id == id) ?? throw new ArgumentException("Unknown task ID.");
    private bool Visible(TaskRecord task, ProgramEditIdentity actor) => actor.IsMasterControl || task.AgentId == actor.AgentId ||
        tasks.Plans.Any(p => p.CoordinatorId == actor.AgentId && p.TaskIds.Contains(task.Id));
    private void ChangeTask(TaskRecord task, string action, ProgramEditIdentity? actor, string detail)
    {
        // Reserve lifecycle slots so a saved active task can always record cancellation/recovery/completion.
        if (task.Audit.Count >= 60 && action is not ("Finished" or "Cancelled" or "Recovered"))
            throw new InvalidOperationException("Task audit capacity reached; further approval/retry is refused without deleting history.");
        var audit = new TaskAudit(timeProvider.GetUtcNow().ToUniversalTime(), action, actor?.OperatorId, actor?.AgentId, detail);
        task = task with { Audit = Array.AsReadOnly(task.Audit.Append(audit).ToArray()) };
        var next = tasks with { Records = Array.AsReadOnly(tasks.Records.Where(t => t.Id != task.Id).Append(task).ToArray()) };
        TaskState.Validate(next); tasks = next;
        RecordEvent("Task" + action, task.Id, null, detail);
    }
    public TaskState TasksFor(EditSessionReference session)
    {
        using var lease = TaskLease(session);
        lock (gate) return TaskState.Copy(tasks with { Records = Array.AsReadOnly(tasks.Records.Where(t => Visible(t, lease.Identity)).ToArray()),
            Workspaces = Array.AsReadOnly(tasks.Workspaces.Where(w => lease.Identity.IsMasterControl || tasks.Agents.Any(a => a.Id == lease.Identity.AgentId && a.Role is AgentRole.MCP or AgentRole.MAXIS)).ToArray()),
            Relay = RoleView(lease.Identity,AgentRole.RELAY) ? tasks.Relay.Copy() : new RelayState(),
            Storage = StorageVisible(lease.Identity) ? tasks.Storage.Copy() : new StorageState(),
            Plans = Array.AsReadOnly(tasks.Plans.Where(p => lease.Identity.IsMasterControl || p.CoordinatorId == lease.Identity.AgentId).ToArray()) });
    }
    public void StartAgents(EditSessionReference session)
    {
        using var lease = TaskLease(session, true);
        lock (gate)
        {
            var wasRunning = clock.IsRunning;
            Start();
            try { InitializeSystemAgents(session); }
            catch { if (!wasRunning) Pause(); throw; }
        }
    }
    public void InitializeSystemAgents(EditSessionReference session)
    {
        using var lease = TaskLease(session, true);
        lock (gate)
        {
            RequireAgentActivity();
            if (tasks.Agents.Count != 0) return;
            // Validate all names before making any residents; never promote a same-named civilian.
            foreach (var role in Enum.GetValues<AgentRole>())
            {
                var matches = world.Programs.Values.Where(p => p.Name.Equals(role.ToString(), StringComparison.OrdinalIgnoreCase)).ToArray();
                if (matches.Length > 1 || matches.Any(p => p.Kind != ResidentKind.Agent)) throw new ArgumentException("System-agent name collision: " + role);
                if (matches.Length == 0 && role != AgentRole.MCP && world.Programs.ContainsKey(DialogueId("system-agent-" + role))) throw new ArgumentException("System-agent ID collision: " + role);
            }
            var coordinator = EnsureCoordinator();
            var entries = new List<SystemAgent>();
            foreach (var role in Enum.GetValues<AgentRole>())
            {
                var p = world.Programs.Values.SingleOrDefault(p => p.Name.Equals(role.ToString(), StringComparison.OrdinalIgnoreCase));
                if (p is null)
                {
                    p = new GridProgram { Id = DialogueId("system-agent-" + role), Name = role.ToString(), Kind = ResidentKind.Agent, Occupation = "System agent (approval-gated capabilities)" };
                    world.AddProgram(p, world.GetProgram(coordinator.Id).CurrentDistrictId);
                }
                RegisterAgentIdentity(p.Id); entries.Add(new(p.Id, role));
            }
            tasks = tasks with { Agents = Array.AsReadOnly(entries.ToArray()) };
            RecordEvent("SystemAgentsInitialized", LocalOperator.Id, null, "Six system-agent roles registered; MAXIS real actions require workspace registration and task approval.");
        }
    }
    public TaskRecord ProposeTask(EditSessionReference session, Guid agentId, string capability, string argument, string requestKey)
    {
        using var lease = TaskLease(session);
        lock (gate)
        {
            var actor = lease.Identity;
            if (!actor.IsMasterControl) RequireAgentActivity();
            if (!actor.IsMasterControl && actor.AgentId != agentId) throw DeniedTask(actor, "Agent sessions may propose only their own capabilities.");
            var agent = tasks.Agents.SingleOrDefault(a => a.Id == agentId) ?? throw new ArgumentException("Initialize system agents first.");
            if (!TaskCapabilities.Valid(agent.Role, capability, argument) || !TaskState.Label(requestKey, 80)) throw new ArgumentException("Unknown capability or invalid parameters/request key.");
            if (DevelopmentPolicy.IsReal(capability)) _ = DevelopmentTarget(capability,argument);
            if (StoragePolicy.IsReal(capability)) _ = StorageTarget(capability,argument);
            var existing = tasks.Records.SingleOrDefault(t => t.RequestKey == requestKey);
            if (existing is not null)
            {
                if (!Visible(existing, actor) || existing.AgentId != agentId || existing.Capability != capability || existing.Argument != argument || existing.OperatorId != actor.OperatorId || existing.SubmittedAsAgent != actor.AgentId)
                    throw new ArgumentException("Request key is already used by a different proposal.");
                return existing;
            }
            if (tasks.Records.Count >= 256) throw new InvalidOperationException("Task capacity reached; records were not truncated.");
            var task = new TaskRecord(Guid.NewGuid(), requestKey, agentId, capability, argument, actor.OperatorId, actor.AgentId, Status.AwaitingApproval, 0, null, null, []);
            ChangeTask(task, "Proposed", actor, "Exact target, capability and parameters await human approval."); return FindTask(task.Id);
        }
    }
    public void ApproveTask(EditSessionReference session, Guid id)
    {
        using var lease = TaskLease(session, true);
        lock (gate)
        {
            var task = FindTask(id); if (task.Archived || task.Status != Status.AwaitingApproval) throw new InvalidOperationException("Only awaiting-approval tasks can be approved.");
            if (DevelopmentPolicy.IsReal(task.Capability)) _ = DevelopmentTarget(task.Capability,task.Argument);
            if (StoragePolicy.IsReal(task.Capability)) _ = StorageTarget(task.Capability,task.Argument);
            ChangeTask(task with { Status = Status.Queued, ApprovedBy = lease.Identity.OperatorId }, "Approved", lease.Identity, "Human approved this immutable request for one attempt.");
        }
    }
    public void DenyTask(EditSessionReference session, Guid id, string reason)
    {
        using var lease = TaskLease(session, true);
        lock (gate)
        {
            if (!TaskState.Label(reason, 512)) throw new ArgumentException("A denial reason of 1–512 printable characters is required.");
            var task = FindTask(id);
            if (task.Archived || task.Status != Status.AwaitingApproval) throw new InvalidOperationException("Only awaiting-approval tasks can be denied.");
            ChangeTask(task with { Status = Status.Denied, ApprovedBy = null, Result = reason }, "Denied", lease.Identity, reason);
        }
    }
    public void CancelTask(EditSessionReference session, Guid id)
    {
        using var lease = TaskLease(session);
        lock (gate)
        {
            var task = FindTask(id); if (task.Archived) throw new InvalidOperationException("Archived tasks cannot be changed."); if (!Visible(task, lease.Identity)) throw DeniedTask(lease.Identity, "Task outside this agent's scope.");
            if (task.Status is not (Status.AwaitingApproval or Status.Queued or Status.Running)) throw new InvalidOperationException("Task is already terminal.");
            ChangeTask(task with { Status = Status.Cancelled, Result = "Cancelled by authorized operator." }, "Cancelled", lease.Identity, "Cancellation requested; no further attempt is authorized.");
        }
    }
    public void RetryTask(EditSessionReference session, Guid id)
    {
        using var lease = TaskLease(session);
        lock (gate)
        {
            var task = FindTask(id); if (task.Archived) throw new InvalidOperationException("Archived tasks cannot be changed."); if (!Visible(task, lease.Identity)) throw DeniedTask(lease.Identity, "Task outside this agent's scope.");
            if (task.Status is not (Status.Failed or Status.TimedOut or Status.Interrupted) || task.Attempts >= 3) throw new InvalidOperationException("Retry requires a failed/interrupted task below the three-attempt limit.");
            ChangeTask(task with { Status = Status.AwaitingApproval, ApprovedBy = null, Result = null }, "RetryRequested", lease.Identity, "Fresh human approval required; prior external side effects may remain.");
        }
    }
    public async Task<string> RunNextTaskAsync(EditSessionReference session, Func<CancellationToken, Task> save, CancellationToken token, TaskRecord? expected = null)
    {
        ArgumentNullException.ThrowIfNull(save);
        TaskRecord task; AgentRole role; ProgramEditIdentity actor; long activity;
        using (var lease = TaskLease(session, true))
        lock (gate)
        {
            RequireAgentActivity(); activity = runVersion;
            if (taskExecuting) throw new InvalidOperationException("The broker is already running a task.");
            task = tasks.Records.FirstOrDefault(t => !t.Archived && t.Status == Status.Queued && (expected == null || t.Id == expected.Id)) ?? throw new InvalidOperationException("No approved queued tasks.");
            if (expected is not null && !SameTask(task, expected)) throw new InvalidOperationException("Selected task changed; review a new batch.");
            var agent = tasks.Agents.Single(a => a.Id == task.AgentId); role = agent.Role; actor = lease.Identity;
            if (!identities.RegisteredAgents.Contains(agent.Id) || world.GetProgram(agent.Id).Kind != ResidentKind.Agent || !TaskCapabilities.Valid(role, task.Capability, task.Argument)) throw DeniedTask(actor, "Task target/capability no longer authorized.");
            token.ThrowIfCancellationRequested();
            ChangeTask(task with { Status = Status.Running, Attempts = task.Attempts + 1 }, "Started", actor, "Broker claimed an attempt; durable start is required before invocation.");
            taskExecuting = true;
        }
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token); stop.CancelAfter(TimeSpan.FromSeconds(DevelopmentPolicy.IsReal(task.Capability) ? 120 : SentinelPolicy.IsReal(task.Capability) ? 30 : StoragePolicy.IsReal(task.Capability) ? 120 : VoltRelayPolicy.IsReal(task.Capability) ? 30 : 1));
        Status outcome = Status.Failed; string result;
        try
        {
            try
            {
                await save(token); // Persistence fence: never invoke an adapter if claiming the task was not saved.
                using (var lease = TaskLease(session, true))
                lock (gate) if (!clock.IsRunning || runVersion != activity || FindTask(task.Id).Status != Status.Running) throw new OperationCanceledException();
                Task<string> execution;
                if (DevelopmentPolicy.IsReal(task.Capability))
                {
                    DevelopmentWorkspace workspace; lock(gate) workspace = DevelopmentTarget(task.Capability,task.Argument);
                    execution = maxisRuntime!.ExecuteAsync(workspace,task,stop.Token);
                }
                else if (task.Capability is "volt.status" or "volt.throttle") execution = Task.FromResult(ExecuteVolt(session,task));
                else if (task.Capability is "relay.notify" or "relay.brief" or "relay.speak") execution = ExecuteRelayAsync(session,task,save,stop.Token);
                else if (StoragePolicy.IsReal(task.Capability)) execution = ExecuteStorageAsync(session,task,stop.Token);
                else if (SentinelPolicy.IsReal(task.Capability)) execution = ReadSecurityAsync(task.Capability,stop.Token);
                else execution = TaskCapabilities.ExecuteAsync(role, task.Capability, task.Argument, stop.Token);
                try
                {
                    while (!execution.IsCompleted)
                    {
                        await Task.WhenAny(execution, Task.Delay(25, stop.Token));
                        if (stop.IsCancellationRequested) break;
                        using var lease = TaskLease(session, true);
                        lock (gate) if (!clock.IsRunning || runVersion != activity || FindTask(task.Id).Status != Status.Running || DevelopmentPolicy.IsReal(task.Capability) && !tasks.Workspaces.Any(w => w.Enabled && w.Name.Equals(task.Argument.Split('|')[0], StringComparison.OrdinalIgnoreCase)) || task.Capability is "jn.inventory" or "jn.duplicates" or "jn.cleanup" && !tasks.Storage.Scopes.Any(s => s.Enabled && s.Name.Equals(task.Argument,StringComparison.OrdinalIgnoreCase))) { stop.Cancel(); break; }
                    }
                    result = await execution; outcome = Status.Completed;
                    using var finalLease = TaskLease(session, true);
                }
                finally { stop.Cancel(); try { await execution; } catch (OperationCanceledException) { } }
            }
            catch (OperationCanceledException) { outcome = token.IsCancellationRequested ? Status.Cancelled : Status.TimedOut; result = "Attempt cancelled or exceeded its deadline (demo: 1 second; MAXIS/JN: 120 seconds; SENTINEL/VOLT/RELAY: 30 seconds)."; }
            catch (UnauthorizedAccessException) { outcome = Status.Interrupted; result = "Session authorization changed; fresh approval is required for retry."; }
            catch (Exception e) when (e is IOException or InvalidDataException or System.Data.Common.DbException or InvalidOperationException or ArgumentException or System.ComponentModel.Win32Exception) { result = "Attempt failed: " + e.GetType().Name + ". " + new string(e.Message.Where(c => !char.IsControl(c)).Take(350).ToArray()); }
            if (DevelopmentPolicy.IsReal(task.Capability) && outcome != Status.Completed && maxisRuntime is not null)
                result += " Attempt artifacts (if created): " + maxisRuntime.AttemptDirectory(task);
            lock (gate)
            {
                var current = FindTask(task.Id);
                if (!clock.IsRunning || runVersion != activity)
                {
                    outcome = Status.Interrupted;
                    result = "GRID paused or stopped during this attempt. External effects may remain; inspect results before retrying with fresh approval.";
                }
                if (current.Status == Status.Running) ChangeTask(current with { Status = outcome, Result = result }, "Finished", actor, result.Length > 500 ? result[..500] : result);
                result = FindTask(task.Id).Status + ": " + FindTask(task.Id).Result;
            }
            await save(CancellationToken.None);
            return result;
        }
        finally { lock (gate) taskExecuting = false; }
    }
    private void RestoreTasks(TaskState state)
    {
        tasks = TaskState.Copy(state);
        foreach(var d in tasks.Relay.Deliveries.Where(d=>d.Status==RelayDeliveryStatus.Started).ToArray())
            DeliveryFinished(d.Id,RelayDeliveryStatus.Interrupted,"Restart interrupted delivery; outcome unknown, no automatic replay.");
        foreach (var task in tasks.Records.ToArray())
            if (task.Status is Status.Running or Status.Queued)
                ChangeTask(task with { Status = task.Status == Status.Running ? Status.Interrupted : Status.AwaitingApproval, ApprovedBy = null,
                    Result = task.Status == Status.Running ? "Interrupted by restart; external outcome is not assumed." : null }, "Recovered", null, "Restart invalidates dispatch approval; no automatic replay.");
    }
}
