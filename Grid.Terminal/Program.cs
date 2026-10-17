
using Grid.Core;

Console.WriteLine("PROJECT GRID — GENESIS");
Console.WriteLine("----------------------");

var districts = new List<GridDistrict>
{
    new()
    {
        Name = "The Core",
        Description = "Central reactor and administration"
    },
    new()
    {
        Name = "Processing Sector",
        Description = "Manufacturing and research"
    },
    new()
    {
        Name = "Memory Banks",
        Description = "Research and temporary resource storage"
    },
    new()
    {
        Name = "Archive Sector",
        Description = "History and long-term storage"
    },
    new()
    {
        Name = "Gateway",
        Description = "Communications, trade, and transit"
    }
};

Console.WriteLine($"Districts: {districts.Count}");
Console.WriteLine();

foreach (var district in districts)
{
    Console.WriteLine($"[{district.Name}]");
    Console.WriteLine($" ID: {district.Id}");
    Console.WriteLine($" Purpose: {district.Description}");
    Console.WriteLine($" Population: {district.ProgramIds.Count}");
    Console.WriteLine();
}

bool uniqueIds =
    districts.Select(d => d.Id).Distinct().Count()
    == districts.Count;

Console.WriteLine($"Unique district IDs: {uniqueIds}");
