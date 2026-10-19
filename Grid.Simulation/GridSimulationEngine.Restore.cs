using Grid.Core;

namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    /// <summary>Restores durable state into a new, paused session. Wall time and edit authority are never replayed.</summary>
    public static GridSimulationEngine Restore(GridSimulationSnapshot snapshot, TimeProvider? timeProvider = null)
    {
        ValidateSnapshot(snapshot);
        var world = new GridWorld(snapshot.WorldId, snapshot.City);
        foreach (var saved in snapshot.Districts)
        {
            var district = new GridDistrict { Id = saved.Id, Name = saved.Name, Description = saved.Description };
            foreach (var resource in saved.Resources) district.Resources.Add(resource.Key, resource.Value);
            world.AddDistrict(district);
        }
        foreach (var saved in snapshot.Programs)
        {
            var program = new GridProgram { Id = saved.Id, Name = saved.Name, Occupation = saved.Occupation,
                Energy = saved.Energy, Status = saved.Status, Kind = saved.Kind, Application = saved.Application is { } binding ? binding with { Processes = Array.AsReadOnly(binding.Processes.ToArray()) } : null };
            world.AddProgram(program, saved.DistrictId);
            program.Commit(program.Values, saved.Revision);
        }
        var engine = new GridSimulationEngine(world, snapshot.TickDuration, timeProvider);
        engine.clock.Restore(snapshot.TickCount);
        return engine;
    }

    public static void ValidateSnapshot(GridSimulationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.WorldId == Guid.Empty || snapshot.TickCount < 0 || snapshot.TickDuration.Ticks <= 0 ||
            snapshot.TickCount > long.MaxValue / snapshot.TickDuration.Ticks ||
            snapshot.ElapsedSimulationTime.Ticks != snapshot.TickCount * snapshot.TickDuration.Ticks)
            throw new InvalidDataException("Invalid world identity or simulation clock.");
        var districts = new HashSet<Guid>();
        var programs = new Dictionary<Guid, Guid>();
        foreach (var p in snapshot.Programs)
        {
            if (p.Application is { } a && (string.IsNullOrWhiteSpace(a.ApplicationKey) || a.ApplicationKey.Length > 128 ||
                a.Incarnation == Guid.Empty || a.ObservedAtUtc.Offset != TimeSpan.Zero || a.Processes is null || a.Processes.Count is < 1 or > 4096 ||
                a.Processes.Distinct().Count() != a.Processes.Count || a.WorkingSetBytes < 0 ||
                a.Processes.Any(l => l is null || l.Pid <= 0 || l.StartedAtUtc.Offset != TimeSpan.Zero || l.StartedAtUtc > a.ObservedAtUtc)))
                throw new InvalidDataException("Invalid application binding.");
            if (p.Id == Guid.Empty || !programs.TryAdd(p.Id, p.DistrictId) ||
                !Enum.IsDefined(p.Kind) || (p.Kind == ResidentKind.Application) != (p.Application is not null) ||
                !ValidLabel(p.Name, 80) || !ValidLabel(p.Occupation, 120) ||
                !float.IsFinite(p.Energy) || p.Energy < 0 || p.Energy > 100 || p.Revision < 0 || !Enum.IsDefined(p.Status))
                throw new InvalidDataException("Invalid or duplicate program.");
        }
        var members = new HashSet<Guid>();
        foreach (var d in snapshot.Districts)
        {
            if (d.Id == Guid.Empty || !districts.Add(d.Id) || string.IsNullOrWhiteSpace(d.Name) || d.Description is null)
                throw new InvalidDataException("Invalid or duplicate district.");
            foreach (var resource in d.Resources)
                if (string.IsNullOrWhiteSpace(resource.Key) || !float.IsFinite(resource.Value))
                    throw new InvalidDataException("Invalid district resource.");
            foreach (var id in d.ProgramIds)
                if (!members.Add(id) || !programs.TryGetValue(id, out var district) || district != d.Id)
                    throw new InvalidDataException("Inconsistent district membership.");
        }
        if (members.Count != programs.Count || programs.Values.Any(id => !districts.Contains(id)))
            throw new InvalidDataException("Missing district membership.");
        if (snapshot.City is { } city && !city.Read().Districts.Select(d => d.Id).Order().SequenceEqual(districts.Order()))
            throw new InvalidDataException("Saved layout and district identities disagree.");
    }

    private static bool ValidLabel(string value, int limit) => !string.IsNullOrWhiteSpace(value) &&
        value.Length <= limit && value == value.Trim() && !value.Any(char.IsControl);
}
