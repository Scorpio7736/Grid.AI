using System.Collections.ObjectModel;
namespace Grid.Core;

/// <summary>Owns membership. Engine operations and world mutations share the same lock.</summary>
public sealed class GridWorld
{
    private readonly Dictionary<Guid, GridProgram> programs = new();
    private readonly Dictionary<Guid, GridDistrict> districts = new();
    internal object SyncRoot { get; } = new();
    // Trusted engine observer; invoked under SyncRoot only after a real direct transfer.
    internal event Action<GridProgram, Guid, Guid>? ProgramMoved;
    public Guid InstanceId { get; } = Guid.NewGuid();
    public Guid Id { get; }
    public Generation.CityBlueprint? City { get; }
    public IReadOnlyDictionary<Guid, GridProgram> Programs { get; }
    public IReadOnlyDictionary<Guid, GridDistrict> Districts { get; }
    public GridWorld(Guid? id = null, Generation.CityBlueprint? city = null)
    {
        Id = id ?? Guid.NewGuid();
        City = city;
        if (Id == Guid.Empty) throw new ArgumentException("World ID must not be empty.", nameof(id));
        Programs = new ReadOnlyDictionary<Guid, GridProgram>(programs);
        Districts = new ReadOnlyDictionary<Guid, GridDistrict>(districts);
    }
    public void AddDistrict(GridDistrict district)
    {
        ArgumentNullException.ThrowIfNull(district);
        lock (SyncRoot)
        {
            if (district.Id == Guid.Empty) throw new ArgumentException("District ID must not be empty.", nameof(district));
            if (district.Owner is not null || districts.ContainsKey(district.Id))
                throw new InvalidOperationException("District already belongs to a world or its ID is duplicated.");
            districts.Add(district.Id, district);
            district.Owner = this;
        }
    }
    public void AddProgram(GridProgram program, Guid districtId)
    {
        ArgumentNullException.ThrowIfNull(program);
        lock (SyncRoot)
        {
            if (program.Id == Guid.Empty) throw new ArgumentException("Program ID must not be empty.", nameof(program));
            if (program.Owner is not null || programs.ContainsKey(program.Id))
                throw new InvalidOperationException("Program already belongs to a world or its ID is duplicated.");
            var district = GetDistrict(districtId);
            programs.Add(program.Id, program);
            district.AddMember(program.Id);
            program.CurrentDistrictId = districtId;
            program.Owner = this;
        }
    }
    public GridProgram GetProgram(Guid id)
    {
        lock (SyncRoot) return programs.TryGetValue(id, out var program)
            ? program : throw new KeyNotFoundException($"Unknown program: {id}.");
    }
    internal void RemoveProgram(Guid id)
    {
        lock (SyncRoot)
        {
            var program = GetProgram(id);
            GetDistrict(program.CurrentDistrictId).RemoveMember(id);
            programs.Remove(id);
            program.Owner = null;
            program.CurrentDistrictId = Guid.Empty;
        }
    }
    public GridDistrict GetDistrict(Guid id)
    {
        lock (SyncRoot) return districts.TryGetValue(id, out var district)
            ? district : throw new KeyNotFoundException($"Unknown district: {id}.");
    }
    public void MoveProgram(Guid programId, Guid destinationDistrictId)
    {
        lock (SyncRoot)
        {
            var program = GetProgram(programId);
            var previous = program.CurrentDistrictId;
            CommitProgram(program, program.Values with { DistrictId = destinationDistrictId });
            if (previous != program.CurrentDistrictId)
                ProgramMoved?.Invoke(program, previous, program.CurrentDistrictId);
        }
    }
    internal void CommitProgram(GridProgram program, GridProgramValues values)
    {
        lock (SyncRoot)
        {
            if (program.Owner != this) throw new InvalidOperationException("Program belongs to another world.");
            var destination = GetDistrict(values.DistrictId);
            var source = GetDistrict(program.CurrentDistrictId);
            if (program.Values == values) return;
            var revision = checked(program.Revision + 1);
            if (source != destination)
            {
                source.RemoveMember(program.Id);
                destination.AddMember(program.Id);
            }
            program.Commit(values, revision);
        }
    }
}
