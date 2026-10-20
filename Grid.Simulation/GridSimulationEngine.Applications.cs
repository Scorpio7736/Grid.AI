using Grid.Core;
using Grid.Simulation.Applications;
using Grid.Simulation.Resources;
namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    private DateTimeOffset lastApplicationSample = DateTimeOffset.MinValue;
    private string applicationDiscoveryStatus = "Not sampled";
    private string CurrentDiscoveryStatus => timeProvider.GetUtcNow() < lastApplicationSample ||
        timeProvider.GetUtcNow() - lastApplicationSample > TimeSpan.FromSeconds(10)
        ? "Unknown/stale (no fresh application sample)" : applicationDiscoveryStatus;
    private readonly Dictionary<Guid, int> missingApplications = new();
    private readonly Dictionary<Guid, ResourceRequirement[]> requirements = new();
    private readonly Dictionary<Guid, ResourceConditionPolicy> conditions = new();

    private ApplicationBinding? CurrentBinding(GridProgram p) => p.Application is { } b
        ? b with { Confirmed = b.Confirmed && timeProvider.GetUtcNow() >= b.ObservedAtUtc &&
            timeProvider.GetUtcNow() - b.ObservedAtUtc <= TimeSpan.FromSeconds(10) } : null;
    private ResourceCondition? CurrentCondition(GridProgram p)
    {
        if (p.Kind == ResidentKind.Legacy) return null;
        if (p.Kind == ResidentKind.Application && (CurrentBinding(p)?.Confirmed != true || p.Application?.Activity == ApplicationActivity.Sleeping))
            return new(null, "Application observation unavailable or stale.");
        if (!conditions.TryGetValue(p.Id, out var policy)) conditions[p.Id] = policy = new();
        return policy.Evaluate(hostResources, requirements.GetValueOrDefault(p.Id) ?? [], timeProvider.GetUtcNow());
    }

    // Trusted configuration boundary, not a terminal privilege or OS allocation operation.
    public void SetResidentRequirements(Guid id, IReadOnlyList<ResourceRequirement> value)
    {
        var copy = value.ToArray();
        new ResourceConditionPolicy().Evaluate([], copy, timeProvider.GetUtcNow());
        lock (gate)
        {
            _ = world.GetProgram(id);
            if (behaviors.GetValueOrDefault(id)?.Routine is not null)
                throw new InvalidOperationException("Use ConfigureRoutine to change a configured routine budget.");
            requirements[id] = copy; conditions.Remove(id);
        }
    }

    public void ReconcileApplications(ApplicationObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        var apps = observation.Applications.Select(a => a with { Processes = Array.AsReadOnly(a.Processes.ToArray()) }).ToArray();
        var exited = observation.ConfirmedExited.ToHashSet();
        if (observation.ObservedAtUtc.Offset != TimeSpan.Zero || string.IsNullOrWhiteSpace(observation.Source) || observation.Source.Length > 256 || apps.Length > 512 ||
            apps.Select(a => a.Key).Distinct(StringComparer.Ordinal).Count() != apps.Length ||
            apps.SelectMany(a => a.Processes).Distinct().Count() != apps.Sum(a => a.Processes.Count) ||
            apps.Sum(a => a.Processes.Count) > 4096 || apps.SelectMany(a => a.Processes).Any(exited.Contains) || exited.Count > 8192 ||
            apps.Any(a => string.IsNullOrWhiteSpace(a.Key) || a.Key.Length > 128 || !ValidLabel(a.Name, 80) ||
                a.WorkingSetBytes < 0 || a.Processes.Count > 4096 || !Enum.IsDefined(a.Activity) || (a.Processes.Count == 0 && a.Activity is ApplicationActivity.Running or ApplicationActivity.OnCall) || a.Processes.Any(p => p.Pid <= 0 || p.StartedAtUtc.Offset != TimeSpan.Zero || p.StartedAtUtc > observation.ObservedAtUtc)))
            throw new ArgumentException("Invalid application observation.");
        lock (gate)
        {
            var now = timeProvider.GetUtcNow();
            if (observation.ObservedAtUtc <= lastApplicationSample || observation.ObservedAtUtc > now ||
                now - observation.ObservedAtUtc > TimeSpan.FromSeconds(10)) return;
            if (world.Districts.Count == 0 && apps.Length != 0) throw new InvalidOperationException("Application residents require a district.");
            lastApplicationSample = observation.ObservedAtUtc;
            applicationDiscoveryStatus = $"{(observation.Complete ? "Complete" : "Partial/unavailable")} | {observation.Source} | Observed groups: {apps.Length}";
            var existing = world.Programs.Values.Where(p => p.Kind == ResidentKind.Application).ToArray();
            var incoming = apps.ToDictionary(a => a.Key, StringComparer.Ordinal);
            foreach (var p in existing)
            {
                var binding = p.Application!;
                var found = incoming.GetValueOrDefault(binding.ApplicationKey);
                if (found is not null && (found.Processes.Count == 0 || binding.Processes.Count == 0))
                {
                    p.Application = binding with { Processes = found.Processes, Activity = found.Activity,
                        Incarnation = binding.Processes.Count == 0 && found.Processes.Count > 0 ? Guid.NewGuid() : binding.Incarnation,
                        ObservedAtUtc = observation.ObservedAtUtc, Confirmed = found.Activity != ApplicationActivity.Unknown,
                        WorkingSetBytes = found.WorkingSetBytes, Source = observation.Source };
                    incoming.Remove(found.Key); missingApplications.Remove(p.Id); conditions.Remove(p.Id);
                    if (binding.Activity != found.Activity)
                        RecordEvent("ApplicationActivity", p.Id, p.Name, $"{binding.Activity} -> {found.Activity}", "{}");
                    continue;
                }
                // A changed PID lifetime is a new incarnation even if the executable key is unchanged.
                if (found is not null && binding.Processes.Intersect(found.Processes).Any())
                {
                    var members = observation.Complete ? found.Processes : Array.AsReadOnly(binding.Processes.Where(l => !exited.Contains(l))
                        .Concat(found.Processes).Distinct().ToArray());
                    if (members.Count <= 4096)
                        p.Application = binding with { Processes = members, ObservedAtUtc = observation.ObservedAtUtc,
                            Activity = found.Activity, Confirmed = found.Activity != ApplicationActivity.Unknown, WorkingSetBytes = observation.Complete ? found.WorkingSetBytes : null, Source = observation.Source };
                    if (binding.Activity != p.Application!.Activity)
                        RecordEvent("ApplicationActivity", p.Id, p.Name, $"{binding.Activity} -> {p.Application.Activity}", "{}");
                    missingApplications.Remove(p.Id); incoming.Remove(found.Key);
                }
                else if (binding.Processes.All(exited.Contains) || observation.Complete && (found is not null || missingApplications.GetValueOrDefault(p.Id) >= 1))
                {
                    world.RemoveProgram(p.Id); missingApplications.Remove(p.Id); requirements.Remove(p.Id); conditions.Remove(p.Id); behaviors.Remove(p.Id);
                    RecordEvent("ApplicationExit", p.Id, p.Name, binding.Processes.All(exited.Contains) ? "Observed process lifetimes confirmed exited." : found is null ? "Absent from two complete observations." : "Previous observed process lifetime ended; new incarnation observed.",
                        System.Text.Json.JsonSerializer.Serialize(new { binding.Incarnation, source = observation.Source }));
                }
                else
                {
                    p.Application = binding with { Confirmed = false, WorkingSetBytes = null };
                    if (found is not null) incoming.Remove(found.Key);
                    // Incomplete samples break the consecutive-confirmation chain.
                    missingApplications[p.Id] = observation.Complete ? 1 : 0;
                    conditions.Remove(p.Id);
                }
            }
            var slots = Math.Max(0, 512 - world.Programs.Values.Count(p => p.Kind == ResidentKind.Application));
            if (incoming.Count > slots) applicationDiscoveryStatus += $" | Deferred at resident limit: {incoming.Count - slots}";
            foreach (var a in incoming.Values.Take(slots))
            {
                var program = new GridProgram { Name = a.Name, Occupation = "Observed application", Kind = ResidentKind.Application,
                    Application = new(a.Key, Guid.NewGuid(), a.Processes, observation.ObservedAtUtc, true,
                        observation.Complete ? a.WorkingSetBytes : null, observation.Source) { Activity = a.Activity, Confirmed = a.Activity != ApplicationActivity.Unknown } };
                var district = world.City?.Read().Districts.Single(d => d.Category == Grid.Core.Generation.HardwareKind.Cpu).Id
                    ?? world.Districts.Values.OrderBy(d => d.Id).First().Id;
                world.AddProgram(program, district);
                RecordEvent("ApplicationObserved", program.Id, program.Name, $"Application discovered: {a.Activity}; no offline activity inferred.",
                    System.Text.Json.JsonSerializer.Serialize(new { program.Application.Incarnation, source = observation.Source }));
            }
        }
    }
}
