using Grid.Core;
using Grid.Simulation.Editing;
namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    private readonly IProgramEditAuthorizer editAuthorizer;
    private readonly ProgramEditFieldRegistry editFields;
    private readonly object previewIssuer = new();
    public Guid WorldInstanceId => world.InstanceId;
    public ProgramEditFieldRegistry EditableFields => editFields;

    public ProgramEditResult PrepareEdit(EditSessionReference session, ProgramEditRequest request,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return new(ProgramEditOutcome.Cancelled);
        if (session is null) return new(ProgramEditOutcome.Forbidden);
        // Lock order: live authorization lease, then world/engine lock.
        using var lease = editAuthorizer.Acquire(session, world.InstanceId);
        if (!IsAuthorized(lease, session)) return new(ProgramEditOutcome.Forbidden);
        lock (gate)
        {
            if (cancellationToken.IsCancellationRequested) return new(ProgramEditOutcome.Cancelled);
            if (request is null || request.Fields is null) return Invalid("", "A target and fields are required.");
            if (!world.Programs.TryGetValue(request.ProgramId, out var program)) return new(ProgramEditOutcome.NotFound);
            var staged = Stage(program.Values, request.Fields.ToArray());
            if (staged.Errors.Count != 0) return new(ProgramEditOutcome.ValidationFailed, Errors: staged.Errors);
            if (cancellationToken.IsCancellationRequested) return new(ProgramEditOutcome.Cancelled);
            var preview = new ProgramEditPreview(previewIssuer, Guid.NewGuid(), lease!.Identity,
                program.Id, program.Name, program.Revision, staged.Changes);
            return new(ProgramEditOutcome.Prepared, Preview: preview);
        }
    }

    public ProgramEditResult ApplyEdit(EditSessionReference session, ProgramEditPreview preview,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return new(ProgramEditOutcome.Cancelled);
        if (session is null) return new(ProgramEditOutcome.Forbidden);
        using var lease = editAuthorizer.Acquire(session, world.InstanceId);
        if (!IsAuthorized(lease, session)) return new(ProgramEditOutcome.Forbidden);
        lock (gate)
        {
            if (cancellationToken.IsCancellationRequested) return new(ProgramEditOutcome.Cancelled);
            if (preview is null || !ReferenceEquals(preview.Issuer, previewIssuer) || preview.Consumed ||
                preview.Identity.OperatorId != lease!.Identity.OperatorId ||
                preview.Identity.SessionId != lease.Identity.SessionId ||
                preview.Identity.Generation != lease.Identity.Generation ||
                preview.Identity.WorldInstanceId != world.InstanceId)
                return new(ProgramEditOutcome.Conflict);
            if (!world.Programs.TryGetValue(preview.ProgramId, out var program)) return new(ProgramEditOutcome.NotFound);
            if (program.Revision != preview.ExpectedRevision) return new(ProgramEditOutcome.Conflict);
            var staged = Stage(program.Values, preview.Changes.Select(c => new ProgramFieldEdit(c.Field, c.After)).ToArray());
            if (staged.Errors.Count != 0) return new(ProgramEditOutcome.ValidationFailed, Errors: staged.Errors);
            var changed = staged.Values != program.Values;
            if (changed && program.Revision == long.MaxValue) return Invalid("", "Program revision is exhausted.");
            var revision = changed ? program.Revision + 1 : program.Revision;
            var receipt = new ProgramEditReceipt(preview.RequestId, lease.Identity, program.Id,
                program.Revision, revision, clock.TickCount, clock.ElapsedSimulationTime,
                timeProvider.GetUtcNow(), staged.Changes);
            // Prepare presentation data before commit as well: formatting cannot fail after mutation.
            var eventDetail = changed ? string.Join("; ", staged.Changes.Where(c => c.IsChanged).Select(c =>
                $"{c.Field}: {EventValue(c.Before)} -> {EventValue(c.After)}")) : "";
            if (cancellationToken.IsCancellationRequested) return new(ProgramEditOutcome.Cancelled);
            world.CommitProgram(program, staged.Values);
            preview.Consumed = true;
            if (changed) RecordEvent("Edit", program.Id, program.Name, eventDetail,
                System.Text.Json.JsonSerializer.Serialize(new { receipt.RequestId, receipt.Identity,
                    beforeRevision = program.Revision - 1, afterRevision = program.Revision,
                    changes = staged.Changes.Where(c => c.IsChanged).Select(c => new { c.Field, before = EventValue(c.Before), after = EventValue(c.After) }) }));
            return new(changed ? ProgramEditOutcome.Applied : ProgramEditOutcome.NoChange, Receipt: receipt);
        }
    }

    private bool IsAuthorized(IProgramEditLease? lease, EditSessionReference session) =>
        lease?.Identity is { } identity && identity.OperatorId != Guid.Empty &&
        identity.SessionId == session.SessionId && identity.SessionId != Guid.Empty &&
        identity.Generation == session.Generation && identity.Generation >= 0 &&
        identity.WorldInstanceId == world.InstanceId && identity.IsMasterControl && identity.AgentId is null;

    private static string EventValue(ProgramEditValue value) => value is TextValue or EnergyValue or StatusValue or DistrictValue
        ? ProgramEditFormatting.Format(value) : value.ToString() ?? "";

    private static ProgramEditResult Invalid(string field, string message) =>
        new(ProgramEditOutcome.ValidationFailed, Errors: Array.AsReadOnly(new[] { new ProgramEditError(field, message) }));

    private (GridProgramValues Values, IReadOnlyList<ProgramFieldChange> Changes, IReadOnlyList<ProgramEditError> Errors)
        Stage(GridProgramValues original, IReadOnlyList<ProgramFieldEdit> edits)
    {
        var state = original;
        var errors = new List<ProgramEditError>();
        var changes = new List<ProgramFieldChange>();
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (edits.Count == 0) errors.Add(new("", "At least one field is required."));
        foreach (var edit in edits)
        {
            if (edit is null || edit.Field is null || !editFields.TryGet(edit.Field, out var field))
            {
                errors.Add(new(edit?.Field ?? "", "Unknown editable field."));
                continue;
            }
            if (!keys.Add(field.Key)) { errors.Add(new(field.Key, "Duplicate field.")); continue; }
            try
            {
                var normalized = field.Normalize(edit.Value);
                state = field.Stage(state, normalized);
                changes.Add(new(field.Key, field.Read(original), normalized));
            }
            catch (Exception e) when (e is ArgumentException or FormatException or OverflowException)
            { errors.Add(new(field.Key, e.Message)); }
        }
        if (!float.IsFinite(state.Energy) || state.Energy < 0 || state.Energy > 100)
            errors.Add(new("energy", "Energy must be finite and within 0–100."));
        if (!Enum.IsDefined(state.Status)) errors.Add(new("status", "Invalid status."));
        if (!world.Districts.ContainsKey(state.DistrictId))
            errors.Add(new("district", "Destination district does not exist in this world."));
        if (state.Name != original.Name) ValidateText("name", state.Name, 80, errors);
        if (state.Occupation != original.Occupation) ValidateText("occupation", state.Occupation, 120, errors);
        return (state, changes.AsReadOnly(), errors.AsReadOnly());
    }
    private static void ValidateText(string key, string text, int limit, List<ProgramEditError> errors)
    {
        if (text is null || text.Length == 0 || text.Length > limit || text.Any(char.IsControl) || text != text.Trim())
            errors.Add(new(key, $"Text must be trimmed, contain 1–{limit} UTF-16 units, and contain no controls."));
    }
}
