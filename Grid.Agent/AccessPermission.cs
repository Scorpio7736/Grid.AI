namespace Grid.Agent;

public enum AccessRiskTier { Normal, Medium, High }
public enum AccessGrantDuration { Once, Session, Always }

/// <summary>A user-issued access grant. It authorizes a path, never an AI-authored action or result.</summary>
public sealed record AccessPermission(Guid Id, string Path, AccessRiskTier Risk, AccessGrantDuration Duration,
    Guid SessionId, DateTimeOffset GrantedAtUtc, DateTimeOffset? ConsumedAtUtc = null, bool Revoked = false)
{
    public bool Available(Guid sessionId) => !Revoked && ConsumedAtUtc is null &&
        (Duration != AccessGrantDuration.Session || SessionId == sessionId);

    public static void Validate(IReadOnlyList<AccessPermission> permissions)
    {
        if (permissions is null || permissions.Count > 128 || permissions.Any(p => p is null))
            throw new InvalidDataException("Invalid access-permission state.");
        var ids = new HashSet<Guid>();
        foreach (var permission in permissions)
            if (permission.Id == Guid.Empty || !ids.Add(permission.Id) || permission.SessionId == Guid.Empty ||
                string.IsNullOrWhiteSpace(permission.Path) || permission.Path.Length > 160 || permission.Path != permission.Path.Trim() ||
                permission.Path.Any(char.IsControl) || !Enum.IsDefined(permission.Risk) || !Enum.IsDefined(permission.Duration) ||
                permission.GrantedAtUtc.Offset != TimeSpan.Zero || permission.ConsumedAtUtc is { } consumed && consumed.Offset != TimeSpan.Zero ||
                permission.ConsumedAtUtc < permission.GrantedAtUtc || permission.Revoked && permission.ConsumedAtUtc is null)
                throw new InvalidDataException("Invalid access permission.");
    }
}
