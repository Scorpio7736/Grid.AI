namespace Grid.Terminal;

/// <summary>
/// Source-controlled inventory for exceptional operator recovery work. This is
/// deliberately separate from the ordinary command dispatcher: entries here
/// describe reviewed contracts and do not make a command executable.
/// </summary>
public static class DevelopmentBreakGlassRegistry
{
    public const int Version = 1;
    public const string GenericDenial = "Operator recovery commands are unavailable in this terminal context.";

    public sealed record VisiblePath(string Syntax, string Safeguard);
    public sealed record HiddenContract(string Syntax, string SessionClass, bool MayMutate);

    public static IReadOnlyList<VisiblePath> VisibleRecoveryPaths { get; } =
    [
        new("Grid.Terminal --recover <world-path>", "Explicit startup recovery path; backup validation remains authoritative."),
        new("nge", "Authenticated Master Control plus original-host interactive confirmation."),
        new("grid -copyto <destination>", "Reviewed destination, save, copy and hash verification; source retained."),
        new("grid -transferto <destination>", "Reviewed destination, save, copy and hash verification before eligible source cleanup."),
        new("logout", "Ends only the active authenticated GRID session."),
        new("exit", "Stops the simulation and saves before host shutdown."),
        new("context revoke <id|name>", "Revokes only the selected captured-context entry."),
        new("maxis revoke <workspace>", "Revokes only the selected registered workspace."),
        new("jn revoke <root>", "Revokes only the selected JN root."),
        new("device revoke <device-id>", "Revokes only the selected device grant.")
    ];

    public static IReadOnlyList<HiddenContract> HiddenContracts { get; } =
    [
        new("::grid-rescue status", "locked-local", false),
        new("::grid-rescue verify-world", "locked-local", false),
        new("::grid-rescue freeze-request", "locked-local", true),
        new("::grid-rescue recovery-challenge", "locked-local", true),
        new("::grid-rescue cancel <operation-id>", "locked-local", true),
        new("::grid-agent-break self-status", "registered-agent", false),
        new("::grid-agent-break abort-self", "registered-agent", true),
        new("::grid-agent-break detach-self", "registered-agent", true),
        new("::grid-agent-break evidence-self", "registered-agent", false),
        new("::grid-breakglass status", "master-control-original-host", false),
        new("::grid-breakglass freeze [<operation-id> <nonce>]", "master-control-original-host", true),
        new("::grid-breakglass isolate [<operation-id> <nonce>]", "master-control-original-host", true),
        new("::grid-breakglass revoke-sessions [<operation-id> <nonce>]", "master-control-original-host", true),
        new("::grid-breakglass disable-remote [<operation-id> <nonce>]", "master-control-original-host", true),
        new("::grid-breakglass safe-mode-plan", "master-control-original-host", false),
        new("::grid-breakglass safe-mode-commit <operation-id> <nonce>", "master-control-original-host", true),
        new("::grid-breakglass recovery-plan backup", "master-control-original-host", false),
        new("::grid-breakglass recovery-commit <operation-id> <nonce>", "master-control-original-host", true),
        new("::grid-breakglass evidence-export <approved-local-path> [<operation-id> <nonce>]", "master-control-original-host", true),
        new("::grid-breakglass nullify-plan <world-id>", "master-control-original-host", false),
        new("::grid-breakglass nullify-commit <operation-id> <world-id> <nonce>", "master-control-original-host", true),
        new("::grid-breakglass cancel <operation-id>", "master-control-original-host", true)
    ];

    /// <summary>Recognizes the reserved namespace without confirming a particular command exists.</summary>
    public static bool IsReservedInput(string? token) =>
        token?.StartsWith("::grid-", StringComparison.OrdinalIgnoreCase) == true;

    public static void Validate()
    {
        if (Version < 1) throw new InvalidOperationException("Break-glass registry version must be positive.");
        if (VisibleRecoveryPaths.Count == 0 || HiddenContracts.Count == 0)
            throw new InvalidOperationException("Break-glass source inventories cannot be empty.");
        if (VisibleRecoveryPaths.Any(path => string.IsNullOrWhiteSpace(path.Syntax) || string.IsNullOrWhiteSpace(path.Safeguard)))
            throw new InvalidOperationException("Every visible recovery path requires syntax and a safeguard.");
        if (HiddenContracts.Any(contract => string.IsNullOrWhiteSpace(contract.Syntax) ||
                                            string.IsNullOrWhiteSpace(contract.SessionClass) ||
                                            !IsReservedInput(FirstToken(contract.Syntax))))
            throw new InvalidOperationException("Every hidden contract requires reserved syntax and a session class.");
        if (HiddenContracts.GroupBy(contract => contract.Syntax, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() != 1))
            throw new InvalidOperationException("Hidden command contracts must be unique.");
        if (VisibleRecoveryPaths.Select(path => FirstToken(path.Syntax))
            .Any(token => HiddenContracts.Select(contract => FirstToken(contract.Syntax))
                .Contains(token, StringComparer.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Hidden command families cannot alias visible recovery commands.");
    }

    private static string FirstToken(string syntax) => syntax.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
}
