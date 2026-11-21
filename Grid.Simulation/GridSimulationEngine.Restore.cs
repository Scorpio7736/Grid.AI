using Grid.Core;

namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    /// <summary>Restores durable state into a new, paused session. Wall time and edit authority are never replayed.</summary>
    public static GridSimulationEngine Restore(GridSimulationSnapshot snapshot, TimeProvider? timeProvider = null, Grid.Simulation.Editing.IProgramEditAuthorizer? editAuthorizer = null)
    {
        snapshot = snapshot with { Tasks = Tasks.TaskState.NormalizeAgentBrand(snapshot.Tasks) };
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
        var engine = new GridSimulationEngine(world, snapshot.TickDuration, timeProvider, editAuthorizer);
        foreach (var saved in snapshot.Programs.Where(p => p.Behavior is not null))
        {
            var behavior = saved.Behavior!;
            if (behavior.Routine is { } routine)
            {
                behavior = behavior with { Routine = Behavior.ResidentBehavior.Copy(routine) };
                engine.requirements[saved.Id] = behavior.Routine.Requirements.ToArray();
            }
            if (behavior.Travel is { } travel) behavior = behavior with { Travel = travel with { Route = Array.AsReadOnly(travel.Route.ToArray()) } };
            engine.behaviors[saved.Id] = behavior;
        }
        foreach (var app in snapshot.ApplicationCatalog) engine.applicationCatalog.Add(app.Key, app);
        engine.identities = snapshot.Identities with { RegisteredAgents = Array.AsReadOnly(snapshot.Identities.RegisteredAgents.ToArray()) };
        engine.context = Grid.Communication.ContextState.Copy(snapshot.Context);
        engine.communications = Grid.Communication.CommunicationState.Copy(snapshot.Communications);
        engine.social = Social.SocialState.Copy(snapshot.Social);
        engine.clock.Restore(snapshot.TickCount);
        engine.RenameLegacySystemAgents(snapshot.Tasks);
        engine.RestoreTasks(snapshot.Tasks);
        return engine;
    }

    public static void ValidateSnapshot(GridSimulationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.WorldId == Guid.Empty || snapshot.TickCount < 0 || snapshot.TickDuration.Ticks <= 0 ||
            snapshot.TickCount > long.MaxValue / snapshot.TickDuration.Ticks ||
            snapshot.ElapsedSimulationTime.Ticks != snapshot.TickCount * snapshot.TickDuration.Ticks)
            throw new InvalidDataException("Invalid world identity or simulation clock.");
        if (snapshot.ApplicationCatalog.Select(a => a.Key).Distinct(StringComparer.Ordinal).Count() != snapshot.ApplicationCatalog.Count ||
            snapshot.ApplicationCatalog.Any(a => string.IsNullOrWhiteSpace(a.Key) || a.Key.Length > 128 || !ValidLabel(a.Name, 80) ||
                string.IsNullOrWhiteSpace(a.Source) || a.Source.Length > 256 || a.LastSeenUtc.Offset != TimeSpan.Zero))
            throw new InvalidDataException("Invalid application catalog.");
        var districts = new HashSet<Guid>();
        var programs = new Dictionary<Guid, Guid>();
        foreach (var p in snapshot.Programs)
        {
            if (p.Behavior is { } behavior)
            {
                if (p.Kind == ResidentKind.Legacy) throw new InvalidDataException("Demo residents cannot own routines.");
                Behavior.ResidentBehavior.Validate(behavior, p.Kind);
            }
            if (p.Application is { } a && (string.IsNullOrWhiteSpace(a.ApplicationKey) || a.ApplicationKey.Length > 128 ||
                a.Incarnation == Guid.Empty || a.ObservedAtUtc.Offset != TimeSpan.Zero || a.Processes is null || a.Processes.Count > 4096 || !Enum.IsDefined(a.Activity) || (a.Processes.Count == 0 && a.Activity is ApplicationActivity.Running or ApplicationActivity.OnCall) ||
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
        Behavior.WorkplacePolicy.ValidateAssignments(snapshot);
        Social.SocialState.Validate(snapshot);
        Grid.Communication.ContextState.Validate(snapshot.Context);
        if (snapshot.Context.Documents.Any(d => d.GrantedAgentId is { } agent && !snapshot.Identities.RegisteredAgents.Contains(agent))) throw new InvalidDataException("Context grant references an unregistered agent.");
        Grid.Communication.LocalIdentityState.Validate(snapshot.Identities);
        if (snapshot.Identities.RegisteredAgents.Any(id => !snapshot.Programs.Any(p => p.Id == id && p.Kind == ResidentKind.Agent)))
            throw new InvalidDataException("Registered agent identity is missing or has the wrong resident kind.");
        Grid.Communication.CommunicationState.Validate(snapshot.Communications, snapshot.TickCount);
        if (snapshot.Communications.Memories.Any(m => !snapshot.Identities.RegisteredAgents.Contains(m.AgentId) || m.Grants.Any(g => !snapshot.Identities.RegisteredAgents.Contains(g.AgentId))))
            throw new InvalidDataException("Memory owner or grant references an unregistered agent.");
        foreach (var memory in snapshot.Communications.Memories.Where(m => !m.Forgotten && m.Provenance is { Kind: "context" }))
        {
            var source = snapshot.Context.Documents.SingleOrDefault(d => d.Id == memory.Provenance!.SourceId);
            if (source is null ? !memory.Provenance!.SourceWasTemporary : source.SourceHash != memory.Provenance!.SourceHash || source.GrantedAgentId != memory.AgentId || !source.Project.Equals(memory.Project, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Durable memory source grant/hash does not match captured context.");
        }
        Tasks.TaskState.Validate(snapshot.Tasks);
        if (snapshot.Tasks.Agents.Any(a => !snapshot.Programs.Any(p => p.Id == a.Id && p.Kind == ResidentKind.Agent) || !snapshot.Identities.RegisteredAgents.Contains(a.Id)))
            throw new InvalidDataException("Invalid system-agent identity binding.");
    }

    private static bool ValidLabel(string value, int limit) => !string.IsNullOrWhiteSpace(value) &&
        value.Length <= limit && value == value.Trim() && !value.Any(char.IsControl);
}
