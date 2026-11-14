// Compatibility facade: one definition in Grid.Agent, no duplicate state types.
using System.Runtime.CompilerServices;
[assembly: TypeForwardedTo(typeof(Grid.Communication.ParticipantKind))]
[assembly: TypeForwardedTo(typeof(Grid.Communication.MessageOrigin))]
[assembly: TypeForwardedTo(typeof(Grid.Communication.ConversationStatus))]
[assembly: TypeForwardedTo(typeof(Grid.Communication.Recipient))]
[assembly: TypeForwardedTo(typeof(Grid.Communication.GridMessage))]
[assembly: TypeForwardedTo(typeof(Grid.Communication.Conversation))]
[assembly: TypeForwardedTo(typeof(Grid.Communication.CommunicationState))]
[assembly: TypeForwardedTo(typeof(Grid.Communication.IGridCommunicationHub))]
