using Grid.Simulation.Editing;
using Status = Grid.Simulation.Tasks.TaskStatus;

namespace Grid.Simulation;

public sealed record MasterEmergencyRevocation(int QueuedApprovalsRevoked, int RunningTasksCancelled);

public sealed partial class GridSimulationEngine
{
    private IProgramEditLease MasterBreakGlassLease(EditSessionReference session)
    {
        var lease = editAuthorizer.Acquire(session, world.InstanceId);
        if (!IsAuthorized(lease, session) || lease!.Identity.AgentId is not null || !lease.Identity.IsOriginalHost)
        { lease?.Dispose(); throw new UnauthorizedAccessException("Original-host Master Control is required."); }
        return lease;
    }

    public void MasterBreakGlassFreeze(EditSessionReference session, string action)
    {
        using var lease = MasterBreakGlassLease(session);
        lock (gate)
        {
            Pause();
            RecordEvent("MasterBreakGlass", lease.Identity.OperatorId, null, action + "; GRID activity frozen.");
        }
    }

    public void MasterBreakGlassDisableRemoteAuthority(EditSessionReference session, string action)
    {
        using var lease = MasterBreakGlassLease(session);
        lock (gate)
        {
            activeChallengeSessions.Clear();
            activeChallengeClaims.Clear();
            pairedChallengeEndpoints.Clear();
            challengeAuthorityGeneration++;
            RecordEvent("MasterBreakGlass", lease.Identity.OperatorId, null, action + "; transient remote challenge authority revoked.");
        }
    }

    public MasterEmergencyRevocation MasterBreakGlassRevokeApprovals(EditSessionReference session)
    {
        using var lease = MasterBreakGlassLease(session);
        lock (gate)
        {
            var queued = 0; var running = 0;
            foreach (var task in tasks.Records.Where(task => !task.Archived && task.Status is Status.Queued or Status.Running).ToArray())
            {
                if (task.Status == Status.Queued)
                {
                    queued++;
                    ChangeTask(task with { Status = Status.AwaitingApproval, ApprovedBy = null }, "ApprovalRevoked", lease.Identity,
                        "Emergency session revocation removed prior dispatch approval.");
                }
                else
                {
                    running++;
                    ChangeTask(task with { Status = Status.Cancelled, ApprovedBy = null,
                        Result = "Emergency session revocation cancelled the running attempt; external effects may remain." },
                        "Cancelled", lease.Identity, "Emergency session revocation requested cancellation.");
                }
            }
            activeChallengeSessions.Clear(); activeChallengeClaims.Clear(); pairedChallengeEndpoints.Clear(); challengeAuthorityGeneration++;
            RecordEvent("MasterBreakGlass", lease.Identity.OperatorId, null,
                $"Sessions/approvals revoked; queued {queued}; running {running}.");
            return new(queued, running);
        }
    }

    public void AuditMasterBreakGlass(EditSessionReference session, string action)
    {
        using var lease = MasterBreakGlassLease(session);
        lock (gate) RecordEvent("MasterBreakGlass", lease.Identity.OperatorId, null, action);
    }
}
