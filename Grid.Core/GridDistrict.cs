namespace Grid.Core;

public sealed class GridDistrict
{
    private readonly List<Guid> programIds = new();

    public GridDistrict() => ProgramIds = programIds.AsReadOnly();

    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IReadOnlyList<Guid> ProgramIds { get; }
    public Dictionary<string, float> Resources { get; } = new();

    internal GridWorld? Owner { get; set; }
    internal void AddMember(Guid id) => programIds.Add(id);
    internal void RemoveMember(Guid id) => programIds.Remove(id);
}
