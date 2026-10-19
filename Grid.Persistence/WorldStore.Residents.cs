using Grid.Core;
using Grid.Simulation;
namespace Grid.Persistence;

public sealed partial class WorldStore
{
    private static GridSimulationSnapshot PersistentState(GridSimulationSnapshot state, bool excludeLegacy = false)
    {
        var programs = state.Programs.Where(p => p.Kind != ResidentKind.Application && (!excludeLegacy || p.Kind != ResidentKind.Legacy)).ToArray();
        var ids = programs.Select(p => p.Id).ToHashSet();
        return state with { Programs = Array.AsReadOnly(programs), Districts = Array.AsReadOnly(state.Districts.Select(d =>
            d with { ProgramIds = Array.AsReadOnly(d.ProgramIds.Where(ids.Contains).ToArray()) }).ToArray()) };
    }

    /// <summary>Production retrofit. Archive every original resident/state before removing legacy fiction from active population.</summary>
    public (SavedWorld Saved, string? ArchivePath) ArchiveLegacyResidents(CancellationToken token = default)
    {
        lock (gate)
        {
            var saved = Load();
            if (!saved.State.Programs.Any(p => p.Kind == ResidentKind.Legacy)) return (saved, null);
            token.ThrowIfCancellationRequested();
            // Unique, never rolling or overwritten, even after recovering an older backup.
            var archive = DatabasePath + ".legacy-" + Guid.NewGuid().ToString("N") + ".db";
            CopyValidated(DatabasePath, archive);
            token.ThrowIfCancellationRequested();
            Save(saved.Info, PersistentState(saved.State, excludeLegacy: true), token);
            return (Load(), archive);
        }
    }
}
