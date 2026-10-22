namespace Grid.Terminal;

/// <summary>Register a command to extend the terminal; help is generated from this metadata.</summary>
public sealed record TerminalCommand(string Name, string Usage, string Description,
    Func<IReadOnlyList<string>, CommandResult> Execute)
{
    public string Capability { get; init; } = "world.admin";
}
public sealed record CommandResult(string Text, bool ExitRequested = false, bool LiveViewRequested = false, bool ClearRequested = false, bool NullificationRequested = false, bool LoginRequested = false, bool SetupRequested = false, bool LogoutRequested = false, TransferRequest? Transfer = null, Func<IReadOnlyList<string>, CommandResult>? ContextPicker = null, VoiceOperation? Voice = null, Func<CancellationToken, Task<CommandResult>>? BrokerOperation = null, int? AnimateFrom = null, Func<CommandResult>? ConfirmTaskBatch = null);
