using Grid.Simulation;
using Grid.Simulation.Tasks;
using Grid.Communication;
namespace Grid.Terminal;
public sealed class LocalRelayVoice : IRelayVoice
{
    private readonly ILocalVoiceProvider provider = new WindowsVoiceProvider();
    public Task SpeakAsync(string text,VoiceProfile? profile,CancellationToken token)=>provider.SpeakAsync(text,profile,token);
}
public static class VoltRelayCommands
{
    public static void Register(CommandDispatcher commands,GridSimulationEngine engine,TerminalIdentitySession identity,Func<CancellationToken,Task>? save)
    {
        commands.Register(new("volt","volt [status]","Read VOLT resource freshness and GRID pacing. Changes use approved volt.throttle tasks.",args=>
        {
            try { return new(args.Count==0 || args.Count==1 && args[0]=="status" ? engine.VoltStatus(identity.Reference).Replace(" | ","\n") : "Usage: volt status. Change pacing: tasks propose VOLT volt.throttle <normal|eco|auto>"); }
            catch(Exception e) when(e is ArgumentException or InvalidOperationException or UnauthorizedAccessException) { return new("VOLT error: "+e.Message); }
        }) { Capability="session" });
        commands.Register(new("relay","relay <inbox|inspect|deliveries|ack> ...","Read local notifications and delivery evidence. Speech requires an explicit approved relay.speak task.",args=>
        {
            try
            {
                var state=engine.RelayFor(identity.Reference);
                if(args.Count==0 || args.Count==1 && args[0]=="inbox")
                    return new("RELAY LOCAL INBOX\n"+TerminalTable.Render(["Notification","Acknowledged","Preview"],state.Notifications.Select(n=>new[]{TaskDisplay.Ref(n.Id,state.Notifications.Select(x=>x.Id)),n.AcknowledgedBy is null ? "No" : "Yes",n.Text.Length>60 ? n.Text[..60]+"..." : n.Text}))+"\nDetails: relay inspect <notification-reference>\nDelivered to inbox does not mean read or heard.");
                if(args.Count==1 && args[0]=="deliveries")
                    return new(TerminalTable.Render(["Notification","Channel","Attempt","Status","Detail"],state.Deliveries.Select(d=>new[]{TaskDisplay.Ref(d.NotificationId,state.Notifications.Select(x=>x.Id)),d.Channel,d.Attempt.ToString(),d.Status.ToString(),d.Detail})));
                if(args.Count==2 && args[0] is "inspect" or "ack")
                {
                    var id=TaskDisplay.Resolve(args[1],state.Notifications.Select(n=>n.Id),"notification");
                    if(args[0]=="ack")
                    {
                        if(save is null) return new("Acknowledgement requires a saved world.");
                        engine.AcknowledgeRelay(identity.Reference,id); save(CancellationToken.None).GetAwaiter().GetResult(); return new("Human acknowledgement saved.");
                    }
                    var n=state.Notifications.Single(n=>n.Id==id);
                    return new($"NOTIFICATION {n.Id}\nCreated UTC: {n.CreatedUtc:O}\nOrigin task: {n.TaskId}\nBriefed task: {n.SourceTaskId}\nAcknowledged by: {n.AcknowledgedBy}\nAcknowledged UTC: {n.AcknowledgedUtc:O}\n\n{n.Text}\n\nOptional explicit speech proposal (uses the full ID):\ntasks propose RELAY relay.speak {n.Id}");
                }
                return new("Usage: relay inbox | relay inspect <notification-reference> | relay deliveries | relay ack <notification-reference>. No external messaging or automatic playback.");
            }
            catch(Exception e) when(e is ArgumentException or InvalidOperationException or UnauthorizedAccessException or IOException or System.Data.Common.DbException)
            { return new("RELAY error: "+e.Message+(e is IOException or System.Data.Common.DbException ? " If saving failed, in-memory changes remain." : "")); }
        }) { Capability="session" });
    }
}
