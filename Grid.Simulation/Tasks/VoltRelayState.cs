using Grid.Communication;
namespace Grid.Simulation.Tasks;
public interface IRelayVoice
{
    Task SpeakAsync(string text,VoiceProfile? profile,CancellationToken token);
}
public sealed record RelayNotification(Guid Id,Guid TaskId,Guid? SourceTaskId,string Text,DateTimeOffset CreatedUtc,Guid? AcknowledgedBy=null,DateTimeOffset? AcknowledgedUtc=null);
public enum RelayDeliveryStatus { Started,Delivered,Failed,Cancelled,Interrupted }
public sealed record RelayDelivery(Guid Id,Guid TaskId,Guid NotificationId,string Channel,int Attempt,RelayDeliveryStatus Status,DateTimeOffset StartedUtc,DateTimeOffset? FinishedUtc,string Detail);
public sealed record RelayState
{
    public IReadOnlyList<RelayNotification> Notifications { get; init; }=Array.Empty<RelayNotification>();
    public IReadOnlyList<RelayDelivery> Deliveries { get; init; }=Array.Empty<RelayDelivery>();
    public RelayState Copy()=>this with { Notifications=Array.AsReadOnly(Notifications.ToArray()),Deliveries=Array.AsReadOnly(Deliveries.ToArray()) };
    public static void Validate(RelayState state,TaskState tasks)
    {
        if(state is null || state.Notifications is null || state.Deliveries is null || state.Notifications.Count>256 || state.Deliveries.Count>512 || state.Notifications.Any(n=>n is null) || state.Deliveries.Any(d=>d is null)) throw new InvalidDataException("Invalid RELAY state.");
        if(state.Notifications.Select(n=>n.Id).Distinct().Count()!=state.Notifications.Count || state.Notifications.Select(n=>n.TaskId).Distinct().Count()!=state.Notifications.Count || state.Deliveries.Select(d=>d.Id).Distinct().Count()!=state.Deliveries.Count || state.Deliveries.Select(d=>(d.TaskId,d.Attempt)).Distinct().Count()!=state.Deliveries.Count) throw new InvalidDataException("Duplicate RELAY records.");
        foreach(var n in state.Notifications)
            if(n.Id==Guid.Empty || !TaskState.Label(n.Text,4096) || n.CreatedUtc.Offset!=TimeSpan.Zero || n.AcknowledgedBy==Guid.Empty || (n.AcknowledgedBy is null)!=(n.AcknowledgedUtc is null) || n.AcknowledgedUtc is {} ack && (ack.Offset!=TimeSpan.Zero || ack<n.CreatedUtc) ||
                !tasks.Records.Any(t=>t.Id==n.TaskId && (n.AcknowledgedBy is null || n.AcknowledgedBy==t.OperatorId) && (t.Capability=="relay.notify" && n.SourceTaskId is null && n.Text==t.Argument || t.Capability=="relay.brief" && Guid.TryParse(t.Argument,out var briefId) && n.SourceTaskId==briefId)) || n.SourceTaskId is {} source && !tasks.Records.Any(t=>t.Id==source)) throw new InvalidDataException("Invalid RELAY notification linkage.");
        foreach(var d in state.Deliveries)
            if(d.Id==Guid.Empty || !Enum.IsDefined(d.Status) || d.Channel is not ("inbox" or "speech") || d.Attempt is <1 or >3 || d.StartedUtc.Offset!=TimeSpan.Zero || !TaskState.Label(d.Detail,512) ||
                (d.Status==RelayDeliveryStatus.Started)!=(d.FinishedUtc is null) || d.FinishedUtc is {} end && (end.Offset!=TimeSpan.Zero || end<d.StartedUtc) || !state.Notifications.Any(n=>n.Id==d.NotificationId) ||
                !tasks.Records.Any(t=>t.Id==d.TaskId && d.Attempt<=t.Attempts && (d.Channel=="speech" ? Guid.TryParse(t.Argument,out var speechId) && speechId==d.NotificationId : state.Notifications.Single(n=>n.Id==d.NotificationId).TaskId==t.Id) && t.Capability==(d.Channel=="speech" ? "relay.speak" : state.Notifications.Single(n=>n.Id==d.NotificationId).SourceTaskId is null ? "relay.notify" : "relay.brief"))) throw new InvalidDataException("Invalid RELAY delivery linkage.");
    }
}
public static class VoltRelayPolicy
{
    public static bool Valid(AgentRole role,string capability,string argument) => role switch
    {
        AgentRole.VOLT => capability=="volt.status" && argument=="current" || capability=="volt.throttle" && argument is "normal" or "eco" or "auto",
        AgentRole.RELAY => capability=="relay.notify" && TaskState.Label(argument,256) || capability is "relay.brief" or "relay.speak" && Guid.TryParse(argument,out var id) && id!=Guid.Empty,
        _=>false
    };
    public static bool IsReal(string capability)=>capability is "volt.status" or "volt.throttle" or "relay.notify" or "relay.brief" or "relay.speak";
}
