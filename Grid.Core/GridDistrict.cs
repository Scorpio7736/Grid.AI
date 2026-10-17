
namespace Grid.Core;

public sealed class GridDistrict
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<Guid> ProgramIds { get; } = new();

    public Dictionary<string, float> Resources { get; } = new();
}
