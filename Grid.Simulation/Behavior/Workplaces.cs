using Grid.Core.Generation;
using System.Security.Cryptography;
using System.Text;
namespace Grid.Simulation.Behavior;

public sealed record Workstation(Guid Id, string Name, Guid DistrictId, RoutineOccupation Occupation, int Capacity);
public sealed record WorkTravel(Guid WorkstationId, IReadOnlyList<Guid> Route, long RemainingEdgeTicks);

/// <summary>District-level civic routes; not physical host scheduling or per-frame pedestrian geometry.</summary>
public static class WorkplacePolicy
{
    public const long EdgeTicks = TimeSpan.TicksPerSecond * 5;

    public static IReadOnlyList<Workstation> Stations(GridSimulationSnapshot state)
    {
        var city = state.City?.Read();
        var result = new List<Workstation>();
        foreach (var occupation in Enum.GetValues<RoutineOccupation>())
        {
            var category = occupation switch {
                RoutineOccupation.Administrator => HardwareKind.Motherboard,
                RoutineOccupation.Archivist => HardwareKind.Storage,
                RoutineOccupation.Researcher => HardwareKind.Ram,
                _ => HardwareKind.Cpu
            };
            foreach (var district in state.Districts.OrderBy(d => d.Id))
            {
                var geometry = city?.Districts.Single(d => d.Id == district.Id);
                if (geometry is not null && geometry.Category != category) continue;
                // One workstation per saved lot. Old/demo worlds get one abstract local station per occupation.
                var count = geometry?.Lots.Count ?? 1;
                for (var i = 0; i < count; i++)
                {
                    var key = $"{district.Id:D}:{occupation}:{i}";
                    var id = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(key)).AsSpan(0, 16));
                    result.Add(new(id, $"{occupation} station {i + 1}", district.Id, occupation, 1));
                }
            }
        }
        return result.AsReadOnly();
    }

    public static IReadOnlyList<Guid>? Route(GridSimulationSnapshot state, Guid from, Guid to)
    {
        var ids = state.Districts.Select(d => d.Id).ToHashSet();
        if (!ids.Contains(from) || !ids.Contains(to)) throw new ArgumentException("Route district is not in the world.");
        if (from == to) return Array.AsReadOnly(new[] { from });
        var roads = state.City?.Read().Roads ?? [];
        var queue = new Queue<Guid>(); queue.Enqueue(from);
        var previous = new Dictionary<Guid, Guid> { [from] = Guid.Empty };
        while (queue.TryDequeue(out var at))
        {
            var neighbors = roads.Where(r => r.From == at || r.To == at)
                .Select(r => r.From == at ? r.To : r.From).Distinct().Order();
            foreach (var next in neighbors)
            {
                if (!previous.TryAdd(next, at)) continue;
                if (next == to)
                {
                    var route = new List<Guid> { to };
                    for (var p = at; p != Guid.Empty; p = previous[p]) route.Add(p);
                    route.Reverse();
                    return route.AsReadOnly();
                }
                queue.Enqueue(next);
            }
        }
        return null;
    }

    public static void ValidateAssignments(GridSimulationSnapshot state)
    {
        var stations = Stations(state).ToDictionary(s => s.Id);
        var assigned = new HashSet<Guid>();
        foreach (var p in state.Programs.Where(p => p.Behavior?.Travel is not null))
        {
            var travel = p.Behavior!.Travel!;
            if (p.Kind is not (Grid.Core.ResidentKind.Agent or Grid.Core.ResidentKind.Special) ||
                p.Behavior.Routine is not { } routine || !stations.TryGetValue(travel.WorkstationId, out var station) ||
                station.Occupation != routine.Occupation || !assigned.Add(station.Id) ||
                travel.Route is null || travel.Route.Count < 1 || travel.Route.Count > state.Districts.Count ||
                travel.Route[0] != p.DistrictId || travel.Route[^1] != station.DistrictId ||
                travel.Route.Distinct().Count() != travel.Route.Count ||
                travel.RemainingEdgeTicks < 0 || travel.RemainingEdgeTicks > EdgeTicks ||
                (travel.Route.Count == 1 ? travel.RemainingEdgeTicks != 0 : travel.RemainingEdgeTicks == 0))
                throw new InvalidDataException("Invalid workstation assignment or route.");
            for (var i = 1; i < travel.Route.Count; i++)
                if (state.City?.Read().Roads.Any(r =>
                    r.From == travel.Route[i - 1] && r.To == travel.Route[i] ||
                    r.To == travel.Route[i - 1] && r.From == travel.Route[i]) != true)
                    throw new InvalidDataException("Movement route crosses a missing road.");
        }
    }
}
