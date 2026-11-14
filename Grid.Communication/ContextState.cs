// Compatibility facade: one definition in Grid.Agent, no duplicate state types.
using System.Runtime.CompilerServices;
[assembly: TypeForwardedTo(typeof(Grid.Communication.ContextDocument))]
[assembly: TypeForwardedTo(typeof(Grid.Communication.ContextState))]
