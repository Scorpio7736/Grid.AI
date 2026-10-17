
using Grid.Core;

var citizen1 = new GridProgram
{
    Name = "Citizen-001",
    Occupation = "Technician"
};

var citizen2 = new GridProgram
{
    Name = "Citizen-002",
    Occupation = "Archivist"
};

Console.WriteLine($"Citizen 1: {citizen1.Id}");
Console.WriteLine($"Citizen 2: {citizen2.Id}");

Console.WriteLine(
    $"Unique identities: {citizen1.Id != citizen2.Id}"
);

Console.WriteLine(
    $"Default energy: {citizen1.Energy}"
);

Console.WriteLine(
    $"Default status: {citizen1.Status}"
);
