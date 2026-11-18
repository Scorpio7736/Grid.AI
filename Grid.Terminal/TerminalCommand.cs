namespace Grid.Terminal;

/// <summary>Register a command to extend the terminal; help is generated from this metadata.</summary>
public sealed record TerminalCommand(string Name, string Usage, string Description,
    Func<IReadOnlyList<string>, CommandResult> Execute)
{
    public string Capability { get; init; } = "world.admin";
}
public enum ChallengeUiAction { Test, Disable }
public sealed record CommandResult(string Text, bool ExitRequested = false, bool LiveViewRequested = false, bool ClearRequested = false, bool NullificationRequested = false, bool LoginRequested = false, bool SetupRequested = false, bool LogoutRequested = false, TransferRequest? Transfer = null, Func<IReadOnlyList<string>, CommandResult>? ContextPicker = null, VoiceOperation? Voice = null, Func<CancellationToken, Task<CommandResult>>? BrokerOperation = null, int? AnimateFrom = null, Func<CommandResult>? ConfirmTaskBatch = null, IReadOnlyList<ConversationSpan>? Presentation = null, WorkspaceChangeReceipt? FileChange = null, ChallengeUiAction? ChallengeAction = null, WorldRecoveryRequest? WorldRecovery = null, string? RecoveryKeyTarget = null);

public sealed record WorkspaceChangeReceipt(Guid Id, Grid.Simulation.Tasks.WorkspaceFileOperation Operation, string State, bool HighRisk, string AbsolutePath = "");
public sealed record WorldRecoveryRequest(Grid.Persistence.WorldRecoveryPlan Plan, Func<bool> StillAuthorized, Action<string, bool>? Report = null);
