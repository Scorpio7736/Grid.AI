namespace Grid.Core;

public static class GridWorldFactory
{
    /// <summary>Creates a fresh Genesis world. Equal seeds reproduce IDs and citizen placement.</summary>
    public static GridWorld CreateGenesis(int seed = 7736)
    {
        var random = new Random(seed);
        var usedIds = new HashSet<Guid>();
        Guid NextId()
        {
            var bytes = new byte[16];
            Guid id;
            do
            {
                random.NextBytes(bytes);
                id = new Guid(bytes);
            } while (id == Guid.Empty || !usedIds.Add(id));
            return id;
        }

        var world = new GridWorld();
        (string Name, string Description, string Occupation)[] definitions =
        [
            ("The Core", "Central reactor and administration", "Administrator"),
            ("Processing Sector", "Manufacturing and research", "Engineer"),
            ("Memory Banks", "Research and temporary resource storage", "Researcher"),
            ("Archive Sector", "History and long-term storage", "Archivist"),
            ("Gateway", "Communications, trade, and transit", "Communications technician")
        ];

        foreach (var definition in definitions)
        {
            var district = new GridDistrict
            {
                Id = NextId(),
                Name = definition.Name,
                Description = definition.Description
            };
            world.AddDistrict(district);
            for (var index = 0; index < 4; index++)
            {
                world.AddProgram(new GridProgram
                {
                    Id = NextId(),
                    Name = $"Citizen {world.Programs.Count + 1:000}",
                    Occupation = definition.Occupation
                }, district.Id);
            }
        }

        return world;
    }
}
