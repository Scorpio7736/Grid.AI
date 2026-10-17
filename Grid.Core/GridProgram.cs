namespace Grid.Core;

public enum GridProgramStatus { Idle, Working, Moving, Recharging, Offline }

public sealed record GridProgramValues(string Name, string Occupation, float Energy,
    GridProgramStatus Status, Guid DistrictId);

public sealed class GridProgram
{
    private string name = string.Empty;
    private string occupation = string.Empty;
    private float energy = 100f;
    private GridProgramStatus status = GridProgramStatus.Idle;
    public Guid Id { get; init; } = Guid.NewGuid();
    // Initialization is supported; world-owned values change through validated engine operations.
    public string Name { get => name; set { EnsureUnowned(); name = value; } }
    public string Occupation { get => occupation; set { EnsureUnowned(); occupation = value; } }
    public float Energy { get => energy; set { EnsureUnowned(); energy = value; } }
    public GridProgramStatus Status { get => status; set { EnsureUnowned(); status = value; } }
    public Guid CurrentDistrictId { get; internal set; }
    public long Revision { get; private set; }
    internal GridWorld? Owner { get; set; }
    internal GridProgramValues Values => new(name, occupation, energy, status, CurrentDistrictId);
    private void EnsureUnowned()
    {
        if (Owner is not null)
            throw new InvalidOperationException("World-owned programs must be changed through validated engine operations.");
    }
    internal void Commit(GridProgramValues values, long revision)
    {
        name = values.Name; occupation = values.Occupation; energy = values.Energy;
        status = values.Status; CurrentDistrictId = values.DistrictId; Revision = revision;
    }
}
