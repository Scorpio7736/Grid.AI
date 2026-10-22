using Grid.Simulation.Editing;
using Grid.Simulation.Resources;
using Grid.Simulation.Tasks;
using System.Globalization;
namespace Grid.Simulation;
public sealed partial class GridSimulationEngine
{
    private DateTimeOffset voltPressureUntil=DateTimeOffset.MinValue;
    private IRelayVoice? relayVoice;
    public void ConfigureRelayVoice(IRelayVoice voice) { ArgumentNullException.ThrowIfNull(voice); lock(gate) { if(taskExecuting) throw new InvalidOperationException("Broker busy."); relayVoice=voice; } }
    private bool RoleView(ProgramEditIdentity actor,AgentRole role)=>actor.IsMasterControl || tasks.Agents.Any(a=>a.Id==actor.AgentId && (a.Role==role || a.Role==AgentRole.MCP));
    private int VoltMultiplier()
    {
        if(tasks.VoltMode=="normal") return 1;
        if(tasks.VoltMode=="eco") return 4;
        var now=timeProvider.GetUtcNow(); var readings=hostResources.Select(r=>r.At(now,TimeSpan.FromSeconds(10))).ToArray();
        var cpu=readings.Where(r=>r.Metric==ResourceMetric.CpuAvailablePercent).ToArray();
        var ram=readings.SingleOrDefault(r=>r.Metric==ResourceMetric.RamAvailableBytes && r.Scope=="host");
        var capacity=readings.SingleOrDefault(r=>r.Metric==ResourceMetric.RamCapacityBytes && r.Scope=="host");
        var pressure=cpu.Any(r=>r.Value is <15) || readings.Any(r=>r.Metric==ResourceMetric.GpuAvailablePercent && r.Value is <10) || ram?.Value is {} free && capacity?.Value is >0 && free/capacity.Value.Value<0.1;
        if(pressure) voltPressureUntil=now.AddSeconds(10);
        if(now<voltPressureUntil) return 4;
        return cpu.Length==0 || cpu.Any(r=>r.Value is null) || ram?.Value is null || capacity?.Value is null or <=0 ? 2 : 1;
    }
    public TimeSpan AutomaticTickDelay { get { lock(gate) return TimeSpan.FromMilliseconds(Math.Min(TickDuration.TotalMilliseconds*VoltMultiplier(),uint.MaxValue-1)); } }
    public string VoltStatus(EditSessionReference session)
    {
        using var lease=TaskLease(session); lock(gate)
        {
            if(!RoleView(lease.Identity,AgentRole.VOLT)) throw DeniedTask(lease.Identity,"VOLT status requires Master Control, MCP or VOLT.");
            var selected=GetHostResources().Where(r=>r.Metric is ResourceMetric.CpuAvailablePercent or ResourceMetric.RamAvailableBytes or ResourceMetric.RamCapacityBytes or ResourceMetric.GpuAvailablePercent or ResourceMetric.VramAvailableBytes).ToArray();
            string Safe(string value)=>new(value.Where(c=>!char.IsControl(c) && c!='|').Take(70).ToArray());
            var lines=selected.Take(6).Select(r=>$"{r.Metric} [{Safe(r.Scope)}]: {(r.Value is {} v ? v.ToString("0.##",CultureInfo.InvariantCulture)+" "+r.Unit : "unknown")} ({r.Status}; {r.ObservedAtUtc:O}; {Safe(r.Source)})");
            return $"[REAL HOST TELEMETRY / GRID PACING] Mode={tasks.VoltMode}; automatic delay multiplier={VoltMultiplier()}; effective wait={AutomaticTickDelay.TotalMilliseconds:0} ms. No Windows allocation/priority changes. | "+(selected.Length==0 ? "No observations yet; CPU/RAM/GPU unknown." : string.Join(" | ",lines))+(selected.Length>6 ? " | Additional scopes omitted; use resources details." : "")+" | Available percent means headroom, not utilization. GPU/VRAM may be unsupported. Manual step and simulated tick duration are unchanged.";
        }
    }
    private string ExecuteVolt(EditSessionReference session,TaskRecord task)
    {
        using(var lease=TaskLease(session,true)) lock(gate)
        {
            if(task.Capability=="volt.throttle") { tasks=tasks with { VoltMode=task.Argument }; voltPressureUntil=DateTimeOffset.MinValue; RecordEvent("VoltPacingChanged",lease.Identity.OperatorId,"VOLT","GRID automatic pacing mode: "+task.Argument); }
        }
        return VoltStatus(session);
    }
    public RelayState RelayFor(EditSessionReference session)
    { using var lease=TaskLease(session); lock(gate) { if(!RoleView(lease.Identity,AgentRole.RELAY)) throw DeniedTask(lease.Identity,"RELAY inbox requires Master Control, MCP or RELAY."); return tasks.Relay.Copy(); } }
    public void AcknowledgeRelay(EditSessionReference session,Guid id)
    {
        using var lease=TaskLease(session,true); lock(gate)
        {
            var note=tasks.Relay.Notifications.SingleOrDefault(n=>n.Id==id) ?? throw new ArgumentException("Unknown notification."); if(note.AcknowledgedBy is not null) return;
            tasks=tasks with { Relay=tasks.Relay with { Notifications=Array.AsReadOnly(tasks.Relay.Notifications.Select(n=>n.Id==id ? n with { AcknowledgedBy=lease.Identity.OperatorId,AcknowledgedUtc=timeProvider.GetUtcNow().ToUniversalTime() } : n).ToArray()) } };
            RecordEvent("RelayAcknowledged",lease.Identity.OperatorId,"RELAY","Human acknowledged notification "+id);
        }
    }
    private void DeliveryFinished(Guid id,RelayDeliveryStatus status,string detail)
    {
        lock(gate)
        {
            tasks=tasks with { Relay=tasks.Relay with { Deliveries=Array.AsReadOnly(tasks.Relay.Deliveries.Select(d=>d.Id==id ? d with { Status=status,FinishedUtc=timeProvider.GetUtcNow().ToUniversalTime(),Detail=detail } : d).ToArray()) } };
            RecordEvent("RelayDelivery"+status,id,"RELAY",detail);
        }
    }
    private async Task<string> ExecuteRelayAsync(EditSessionReference session,TaskRecord task,Func<CancellationToken,Task> save,CancellationToken token)
    {
        RelayNotification note; RelayDelivery delivery; Grid.Communication.VoiceProfile? profile;
        using(var lease=TaskLease(session,true)) lock(gate)
        {
            token.ThrowIfCancellationRequested();
            if(tasks.Relay.Deliveries.Count>=512) throw new InvalidOperationException("RELAY delivery history is full; history was not discarded.");
            var now=timeProvider.GetUtcNow().ToUniversalTime();
            if(task.Capability=="relay.speak") note=tasks.Relay.Notifications.SingleOrDefault(n=>n.Id==Guid.Parse(task.Argument)) ?? throw new ArgumentException("Unknown notification to speak.");
            else
            {
                var existing=tasks.Relay.Notifications.SingleOrDefault(n=>n.TaskId==task.Id);
                if(existing is not null) note=existing;
                else
                {
                if(tasks.Relay.Notifications.Count>=256) throw new InvalidOperationException("RELAY inbox full; no messages deleted.");
                Guid? source=null; var text=task.Argument;
                if(task.Capability=="relay.brief")
                {
                    var original=FindTask(Guid.Parse(task.Argument));
                    if(original.Status is Tasks.TaskStatus.AwaitingApproval or Tasks.TaskStatus.Queued or Tasks.TaskStatus.Running || original.Result is null) throw new InvalidOperationException("Briefing requires a terminal task with a recorded result.");
                    source=original.Id; text=$"Recorded task {original.Id} | {original.Capability} | {original.Status} | {original.Result}";
                }
                note=new(Guid.NewGuid(),task.Id,source,text,now);
                tasks=tasks with { Relay=tasks.Relay with { Notifications=Array.AsReadOnly(tasks.Relay.Notifications.Append(note).ToArray()) } };
                }
            }
            delivery=new(Guid.NewGuid(),task.Id,note.Id,task.Capability=="relay.speak" ? "speech" : "inbox",task.Attempts+1,RelayDeliveryStatus.Started,now,null,"Delivery started; no user receipt is claimed.");
            tasks=tasks with { Relay=tasks.Relay with { Deliveries=Array.AsReadOnly(tasks.Relay.Deliveries.Append(delivery).ToArray()) } };
            profile=communications.VoiceProfiles.SingleOrDefault(p=>p.AgentId==task.AgentId);
        }
        try
        {
            await save(token); // Save delivery intent before audible side effects.
            using(var lease=TaskLease(session,true)) lock(gate) { token.ThrowIfCancellationRequested(); if(FindTask(task.Id).Status!=Tasks.TaskStatus.Running) throw new OperationCanceledException(); }
            if(delivery.Channel=="speech")
            {
                var voice=relayVoice ?? throw new InvalidOperationException("Local RELAY speech provider is unavailable.");
                await voice.SpeakAsync(note.Text,profile,token); token.ThrowIfCancellationRequested();
            }
            using(var lease=TaskLease(session,true)) { token.ThrowIfCancellationRequested(); }
            DeliveryFinished(delivery.Id,RelayDeliveryStatus.Delivered,delivery.Channel=="inbox" ? "Stored in local inbox; not proof the user read it." : "Local speech provider completed; not proof the user heard it.");
            return $"RELAY {delivery.Channel} delivery recorded for notification {note.Id}. Use relay inspect {note.Id}.";
        }
        catch(OperationCanceledException) { DeliveryFinished(delivery.Id,RelayDeliveryStatus.Cancelled,"Delivery cancelled; speech may have partially played. No automatic replay."); throw; }
        catch(UnauthorizedAccessException) { DeliveryFinished(delivery.Id,RelayDeliveryStatus.Interrupted,"Authorization changed; delivery outcome may be partial."); throw; }
        catch(Exception e) when(e is IOException or InvalidDataException or InvalidOperationException or ArgumentException or System.Data.Common.DbException or System.ComponentModel.Win32Exception)
        { DeliveryFinished(delivery.Id,RelayDeliveryStatus.Failed,"Delivery failed: "+e.GetType().Name+". Inspect the task result; no receipt claimed."); throw; }
    }
}
