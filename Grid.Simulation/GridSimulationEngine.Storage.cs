using Grid.Simulation.Editing;
using Grid.Simulation.Tasks;
namespace Grid.Simulation;
public sealed partial class GridSimulationEngine
{
    private JnRuntime? jnRuntime;
    public void ConfigureJnRuntime(string databasePath,string contentRoot) { lock(gate) { if(jnRuntime is not null) throw new InvalidOperationException("JN runtime already configured."); jnRuntime=new(databasePath,contentRoot); } }
    private bool StorageVisible(ProgramEditIdentity actor) => actor.IsMasterControl || tasks.Agents.Any(a=>a.Id==actor.AgentId && a.Role is AgentRole.MCP or AgentRole.JN);
    public StorageState StorageFor(EditSessionReference session)
    { using var lease=TaskLease(session); lock(gate) { if(!StorageVisible(lease.Identity)) throw DeniedTask(lease.Identity,"Storage governance requires Master Control, MCP or JN."); return tasks.Storage.Copy(); } }
    public void RegisterStorageScope(EditSessionReference session,string name,string path)
    {
        using var lease=TaskLease(session,true); lock(gate)
        {
            if(!DevelopmentPolicy.Name(name) || tasks.Storage.Scopes.Count>=32 || tasks.Storage.Scopes.Any(s=>s.Name.Equals(name,StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("Use a new immutable scope alias; registry limit is 32.");
            var scope=new StorageScope(name,DevelopmentPolicy.CheckedPath(path),Environment.MachineName,true);
            (jnRuntime ?? throw new InvalidOperationException("Saved JN runtime required.")).CheckScope(scope);
            var next=tasks.Storage with { Scopes=Array.AsReadOnly(tasks.Storage.Scopes.Append(scope).ToArray()) }; StorageState.Validate(next,tasks); tasks=tasks with { Storage=next };
            RecordEvent("StorageScopeRegistered",lease.Identity.OperatorId,null,"JN scope registered: "+name);
        }
    }
    public void RevokeStorageScope(EditSessionReference session,string name)
    {
        using var lease=TaskLease(session,true); lock(gate)
        {
            if(!tasks.Storage.Scopes.Any(s=>s.Name.Equals(name,StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("Unknown storage alias.");
            tasks=tasks with { Storage=tasks.Storage with { Scopes=Array.AsReadOnly(tasks.Storage.Scopes.Select(s=>s.Name.Equals(name,StringComparison.OrdinalIgnoreCase) ? s with { Enabled=false } : s).ToArray()) } };
            RecordEvent("StorageScopeRevoked",lease.Identity.OperatorId,null,"JN scope revoked: "+name);
        }
    }
    private StorageScope? StorageTarget(string capability,string argument)
    {
        if(jnRuntime is null) throw new InvalidOperationException("Saved JN runtime required.");
        if(capability is "jn.storage" or "jn.quota") return null;
        var scope=tasks.Storage.Scopes.SingleOrDefault(s=>s.Name.Equals(argument,StringComparison.OrdinalIgnoreCase)) ?? throw new ArgumentException("Register a JN storage scope first.");
        jnRuntime.CheckScope(scope); return scope;
    }
    private async Task<string> ExecuteStorageAsync(EditSessionReference session,TaskRecord task,CancellationToken token)
    {
        StorageScope? scope; long quota;
        lock(gate) { scope=StorageTarget(task.Capability,task.Argument); quota=tasks.Storage.QuotaBytes; }
        var report=await jnRuntime!.ExecuteAsync(task,scope,quota,token);
        using var lease=TaskLease(session,true); lock(gate)
        {
            token.ThrowIfCancellationRequested(); if(FindTask(task.Id).Status!=Tasks.TaskStatus.Running) throw new OperationCanceledException();
            _=StorageTarget(task.Capability,task.Argument);
            if(task.Capability!="jn.quota") return report.Summary;
            var previous=tasks.Storage.Proposals.SingleOrDefault(p=>p.TaskId==task.Id); if(previous is not null) return "Existing quota proposal "+previous.Id+"; inspect with jn quota. "+report.Summary;
            var requested=long.Parse(task.Argument)*1_000_000_000;
            if(requested<=tasks.Storage.QuotaBytes || tasks.Storage.Proposals.Count>=32) throw new InvalidOperationException("Quota request must expand the current budget; proposal limit is 32.");
            var mcp=tasks.Agents.Single(a=>a.Role==AgentRole.MCP);
            var proposal=new QuotaProposal(Guid.NewGuid(),task.Id,mcp.Id,requested,report.Bytes,report.Complete,timeProvider.GetUtcNow().ToUniversalTime(),lease.Identity.OperatorId,null,null);
            tasks=tasks with { Storage=tasks.Storage with { Proposals=Array.AsReadOnly(tasks.Storage.Proposals.Append(proposal).ToArray()) } };
            RecordEvent("QuotaEscalatedToMCP",mcp.Id,"MCP",$"Quota proposal {proposal.Id} awaits separate human approval; budget unchanged.");
            return $"Quota proposal {proposal.Id} escalated to MCP; use jn quota to review. Budget unchanged pending separate Master Control approval. "+report.Summary;
        }
    }
    public void ApproveStorageQuota(EditSessionReference session,Guid id)
    {
        using var lease=TaskLease(session,true); lock(gate)
        {
            var p=tasks.Storage.Proposals.SingleOrDefault(p=>p.Id==id) ?? throw new ArgumentException("Unknown quota proposal.");
            if(p.ApprovedBy is not null || p.RequestedBytes<=tasks.Storage.QuotaBytes || FindTask(p.TaskId).Status!=Tasks.TaskStatus.Completed) throw new InvalidOperationException("Only a completed, pending expansion proposal can be approved.");
            var next=tasks.Storage with { QuotaBytes=p.RequestedBytes,Proposals=Array.AsReadOnly(tasks.Storage.Proposals.Select(x=>x.Id==id ? x with { ApprovedBy=lease.Identity.OperatorId,ApprovedUtc=timeProvider.GetUtcNow().ToUniversalTime() } : x).ToArray()) };
            StorageState.Validate(next,tasks); tasks=tasks with { Storage=next };
            RecordEvent("QuotaApproved",lease.Identity.OperatorId,null,$"Logical GRID quota increased to {p.RequestedBytes} bytes via proposal {p.Id}; no disk allocation or deletion.");
        }
    }
}
