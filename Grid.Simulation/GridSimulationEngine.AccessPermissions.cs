using Grid.Agent;
using Grid.Simulation.Editing;

namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    public IReadOnlyList<AccessPermission> AccessPermissions(EditSessionReference session)
    {
        using var lease = AccessPermissionLease(session);
        lock (gate) return Array.AsReadOnly(communications.AccessPermissions.ToArray());
    }

    public void GrantAccessPermission(EditSessionReference session, string path, AccessRiskTier risk, AccessGrantDuration duration)
    {
        path = path?.Trim() ?? "";
        if (path.Length is 0 or > 160 || path.Any(char.IsControl)) throw new ArgumentException("Invalid access path.");
        using var lease = AccessPermissionLease(session);
        lock (gate)
        {
            var retained = communications.AccessPermissions.Where(p => p.Path != path || p.Risk != risk ||
                p.Duration != duration || duration == AccessGrantDuration.Session && p.SessionId != session.SessionId).ToList();
            if (retained.Count >= 128) throw new InvalidOperationException("Access-permission capacity reached; revoke an old grant first.");
            retained.Add(new(Guid.NewGuid(), path, risk, duration, session.SessionId, timeProvider.GetUtcNow()));
            communications = communications with { AccessPermissions = Array.AsReadOnly(retained.ToArray()) };
            RecordEvent("AccessPermissionGranted", null, null, $"{risk}/{duration}: {path}");
        }
    }

    public bool ClaimAccessPermission(EditSessionReference session, string path, AccessRiskTier risk)
    {
        using var lease = AccessPermissionLease(session);
        lock (gate)
        {
            var permission = communications.AccessPermissions.LastOrDefault(p => p.Path == path && p.Risk == risk && p.Available(session.SessionId));
            if (permission is null) return false;
            if (permission.Duration == AccessGrantDuration.Once)
            {
                var consumed = permission with { ConsumedAtUtc = timeProvider.GetUtcNow() };
                communications = communications with
                {
                    AccessPermissions = Array.AsReadOnly(communications.AccessPermissions.Select(p => p.Id == permission.Id ? consumed : p).ToArray())
                };
            }
            RecordEvent("AccessPermissionClaimed", permission.Id, null, $"{risk}/{permission.Duration}: {path}");
            return true;
        }
    }

    public bool HasAccessPermission(EditSessionReference session, string path, AccessRiskTier risk)
    {
        using var lease = AccessPermissionLease(session);
        lock (gate) return communications.AccessPermissions.Any(p => p.Path == path && p.Risk == risk && p.Available(session.SessionId));
    }

    public void RevokeAccessPermission(EditSessionReference session, Guid id)
    {
        using var lease = AccessPermissionLease(session);
        lock (gate)
        {
            var permission = communications.AccessPermissions.SingleOrDefault(p => p.Id == id)
                ?? throw new ArgumentException("Unknown access permission.");
            if (permission.Revoked) return;
            var revoked = permission with { Revoked = true, ConsumedAtUtc = permission.ConsumedAtUtc ?? timeProvider.GetUtcNow() };
            communications = communications with
            {
                AccessPermissions = Array.AsReadOnly(communications.AccessPermissions.Select(p => p.Id == id ? revoked : p).ToArray())
            };
            RecordEvent("AccessPermissionRevoked", id, null, $"{permission.Risk}/{permission.Duration}: {permission.Path}");
        }
    }

    private IProgramEditLease AccessPermissionLease(EditSessionReference session)
    {
        var lease = editAuthorizer.Acquire(session, world.InstanceId) ?? throw new UnauthorizedAccessException("Authenticated Master Control is required.");
        if (!lease.Identity.IsMasterControl || !lease.Identity.IsOriginalHost)
        { lease.Dispose(); throw new UnauthorizedAccessException("Access permissions require original Master Control."); }
        return lease;
    }
}
