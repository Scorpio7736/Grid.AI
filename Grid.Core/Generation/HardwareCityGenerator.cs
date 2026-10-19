using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Grid.Core.Generation;

public static class HardwareCityGenerator
{
    public const string Version = "hardware-city-v1";
    public static GridWorld Create(HardwareProfile raw, int seed)
    {
        var profile = HardwareProfile.Normalize(raw);
        var fingerprint = profile.Fingerprint();
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes($"{Version}:{seed}:{fingerprint}"));
        // Fixed PRNG algorithm, not runtime-dependent Random implementation.
        uint state = BinaryPrimitives.ReadUInt32LittleEndian(digest) | 1;
        int Next(int bound) { state ^= state << 13; state ^= state >> 17; state ^= state << 5; return (int)(state % (uint)bound); }
        var ox = 60 + Next(25); var oz = 60 + Next(25);
        string[] names = ["The Core", "Processing Sector", "Visual Compute", "Memory Banks", "Archive Sector", "Gateway"];
        string[] uses = ["Administration", "Workshop", "Studio", "Research", "Archive", "Communications"];
        var districts = new List<CityDistrict>();
        foreach (var category in profile.Categories)
        {
            var index = (int)category.Kind;
            var id = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes($"{Version}:{seed}:{fingerprint}:{index}")).AsSpan(0, 16));
            var x = ox + index % 3 * 160; var z = oz + index / 3 * 160;
            var capacityGiB = category.Components.Sum(c => Math.Min((double)(c.CapacityBytes ?? 0) / 1073741824, 1048576));
            var units = category.Components.Sum(c => Math.Min(c.Units ?? 0, 64));
            var count = Math.Clamp(4 + Math.Min(category.Components.Count, 4) + (int)Math.Log2(1 + capacityGiB) + units / 4, 4, 16);
            var slots = (from dx in new[] { -36, -12, 12, 36 } from dz in new[] { -36, -12, 12, 36 } select (dx, dz)).ToArray();
            for (var i = slots.Length - 1; i > 0; i--) { var j = Next(i + 1); (slots[i], slots[j]) = (slots[j], slots[i]); }
            var lots = slots.Take(count).Select(s => new CityLot(x + s.dx, z + s.dz, 16, 16, uses[index])).ToArray();
            districts.Add(new(id, category.Kind, names[index], x, z, 112, 112,
                category.Components.Select((_, i) => $"{category.Kind}:{i}").ToArray(), lots));
        }
        var roads = new List<CityRoad>();
        foreach (var (from, to) in new[] { (0, 1), (1, 2), (2, 5), (5, 4), (4, 3), (3, 0) })
            roads.Add(new(districts[from].Id, districts[to].Id, 8));
        if ((digest[4] & 1) != 0) roads.Add(new(districts[1].Id, districts[4].Id, 8));
        var city = CityBlueprint.Create(new(seed, Version, fingerprint, profile, districts, roads));
        var world = new GridWorld(city: city);
        foreach (var district in districts)
            world.AddDistrict(new GridDistrict { Id = district.Id, Name = district.Name,
                Description = $"Hardware-informed {district.Category} district; {district.Lots.Count} semantic building lots." });
        return world;
    }
}
