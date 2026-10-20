using Grid.Simulation;
namespace Grid.Terminal;

public static class SocialCommand
{
    public static void Register(CommandDispatcher dispatcher, GridSimulationEngine engine) =>
        dispatcher.Register(new("social", "social [id|name]", "Inspect opted-in civic relationships, memories and simulated resource pools.", args =>
        {
            var state = engine.GetSnapshot();
            if (args.Count != 0)
            {
                var query = string.Join(" ", args);
                var matches = state.Programs.Where(p => Guid.TryParse(query, out var id) ? p.Id == id :
                    p.Name.Equals(query, StringComparison.OrdinalIgnoreCase)).ToArray();
                if (matches.Length != 1) return new(matches.Length == 0 ? "No matching resident." : "Multiple matches; use the full resident ID.");
                var text = Details(state, matches[0].Id);
                return new(text.Length == 0 ? "Resident is not opted into civic social simulation." : text);
            }
            return new($"Social round {state.Social.Round} at tick {state.TickCount}. Last social goals; opt-in simulation, not real messages or host resources.\n" +
                TerminalTable.Render(["Resident", "Preference", "Last goal", "Civic credits", "Memories"],
                    state.Social.Residents.Select(r => new[] { Name(state, r.ProgramId), r.Preference.ToString(), r.Goal.ToString(),
                        r.CivicCredits.ToString(), r.Memories.Count.ToString() })) +
                "\nSIMULATED CIVIC TOKENS (not CPU/RAM)\n" +
                TerminalTable.Render(["District", "Available", "Capacity", "Recovery / round"], state.Social.Pools.Select(p => new[] {
                    state.Districts.Single(d => d.Id == p.DistrictId).Name, p.Available.ToString(), p.Capacity.ToString(), p.RecoveryPerRound.ToString() })) +
                "\nDetails: social <id|name>. No automatic opt-in of host applications or agent creation.");
        }));

    private static string Name(GridSimulationSnapshot state, Guid id) => state.Programs.Single(p => p.Id == id).Name;

    public static string Details(GridSimulationSnapshot state, Guid id)
    {
        var resident = state.Social.Residents.SingleOrDefault(r => r.ProgramId == id);
        if (resident is null) return "";
        return $"Social: {Name(state, id)} | {resident.Preference} | Last goal: {resident.Goal} | Simulated civic credits: {resident.CivicCredits}\n" +
            "RELATIONSHIPS (simulated affinity -100..100)\n" +
            TerminalTable.Render(["Peer", "Affinity", "Last tick"], resident.Relationships.Select(r => new[] {
                Name(state, r.PeerId), r.Affinity.ToString(), r.LastInteractionTick.ToString() })) +
            "\nRECENT MEMORIES (newest first)\n" +
            TerminalTable.Render(["Tick", "Peer", "Experience"], resident.Memories.Reverse().Select(m => new[] {
                m.Tick.ToString(), Name(state, m.PeerId), m.Outcome.ToString() })) +
            "\nOCCUPATION HISTORY (newest first)\n" +
            TerminalTable.Render(["Tick", "Configured civic occupation"], resident.Occupations.Reverse().Select(o => new[] {
                o.Tick.ToString(), o.Occupation?.ToString() ?? "Unassigned" }));
    }
}
