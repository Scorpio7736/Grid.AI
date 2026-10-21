using Grid.Core;
using Grid.Communication;
namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    // Display-name migration only: IDs retain all grants, profiles, history and conversation ownership.
    private void RenameLegacySystemAgents()
    {
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        { ["ARCHON"] = "MCP", ["FORGE"] = "MAXIS", ["ARCHIVIST"] = "JN" };
        foreach (var program in world.Programs.Values.Where(p => p.Kind == ResidentKind.Agent).ToArray())
        {
            if (!names.TryGetValue(program.Name, out var name)) continue;
            if (program.Revision == long.MaxValue || world.Programs.Values.Any(p => p.Id != program.Id && p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                RecordEvent("AgentRenameSkipped", program.Id, program.Name, $"Rename to {name} requires manual resolution: target name is occupied or revision exhausted. Existing identities preserved.");
                continue;
            }
            program.Commit(program.Values with { Name = name }, program.Revision + 1);
            communications = communications with { Conversations = Array.AsReadOnly(communications.Conversations.Select(c => c with {
                Participants = Array.AsReadOnly(c.Participants.Select(p => p.Kind == ParticipantKind.Program && p.Id == program.Id ? p with { Name = name } : p).ToArray())
            }).ToArray()) };
            RecordEvent("AgentRenamed", program.Id, name, "System-agent display name updated; identity and historical message contents preserved.");
        }
    }
}
