
namespace Grid.Core;

public enum GridProgramStatus
{
    Idle,
    Working,
    Moving,
    Recharging,
    Offline
}

public sealed class GridProgram
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Occupation { get; set; } = string.Empty;

    public float Energy { get; set; } = 100f;

    public Guid CurrentDistrictId { get; set; }

    public GridProgramStatus Status { get; set; }
        = GridProgramStatus.Idle;
}
