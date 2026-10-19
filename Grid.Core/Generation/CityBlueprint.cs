using System.Text.Json;

namespace Grid.Core.Generation;

public sealed record CityLot(int X, int Z, int Width, int Depth, string Use);
public sealed record CityDistrict(Guid Id, HardwareKind Category, string Name, int X, int Z,
    int Width, int Depth, IReadOnlyList<string> ComponentKeys, IReadOnlyList<CityLot> Lots);
public sealed record CityRoad(Guid From, Guid To, int Width);
public sealed record CityPlan(int Seed, string GeneratorVersion, string ProfileFingerprint, HardwareProfile Profile,
    IReadOnlyList<CityDistrict> Districts, IReadOnlyList<CityRoad> Roads);

/// <summary>Immutable saved semantic layout. Read() returns a detached document, never mutable world data.</summary>
public sealed class CityBlueprint
{
    public string Json { get; }
    private CityBlueprint(string json) => Json = json;
    public CityPlan Read() => JsonSerializer.Deserialize<CityPlan>(Json) ?? throw new InvalidDataException("Missing city plan.");
    public static CityBlueprint Create(CityPlan plan) => Parse(JsonSerializer.Serialize(plan));
    public static CityBlueprint Parse(string json)
    {
        if (string.IsNullOrEmpty(json) || json.Length > 512000) throw new InvalidDataException("City document exceeds its budget.");
        var city = new CityBlueprint(json);
        CityPlan p;
        try { p = city.Read(); }
        catch (JsonException ex) { throw new InvalidDataException("Invalid city JSON.", ex); }
        if (p.GeneratorVersion != HardwareCityGenerator.Version || p.Profile is null || p.Districts is null || p.Roads is null ||
            p.Districts.Any(d => d is null || d.Lots is null || d.ComponentKeys is null) || p.Roads.Any(r => r is null) ||
            p.Districts.Count != 6 || p.Roads.Count is < 5 or > 7 ||
            p.ProfileFingerprint != p.Profile.Fingerprint() || p.Districts.Select(d => d.Id).Distinct().Count() != 6 ||
            p.Districts.Select(d => d.Category).Distinct().Count() != 6)
            throw new InvalidDataException("Unsupported or inconsistent city provenance/layout.");
        var profile = HardwareProfile.Normalize(p.Profile);
        if (JsonSerializer.Serialize(profile) != JsonSerializer.Serialize(p.Profile))
            throw new InvalidDataException("Saved hardware profile is not canonical.");
        foreach (var d in p.Districts)
        {
            if (d.Id == Guid.Empty || !Enum.IsDefined(d.Category) || string.IsNullOrWhiteSpace(d.Name) || d.Name.Length > 80 || d.Name.Any(char.IsControl) ||
                d.X is < 0 or > 500 || d.Z is < 0 or > 500 || d.Width != 112 || d.Depth != 112 || d.Lots.Count is < 4 or > 16)
                throw new InvalidDataException("Invalid district geometry.");
            var expectedKeys = profile.Categories.Single(c => c.Kind == d.Category).Components.Select((_, i) => $"{d.Category}:{i}");
            if (!d.ComponentKeys.SequenceEqual(expectedKeys)) throw new InvalidDataException("Invalid component associations.");
            foreach (var lot in d.Lots)
                if (lot is null || lot.Width != 16 || lot.Depth != 16 || Math.Abs((long)lot.X - d.X) is not (12 or 36) || Math.Abs((long)lot.Z - d.Z) is not (12 or 36) ||
                    string.IsNullOrWhiteSpace(lot.Use) || lot.Use.Length > 80 || lot.Use.Any(char.IsControl))
                    throw new InvalidDataException("Invalid building lot or obstructed street.");
            if (d.Lots.Select(l => (l.X, l.Z)).Distinct().Count() != d.Lots.Count)
                throw new InvalidDataException("Overlapping lots.");
            foreach (var other in p.Districts.Where(o => o.Id != d.Id))
                if (Math.Abs(other.X - d.X) < 112 && Math.Abs(other.Z - d.Z) < 112)
                    throw new InvalidDataException("Overlapping districts.");
        }
        foreach (var road in p.Roads)
        {
            var from = p.Districts.SingleOrDefault(d => d.Id == road.From);
            var to = p.Districts.SingleOrDefault(d => d.Id == road.To);
            if (from is null || to is null || from == to || road.Width != 8 || from.X != to.X && from.Z != to.Z)
                throw new InvalidDataException("Invalid road connection.");
        }
        if (p.Roads.Select(r => string.Join(":", new[] { r.From, r.To }.Order())).Distinct().Count() != p.Roads.Count)
            throw new InvalidDataException("Duplicate streets.");
        var reached = new HashSet<Guid> { p.Districts[0].Id };
        for (var i = 0; i < 6; i++) foreach (var road in p.Roads)
        { if (reached.Contains(road.From)) reached.Add(road.To); if (reached.Contains(road.To)) reached.Add(road.From); }
        if (reached.Count != 6) throw new InvalidDataException("City must be connected.");
        return city;
    }
}
