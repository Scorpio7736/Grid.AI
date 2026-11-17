using Grid.Simulation.Editing;

namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    public void AuthorizeSkillAdministration(EditSessionReference session)
    {
        using var lease = TaskLease(session, true);
        if (!lease.Identity.IsOriginalHost) throw new UnauthorizedAccessException("Skill administration requires the original Master Control terminal.");
    }
    public void AuditSkillLifecycle(EditSessionReference session, string action, string skillId, string manifestHash)
    {
        using var lease = TaskLease(session, true);
        lock (gate)
        {
            if (!lease.Identity.IsOriginalHost) throw new UnauthorizedAccessException("Skill administration requires the original Master Control terminal.");
            if (action is not ("Imported" or "Installed" or "Updated" or "Enabled" or "Disabled" or "Configured" or "Revoked" or "Uninstalled" or "RolledBack" or "Invoked") ||
                string.IsNullOrWhiteSpace(skillId) || skillId.Length > 64 || manifestHash.Length != 64 || manifestHash.Any(c => !Uri.IsHexDigit(c)))
                throw new ArgumentException("Invalid skill lifecycle audit record.");
            RecordEvent("Skill" + action, null, skillId, action + " reviewed local skill; manifest SHA256 " + manifestHash + ".");
        }
    }
}
