using Grid.Agent;
using Grid.Simulation.Editing;

namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    private IDeviceIntegration? deviceIntegration;
    private sealed record DeviceGrant(DeviceReview Review, EditSessionReference Session, Guid Operator, string Device, long Activity);
    private readonly Dictionary<Guid, DeviceGrant> deviceGrants = new();
    private readonly HashSet<Guid> deviceExecuting = new();
    public void ConfigureDeviceIntegration(IDeviceIntegration provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        lock (gate)
        {
            if (deviceIntegration is not null) throw new InvalidOperationException("Device provider is already configured.");
            deviceIntegration = provider;
        }
    }
    private IProgramEditLease DeviceLease(EditSessionReference session)
    {
        var lease = TaskLease(session, true);
        if (!lease.Identity.IsOriginalHost)
        { lease.Dispose(); throw new UnauthorizedAccessException("Device access requires the original Master Control terminal."); }
        return lease;
    }
    private IDeviceIntegration Devices => deviceIntegration ?? throw new InvalidOperationException("Device integration unavailable on this host.");
    public DeviceDiscovery DiscoverDevices(EditSessionReference session, bool windowsOnly)
    {
        using var lease = DeviceLease(session);
        lock (gate)
        {
            var result = Devices.Discover(windowsOnly);
            RecordEvent("DeviceDiscovery", null, null, "Read-only device metadata observed; no action or title grant.");
            return result;
        }
    }
    public DeviceReview SelectDevice(EditSessionReference session, DeviceRequest request)
    {
        request.Validate();
        using var lease = DeviceLease(session);
        lock (gate)
        {
            RequireAgentActivity();
            foreach (var old in deviceGrants.Where(p => p.Value.Review.ExpiresUtc <= timeProvider.GetUtcNow() ||
                p.Value.Session.SessionId == session.SessionId && p.Value.Session.Generation != session.Generation).Select(p => p.Key).ToArray())
                deviceGrants.Remove(old);
            if (deviceGrants.Count >= 32) throw new InvalidOperationException("Device review capacity reached; revoke an old selection.");
            if (request.Operation == DeviceOperation.CreateFolder)
                WithFileDestination(session, Path.GetDirectoryName(request.Target) ?? "", (_, _) => true);
            var target = Devices.Resolve(request);
            var review = new DeviceReview(Guid.NewGuid(), request, target, timeProvider.GetUtcNow().AddMinutes(2), false);
            deviceGrants.Add(review.Id, new(review, session, lease.Identity.OperatorId, Devices.DeviceId, ActivityGeneration));
            RecordEvent("DeviceSelected", review.Id, null, request.Operation + ": selected target; approval and execution still required.");
            return review;
        }
    }
    private DeviceGrant CurrentDevice(EditSessionReference session, ProgramEditIdentity identity, Guid id)
    {
        RequireAgentActivity();
        if (!deviceGrants.TryGetValue(id, out var grant) || grant.Session != session ||
            grant.Operator != identity.OperatorId || grant.Activity != ActivityGeneration || grant.Device != Devices.DeviceId || grant.Review.ExpiresUtc <= timeProvider.GetUtcNow())
            throw new UnauthorizedAccessException("Device selection expired, revoked or belongs to another session/device. Select again.");
        grant.Review.Request.Validate();
        var current = Devices.Resolve(grant.Review.Request);
        if (current != grant.Review.Target) throw new InvalidOperationException("Device target changed. Revoke and select it again.");
        return grant;
    }
    public DeviceReview ApproveDevice(EditSessionReference session, Guid id)
    {
        using var lease = DeviceLease(session);
        lock (gate)
        {
            var grant = CurrentDevice(session, lease.Identity, id);
            if (deviceExecuting.Contains(id)) throw new InvalidOperationException("This attempt is already claimed.");
            var review = grant.Review with { Approved = true };
            deviceGrants[id] = grant with { Review = review };
            RecordEvent("DeviceApproved", id, null, review.Request.Operation + ": exact selection approved for one attempt.");
            return review;
        }
    }
    public void RevokeDevice(EditSessionReference session, Guid id)
    {
        using var lease = DeviceLease(session);
        lock (gate)
        {
            if (!deviceGrants.TryGetValue(id, out var grant) || grant.Session != session)
                throw new UnauthorizedAccessException("No device selection for this session.");
            deviceGrants.Remove(id);
            RecordEvent("DeviceRevoked", id, null, "Selected device grant and approval revoked.");
        }
    }
    public async Task<DeviceReceipt> ExecuteDeviceAsync(EditSessionReference session, Guid id,
        Func<CancellationToken, Task> save, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(save);
        using (var lease = DeviceLease(session))
        lock (gate)
        {
            var grant = CurrentDevice(session, lease.Identity, id);
            if (!grant.Review.Approved) throw new InvalidOperationException("Review and approve this selection before execution.");
            if (!deviceExecuting.Add(id)) throw new InvalidOperationException("This device attempt is already claimed.");
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(grant))));
            RecordEvent("DeviceClaimed", id, null, grant.Review.Request.Operation + ": one attempt claimed.",
                System.Text.Json.JsonSerializer.Serialize(new { requestHash = hash, operatorId = lease.Identity.OperatorId,
                    sessionId = session.SessionId, generation = session.Generation }));
        }
        try
        {
            // Same persistence fence as task dispatch: no OS invocation if saving the claim fails.
            await save(token);
            DeviceReceipt receipt;
            using (var lease = DeviceLease(session))
            lock (gate)
            {
                var grant = CurrentDevice(session, lease.Identity, id);
                token.ThrowIfCancellationRequested();
                deviceGrants.Remove(id); // Never replay a completed or uncertain attempt.
                var status = "Completed"; string detail;
                try { detail = Devices.Execute(grant.Review.Request, grant.Review.Target); }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or System.ComponentModel.Win32Exception or NotSupportedException)
                { status = "Unavailable"; detail = error.Message + " No automatic retry; inspect the target before a fresh selection."; }
                detail = new string(detail.Where(c => !char.IsControl(c)).Take(1024).ToArray());
                RecordEvent("Device" + status, id, null, grant.Review.Request.Operation + ": " + status + "; target content excluded from audit.");
                receipt = new(id, grant.Review.Request.Operation, status, detail, timeProvider.GetUtcNow(), "Original-host device provider");
            }
            try { await save(CancellationToken.None); }
            catch (Exception error) when (error is IOException or System.Data.Common.DbException or InvalidOperationException)
            { return receipt with { Status = "OutcomeNotSaved", Detail = receipt.Status + ": " + receipt.Detail + " Result persistence failed; do not retry blindly." }; }
            return receipt;
        }
        finally
        {
            lock (gate) { deviceGrants.Remove(id); deviceExecuting.Remove(id); }
        }
    }
}
