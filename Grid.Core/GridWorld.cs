using System.Collections.ObjectModel;

namespace Grid.Core;

/// <summary>Owns world membership. Mutations must run on a single simulation thread.</summary>
public sealed class GridWorld
{
    private readonly Dictionary<Guid, GridProgram> programs = new();
    private readonly Dictionary<Guid, GridDistrict> districts = new();

    public IReadOnlyDictionary<Guid, GridProgram> Programs { get; }
    public IReadOnlyDictionary<Guid, GridDistrict> Districts { get; }

    public GridWorld()
    {
        Programs = new ReadOnlyDictionary<Guid, GridProgram>(programs);
        Districts = new ReadOnlyDictionary<Guid, GridDistrict>(districts);
    }

    public void AddDistrict(GridDistrict district)
    {
        ArgumentNullException.ThrowIfNull(district);
        if (district.Id == Guid.Empty)
            throw new ArgumentException("District ID must not be empty.", nameof(district));
        if (district.Owner is not null || districts.ContainsKey(district.Id))
            throw new InvalidOperationException("District already belongs to a world or its ID is duplicated.");

        districts.Add(district.Id, district);
        district.Owner = this;
    }

    public void AddProgram(GridProgram program, Guid districtId)
    {
        ArgumentNullException.ThrowIfNull(program);
        if (program.Id == Guid.Empty)
            throw new ArgumentException("Program ID must not be empty.", nameof(program));
        if (program.Owner is not null || programs.ContainsKey(program.Id))
            throw new InvalidOperationException("Program already belongs to a world or its ID is duplicated.");
        var district = GetDistrict(districtId);

        programs.Add(program.Id, program);
        district.AddMember(program.Id);
        program.CurrentDistrictId = districtId;
        program.Owner = this;
    }

    /// <exception cref="KeyNotFoundException">The ID is not in this world.</exception>
    public GridProgram GetProgram(Guid id) => programs.TryGetValue(id, out var program)
        ? program : throw new KeyNotFoundException($"Unknown program: {id}.");

    /// <exception cref="KeyNotFoundException">The ID is not in this world.</exception>
    public GridDistrict GetDistrict(Guid id) => districts.TryGetValue(id, out var district)
        ? district : throw new KeyNotFoundException($"Unknown district: {id}.");

    public void MoveProgram(Guid programId, Guid destinationDistrictId)
    {
        // Resolve both IDs before changing anything, so invalid requests leave the world intact.
        var program = GetProgram(programId);
        var destination = GetDistrict(destinationDistrictId);
        if (program.CurrentDistrictId == destinationDistrictId)
            return;

        var source = GetDistrict(program.CurrentDistrictId);
        source.RemoveMember(programId);
        destination.AddMember(programId);
        program.CurrentDistrictId = destinationDistrictId;
    }
}
