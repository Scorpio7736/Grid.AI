namespace Grid.Simulation.Editing;

public sealed record EditSessionReference(Guid SessionId, long Generation);
public sealed record ProgramEditIdentity(Guid OperatorId, Guid SessionId, long Generation,
    Guid WorldInstanceId, bool IsMasterControl, Guid? AgentId, long PolicyVersion);

/// <summary>
/// Trusted providers verify live authentication, programs.edit, scope, expiry and current policy.
/// Return null on denial. Acquire authorization lock BEFORE engine lock and keep revocation/logout
/// blocked until the lease is disposed. Request-supplied role flags are not authorization.
/// </summary>
public interface IProgramEditAuthorizer
{
    IProgramEditLease? Acquire(EditSessionReference session, Guid worldInstanceId);
    // Separate task authorization permits scoped agents without granting program-edit privileges.
    IProgramEditLease? AcquireTask(EditSessionReference session, Guid worldInstanceId) => Acquire(session, worldInstanceId);
}
public interface IProgramEditLease : IDisposable
{
    ProgramEditIdentity Identity { get; }
}
internal sealed class DenyProgramEdits : IProgramEditAuthorizer
{
    public IProgramEditLease? Acquire(EditSessionReference session, Guid worldInstanceId) => null;
}
