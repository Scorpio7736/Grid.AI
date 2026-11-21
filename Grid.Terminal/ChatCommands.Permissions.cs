using Grid.Agent;

namespace Grid.Terminal;

public sealed partial class ChatCommands
{
    private CommandResult Permissions(IReadOnlyList<string> args)
    {
        if (identity?.IsMaster != true) return new("Access permissions require Master Control.");
        var permissions = engine.AccessPermissions(identity.Reference);
        if (args.Count == 0 || args.Count == 1 && args[0].Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            if (permissions.Count == 0) return new("No saved access permissions.");
            var rows = permissions.OrderBy(p => p.Risk).ThenBy(p => p.Path).ThenByDescending(p => p.GrantedAtUtc).Select(p => new[]
            {
                p.Id.ToString("N")[..8], p.Risk.ToString(), p.Path, p.Duration.ToString(),
                p.Revoked ? "Revoked" : p.ConsumedAtUtc is not null ? "Consumed" : p.Duration == AccessGrantDuration.Session && p.SessionId != identity.SessionId ? "Expired session" : "Active"
            });
            return new(TerminalTable.Render(["ID", "Risk", "Access path", "Duration", "State"], rows) +
                "\nNormal = read/observe | Medium = create/write/update | High = delete/destructive\nSaved access never replaces exact change/task review and approval.");
        }
        if (args.Count == 2 && args[0].Equals("revoke", StringComparison.OrdinalIgnoreCase))
        {
            var matches = permissions.Where(p => p.Id.ToString().StartsWith(args[1], StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1) return new(matches.Length == 0 ? "Unknown permission ID." : "Ambiguous permission ID; provide more characters.");
            engine.RevokeAccessPermission(identity.Reference, matches[0].Id);
            save?.Invoke(CancellationToken.None).GetAwaiter().GetResult();
            return new("Access permission revoked: " + matches[0].Path + ". Future requests must ask again.");
        }
        return new("Usage: permissions [list] | permissions revoke <id>");
    }
}
