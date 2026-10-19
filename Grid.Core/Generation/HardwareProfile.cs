using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Grid.Core.Generation;

public enum HardwareKind { Motherboard, Cpu, Gpu, Ram, Storage, Network }
public enum DiscoveryStatus { Available, NotDetected, Unavailable, Unsupported }
public sealed record HardwareComponent(string Label, long? CapacityBytes = null, int? Units = null, bool? IsVirtual = null);
public sealed record HardwareCategory(HardwareKind Kind, DiscoveryStatus Status, string Source,
    string Note, IReadOnlyList<HardwareComponent> Components, bool Truncated = false);
public sealed record HardwareProfile(DateTimeOffset CollectedAtUtc, IReadOnlyList<HardwareCategory> Categories)
{
    public static HardwareProfile Normalize(HardwareProfile input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.CollectedAtUtc.Offset != TimeSpan.Zero || input.Categories is null || input.Categories.Count > 6 ||
            input.Categories.Any(c => c is null || c.Components is null || c.Components.Any(x => x is null) || !Enum.IsDefined(c.Kind) || !Enum.IsDefined(c.Status)) ||
            input.Categories.Select(c => c.Kind).Distinct().Count() != input.Categories.Count)
            throw new InvalidDataException("Invalid hardware profile categories or timestamp.");
        var categories = Enum.GetValues<HardwareKind>().Select(kind =>
        {
            var category = input.Categories.SingleOrDefault(c => c.Kind == kind) ??
                new(kind, DiscoveryStatus.Unavailable, "Not supplied", "Category unavailable.", []);
            var components = category.Components.Select(c => new HardwareComponent(Clean(c.Label),
                c.CapacityBytes > 0 ? c.CapacityBytes : null, c.Units is > 0 and <= 65536 ? c.Units : null, c.IsVirtual))
                .OrderBy(c => c.Label, StringComparer.Ordinal).ThenBy(c => c.CapacityBytes).ThenBy(c => c.Units).ThenBy(c => c.IsVirtual).ToArray();
            if ((category.Status == DiscoveryStatus.Available) != (components.Length > 0))
                throw new InvalidDataException("Hardware availability disagrees with component inventory.");
            return category with { Source = Clean(category.Source), Note = Clean(category.Note),
                Components = Array.AsReadOnly(components.Take(32).ToArray()), Truncated = category.Truncated || components.Length > 32 };
        }).ToArray();
        return new(input.CollectedAtUtc, Array.AsReadOnly(categories));
    }

    public string Fingerprint() => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
        Normalize(this).Categories.Select(c => new { c.Kind, c.Status, c.Components, c.Truncated })))));

    public static HardwareProfile Unavailable(DiscoveryStatus status, string reason) => new(DateTimeOffset.UtcNow,
        Enum.GetValues<HardwareKind>().Select(k => new HardwareCategory(k, status, "Windows CIM", reason, [])).ToArray());

    private static string Clean(string? text)
    {
        var safe = new string((text ?? "").Normalize(NormalizationForm.FormKC).Select(c => char.IsControl(c) ? ' ' : c).ToArray());
        safe = string.Join(" ", safe.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return safe.Length > 256 ? safe[..256] : safe;
    }
}

public interface IHardwareDiscovery
{
    Task<HardwareProfile> DiscoverAsync(CancellationToken cancellationToken);
}
