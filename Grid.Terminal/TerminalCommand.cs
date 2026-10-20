namespace Grid.Terminal;

/// <summary>Register a command to extend the terminal; help is generated from this metadata.</summary>
public sealed record TerminalCommand(string Name, string Usage, string Description,
    Func<IReadOnlyList<string>, CommandResult> Execute);
public sealed record CommandResult(string Text, bool ExitRequested = false, bool LiveViewRequested = false, bool ClearRequested = false, bool NullificationRequested = false);
