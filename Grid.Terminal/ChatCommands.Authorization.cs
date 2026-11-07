using Grid.Agent;
using Grid.AI;
using Grid.Simulation;
namespace Grid.Terminal;

public sealed partial class ChatCommands
{
    private RequestAuthority.Lease AuthorizeInference(Guid conversationId, Guid agentId, string capability, Func<bool> current)
    {
        var session = identity ?? throw new UnauthorizedAccessException("Login required.");
        var reference = session.Reference; var conversation = Find(conversationId); var actor = session.CurrentRecipient;
        var world = engine.GetSnapshot().WorldId;
        var context = new TrustedRequestContext(world, engine.LocalOperator.Id, actor.Id, agentId, reference.SessionId,
            reference.Generation, conversationId, conversation.ProjectScope, capability);
        bool Validate() => current() && session.Engine == engine && session.IsAuthenticated && session.Reference == reference && session.Allows("chat") &&
            (capability == "ai.chat" || session.IsMaster) && engine.GetSnapshot().WorldId == world &&
            Find(conversationId).ProjectScope == context.Project && Find(conversationId).Participants.Any(p => p.Id == actor.Id) &&
            Find(conversationId).Participants.Any(p => p.Id == agentId) && (capability == "ai.chat" || engine.GetSnapshot().Identities.RegisteredAgents.Contains(agentId));
        return TerminalRequestAuthorization.For(engine).Acquire(context, Validate);
    }
}
