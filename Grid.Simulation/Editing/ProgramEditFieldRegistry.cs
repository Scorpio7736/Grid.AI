using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using Grid.Core;
namespace Grid.Simulation.Editing;

/// <summary>Trusted typed definition; staging returns a new record and never mutates the world.</summary>
public sealed record ProgramEditFieldDescriptor(string Key, string Label, string Description,
    string ValueKind, string Constraints,
    Func<string, ProgramEditValue> Parse, Func<ProgramEditValue, ProgramEditValue> Normalize,
    Func<GridProgramValues, ProgramEditValue> Read,
    Func<GridProgramValues, ProgramEditValue, GridProgramValues> Stage)
{
    public int Version => 1;
    public string RequiredCapability => "programs.edit";
    public string Behavior => "One-time assignment";
}
public sealed class ProgramEditFieldRegistry
{
    private readonly Dictionary<string, ProgramEditFieldDescriptor> fields = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<ProgramEditFieldDescriptor> Fields { get; }
    public ProgramEditFieldRegistry(IEnumerable<ProgramEditFieldDescriptor> descriptors)
    {
        foreach (var descriptor in descriptors)
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            if (string.IsNullOrWhiteSpace(descriptor.Key) || descriptor.Key.Any(char.IsWhiteSpace) ||
                descriptor.Parse is null || descriptor.Normalize is null || descriptor.Read is null || descriptor.Stage is null)
                throw new ArgumentException("A field needs a key and typed parsing/validation/staging functions.");
            if (!fields.TryAdd(descriptor.Key, descriptor)) throw new ArgumentException($"Duplicate field: {descriptor.Key}.");
        }
        Fields = Array.AsReadOnly(fields.Values.OrderBy(f => f.Key).ToArray());
    }
    internal bool TryGet(string key, out ProgramEditFieldDescriptor descriptor) => fields.TryGetValue(key, out descriptor!);
    public ProgramFieldParseResult Parse(string key, string text)
    {
        if (key is null || !TryGet(key, out var descriptor)) return new(null, new(key ?? "", "Unknown editable field."));
        try { return new(new(descriptor.Key, descriptor.Normalize(descriptor.Parse(text))), null); }
        catch (Exception e) when (e is ArgumentException or FormatException or OverflowException)
        { return new(null, new(descriptor.Key, e.Message)); }
    }
    public static ProgramEditFieldRegistry CreateDefault() => new(new[]
    {
        Text("name", "Name", 80, v => v.Name, (v, s) => v with { Name = s }),
        Text("occupation", "Occupation", 120, v => v.Occupation, (v, s) => v with { Occupation = s }),
        new ProgramEditFieldDescriptor("energy", "Energy", "Current fictional energy.", "float", "Finite 0–100 inclusive.",
            ParseEnergy, value => value is EnergyValue e && float.IsFinite(e.Value) && e.Value >= 0 && e.Value <= 100
                ? e : throw new ArgumentException("Energy must be finite and within 0–100."),
            v => new EnergyValue(v.Energy), (v, value) => v with { Energy = ((EnergyValue)value).Value }),
        new ProgramEditFieldDescriptor("status", "Status", "Simulation state; does not control host services.", "status",
            "Idle, Working, Moving, Recharging, Offline.",
            text => Enum.GetNames<GridProgramStatus>().FirstOrDefault(n => n.Equals(text?.Trim(), StringComparison.OrdinalIgnoreCase))
                is { } name ? new StatusValue(Enum.Parse<GridProgramStatus>(name)) : throw new ArgumentException("Use a named program status."),
            value => value is StatusValue s && Enum.IsDefined(s.Value) ? s : throw new ArgumentException("Invalid status."),
            v => new StatusValue(v.Status), (v, value) => v with { Status = ((StatusValue)value).Value }),
        new ProgramEditFieldDescriptor("district", "District", "Immediate world-managed relocation.", "guid",
            "Existing, non-empty district ID in this world.",
            text => Guid.TryParse(text, out var id) ? new DistrictValue(id) : throw new ArgumentException("Invalid district ID."),
            value => value is DistrictValue d && d.Value != Guid.Empty ? d : throw new ArgumentException("A non-empty district ID is required."),
            v => new DistrictValue(v.DistrictId), (v, value) => v with { DistrictId = ((DistrictValue)value).Value })
    });
    private static ProgramEditFieldDescriptor Text(string key, string label, int limit,
        Func<GridProgramValues, string> read, Func<GridProgramValues, string, GridProgramValues> stage) =>
        new(key, label, "Display text; grants no permissions.", "string", $"1–{limit} UTF-16 units after trimming; no controls.",
            text => new TextValue(text), value =>
            {
                if (value is not TextValue t || t.Value is null || t.Value.Any(char.IsControl))
                    throw new ArgumentException("Text must not be null or contain control characters.");
                var normalized = t.Value.Trim();
                if (normalized.Length == 0 || normalized.Length > limit)
                    throw new ArgumentException($"Text must contain 1–{limit} UTF-16 units after trimming.");
                return new TextValue(normalized);
            }, v => new TextValue(read(v)), (v, value) => stage(v, ((TextValue)value).Value));
    private static ProgramEditValue ParseEnergy(string text)
    {
        text = text?.Trim() ?? "";
        if (text.Length > 1024 || !Regex.IsMatch(text, @"\A[+-]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)\z"))
            throw new ArgumentException("Use a decimal with a dot separator (maximum 1024 characters).");
        var negative = text.StartsWith('-');
        var unsigned = text.TrimStart('+', '-');
        var dot = unsigned.IndexOf('.');
        var scale = dot < 0 ? 0 : unsigned.Length - dot - 1;
        var numerator = BigInteger.Parse(unsigned.Replace(".", ""), CultureInfo.InvariantCulture);
        if ((negative && numerator != 0) || numerator > 100 * BigInteger.Pow(10, scale))
            throw new ArgumentException("Energy must be within 0–100.");
        return new EnergyValue(float.Parse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture));
    }
}
