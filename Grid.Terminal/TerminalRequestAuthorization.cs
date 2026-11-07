using System.Runtime.CompilerServices;
using Grid.Agent;
using Grid.AI;
using Grid.Simulation;

namespace Grid.Terminal;

internal static class TerminalRequestAuthorization
{
    private static readonly ConditionalWeakTable<GridSimulationEngine, RequestAuthority> Authorities = new();

    internal static RequestAuthority For(GridSimulationEngine engine) => Authorities.GetValue(engine, _ => new());

    internal static RequestAuthority.Lease AcquireStandalone(TerminalIdentitySession identity, string capability, Func<bool> current, string project = "diagnostics")
    {
        var engine = identity.Engine;
        var reference = identity.Reference;
        var world = engine.GetSnapshot().WorldId;
        var actor = identity.CurrentRecipient;
        var agent = capability == "ai.file-draft"
            ? engine.GetSnapshot().Programs.FirstOrDefault(p => p.Name.Equals("MAXIS", StringComparison.OrdinalIgnoreCase))?.Id ?? Guid.Empty
            : Guid.Empty;
        var context = new TrustedRequestContext(world, engine.LocalOperator.Id, actor.Id, agent,
            reference.SessionId, reference.Generation, Guid.Empty,
            ConversationMemory.ValidLabel(project) ? project.ToLowerInvariant() : "file-draft", capability);

        bool Validate() => current() && identity.IsMaster && identity.Reference == reference &&
            identity.Engine == engine && engine.GetSnapshot().WorldId == world &&
            capability is "ai.file-draft" or "ai.diagnostic";

        return For(engine).Acquire(context, Validate);
    }
}
