using Grid.Communication;
using Grid.Simulation.Editing;
namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    public void SetVoiceProfile(EditSessionReference session, VoiceProfile profile)
    {
        using var lease = editAuthorizer.Acquire(session, world.InstanceId);
        if (!IsAuthorized(lease, session)) throw new UnauthorizedAccessException("Master Control is required to change voice profiles.");
        lock (gate)
        {
            if (!identities.RegisteredAgents.Contains(profile.AgentId)) throw new ArgumentException("Register the Agent before assigning a voice profile.");
            _ = ResolveProgram(profile.AgentId.ToString());
            var profiles = communications.VoiceProfiles.Where(p => p.AgentId != profile.AgentId).Append(profile).ToArray();
            try { VoiceProfile.Validate(profiles); }
            catch (InvalidDataException e) { throw new ArgumentException(e.Message, nameof(profile), e); }
            communications = communications with { VoiceProfiles = Array.AsReadOnly(profiles) };
            RecordEvent("VoiceProfileChanged", profile.AgentId, null, "Local voice preference changed; no audio retained.");
        }
    }
}
