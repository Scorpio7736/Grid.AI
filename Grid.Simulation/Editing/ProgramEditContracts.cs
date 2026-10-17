using System.Globalization;
using Grid.Core;
namespace Grid.Simulation.Editing;

public abstract record ProgramEditValue;
public sealed record TextValue(string Value) : ProgramEditValue;
public sealed record EnergyValue(float Value) : ProgramEditValue;
public sealed record StatusValue(GridProgramStatus Value) : ProgramEditValue;
public sealed record DistrictValue(Guid Value) : ProgramEditValue;
public sealed record ProgramFieldEdit(string Field, ProgramEditValue Value);
public sealed record ProgramEditRequest(Guid ProgramId, IReadOnlyList<ProgramFieldEdit> Fields);
public sealed record ProgramFieldChange(string Field, ProgramEditValue Before, ProgramEditValue After)
{
    public bool IsChanged => Before != After;
}
public enum ProgramEditOutcome { Prepared, Applied, NoChange, ValidationFailed, NotFound, Forbidden, Conflict, Cancelled }
public sealed record ProgramEditError(string Field, string Message);
public sealed record ProgramEditReceipt(Guid RequestId, ProgramEditIdentity Identity,
    Guid ProgramId, long BeforeRevision, long Revision, long Tick, TimeSpan SimulationTime,
    DateTimeOffset OccurredAtUtc, IReadOnlyList<ProgramFieldChange> Changes);

public sealed class ProgramEditPreview
{
    internal ProgramEditPreview(object issuer, Guid requestId, ProgramEditIdentity identity,
        Guid programId, string name, long revision, IReadOnlyList<ProgramFieldChange> changes)
    {
        Issuer = issuer; RequestId = requestId; Identity = identity;
        ProgramId = programId; ProgramName = name; ExpectedRevision = revision; Changes = changes;
    }
    internal object Issuer { get; }
    internal bool Consumed { get; set; }
    public Guid RequestId { get; }
    public ProgramEditIdentity Identity { get; }
    public Guid ProgramId { get; }
    public string ProgramName { get; }
    public long ExpectedRevision { get; }
    public IReadOnlyList<ProgramFieldChange> Changes { get; }
    public string Behavior => "One-time assignment; later simulation updates may change these values.";
}
public sealed record ProgramEditResult(ProgramEditOutcome Outcome,
    ProgramEditPreview? Preview = null, ProgramEditReceipt? Receipt = null,
    IReadOnlyList<ProgramEditError>? Errors = null);
public sealed record ProgramFieldParseResult(ProgramFieldEdit? Edit, ProgramEditError? Error)
{
    public bool Success => Edit is not null;
}
public static class ProgramEditFormatting
{
    public static string Format(ProgramEditValue value) => value switch
    {
        TextValue text => text.Value,
        EnergyValue energy => FormatEnergy(energy.Value),
        StatusValue status => status.Value.ToString(),
        DistrictValue district => district.Value.ToString(),
        _ => throw new ArgumentException("Unsupported field value.")
    };

    private static string FormatEnergy(float value)
    {
        var text = value.ToString("R", CultureInfo.InvariantCulture);
        var exponentIndex = text.IndexOf('E');
        if (exponentIndex < 0) return text;
        var mantissa = text[..exponentIndex];
        var exponent = int.Parse(text[(exponentIndex + 1)..], CultureInfo.InvariantCulture);
        var sign = mantissa.StartsWith('-') ? "-" : "";
        mantissa = mantissa.TrimStart('-');
        var dot = mantissa.IndexOf('.');
        var decimalPosition = (dot < 0 ? mantissa.Length : dot) + exponent;
        var digits = mantissa.Replace(".", "");
        // Expand scientific notation so the preview can be pasted into the decimal-only parser.
        if (decimalPosition <= 0) return sign + "0." + new string('0', -decimalPosition) + digits;
        if (decimalPosition >= digits.Length) return sign + digits + new string('0', decimalPosition - digits.Length);
        return sign + digits.Insert(decimalPosition, ".");
    }
}
