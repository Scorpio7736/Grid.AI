using Grid.Core.Generation;
using Xunit;

namespace Grid.Core.Tests;

public sealed class HardwareCityTests
{
    private static HardwareProfile Profile() => new(DateTimeOffset.Parse("2026-10-19T00:00:00Z"),
        Enum.GetValues<HardwareKind>().Select(k => new HardwareCategory(k, DiscoveryStatus.Available, "Fixture", "",
            [new HardwareComponent(k + " B", 16L * 1073741824, 8), new HardwareComponent(k + " A", 8L * 1073741824, 4)])).ToArray());

    [Fact]
    public void SameNormalizedProfileSeedAndVersionGiveIdenticalCityButDistinctWorldIdentities()
    {
        var input = Profile();
        var reordered = input with { Categories = input.Categories.Reverse().Select(c => c with { Components = c.Components.Reverse().ToArray() }).ToArray() };
        var first = HardwareCityGenerator.Create(input, 42); var second = HardwareCityGenerator.Create(reordered, 42);
        Assert.NotEqual(first.Id, second.Id); Assert.Equal(first.City!.Json, second.City!.Json);
        Assert.Equal(6, first.Districts.Count); Assert.Empty(first.Programs);
        Assert.InRange(first.City.Read().Districts.Sum(d => d.Lots.Count), 24, 96);
        Assert.All(first.City.Read().Districts, d => Assert.Equal(2, d.ComponentKeys.Count));
    }

    [Fact]
    public void HardwareAndSeedChangeGeometryAndFingerprintExcludesObservationTime()
    {
        var input = Profile(); var original = HardwareCityGenerator.Create(input, 42).City!.Read();
        var changed = input with { Categories = input.Categories.Select(c => c.Kind == HardwareKind.Ram
            ? c with { Components = [new HardwareComponent("Small RAM", 1073741824)] } : c).ToArray() };
        var other = HardwareCityGenerator.Create(changed, 42).City!.Read();
        Assert.NotEqual(original.ProfileFingerprint, other.ProfileFingerprint);
        Assert.NotEqual(original.Districts.Single(d => d.Category == HardwareKind.Ram).Lots.Count,
            other.Districts.Single(d => d.Category == HardwareKind.Ram).Lots.Count);
        Assert.NotEqual(HardwareCityGenerator.Create(input, 43).City!.Json, HardwareCityGenerator.Create(input, 42).City!.Json);
        Assert.Equal(input.Fingerprint(), (input with { CollectedAtUtc = input.CollectedAtUtc.AddYears(1) }).Fingerprint());
    }

    [Fact]
    public void MissingAndUnsupportedCategoriesProduceRecordedConnectedFallbacks()
    {
        var partial = new HardwareProfile(DateTimeOffset.UtcNow,
            [new(HardwareKind.Gpu, DiscoveryStatus.Unsupported, "Fixture", "Unsupported", [])]);
        var world = HardwareCityGenerator.Create(partial, 10); var plan = world.City!.Read();
        Assert.Equal(6, plan.Profile.Categories.Count);
        Assert.Equal(DiscoveryStatus.Unsupported, plan.Profile.Categories.Single(c => c.Kind == HardwareKind.Gpu).Status);
        Assert.All(plan.Districts, d => { Assert.Equal(4, d.Lots.Count); Assert.Empty(d.ComponentKeys); });
        Assert.NotNull(CityBlueprint.Parse(world.City.Json));
    }

    [Fact]
    public void MultipleAndRepeatedDevicesRemainBoundedAndDocumentTruncation()
    {
        var input = Profile() with { Categories = [new(HardwareKind.Storage, DiscoveryStatus.Available, "Fixture", "",
            Enumerable.Range(0, 100).Select(_ => new HardwareComponent("Same model", long.MaxValue, 65536)).ToArray())] };
        var city = HardwareCityGenerator.Create(input, 1).City!.Read();
        var storage = city.Profile.Categories.Single(c => c.Kind == HardwareKind.Storage);
        Assert.True(storage.Truncated); Assert.Equal(32, storage.Components.Count);
        Assert.Equal(32, city.Districts.Single(d => d.Category == HardwareKind.Storage).ComponentKeys.Distinct().Count());
        Assert.All(city.Districts, d => Assert.InRange(d.Lots.Count, 4, 16));
    }

    [Fact]
    public void BlueprintIsDetachedAndRejectsBrokenGeometryOrProvenance()
    {
        var blueprint = HardwareCityGenerator.Create(Profile(), 2).City!;
        var plan = blueprint.Read(); var before = blueprint.Json;
        ((IList<CityDistrict>)plan.Districts)[0] = plan.Districts[0] with { X = -1000 };
        Assert.Throws<InvalidDataException>(() => CityBlueprint.Create(plan));
        Assert.Equal(before, blueprint.Json);
        Assert.Throws<InvalidDataException>(() => CityBlueprint.Create(blueprint.Read() with { ProfileFingerprint = "changed" }));
        Assert.Throws<InvalidDataException>(() => CityBlueprint.Create(blueprint.Read() with { Roads = [] }));
        Assert.Throws<InvalidDataException>(() => CityBlueprint.Create(blueprint.Read() with { GeneratorVersion = "future" }));
        var malformed = blueprint.Read();
        ((IList<CityLot>)malformed.Districts[0].Lots)[0] = null!;
        Assert.Throws<InvalidDataException>(() => CityBlueprint.Create(malformed));
        malformed = blueprint.Read();
        ((IList<CityLot>)malformed.Districts[0].Lots)[0] = malformed.Districts[0].Lots[0] with { X = int.MinValue };
        Assert.Throws<InvalidDataException>(() => CityBlueprint.Create(malformed));
    }

    [Fact]
    public void InconsistentInventoryIsRejectedAndNamesAreNormalized()
    {
        var profile = Profile();
        Assert.Throws<InvalidDataException>(() => HardwareProfile.Normalize(profile with { Categories = [profile.Categories[0], profile.Categories[0]] }));
        Assert.Throws<InvalidDataException>(() => HardwareProfile.Normalize(profile with { Categories = [profile.Categories[0] with { Status = DiscoveryStatus.Unavailable }] }));
        var normal = HardwareProfile.Normalize(profile with { Categories = [profile.Categories[0] with { Components = [new("  Board\t A  ", -1, -1)] }] });
        var component = normal.Categories[0].Components.Single(); Assert.Equal("Board A", component.Label);
        Assert.Null(component.CapacityBytes); Assert.Null(component.Units);
    }
}
