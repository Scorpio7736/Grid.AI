// Compatibility facade: one definition in Grid.Agent, no duplicate state types.
using System.Runtime.CompilerServices;
[assembly: TypeForwardedTo(typeof(Grid.Simulation.Editing.EditSessionReference))]
[assembly: TypeForwardedTo(typeof(Grid.Simulation.Editing.ProgramEditIdentity))]
[assembly: TypeForwardedTo(typeof(Grid.Simulation.Editing.IProgramEditAuthorizer))]
[assembly: TypeForwardedTo(typeof(Grid.Simulation.Editing.IProgramEditLease))]

namespace Grid.Simulation.Editing;

internal sealed class DenyProgramEdits : IProgramEditAuthorizer
{
    public IProgramEditLease? Acquire(EditSessionReference session, Guid worldInstanceId) => null;
}
