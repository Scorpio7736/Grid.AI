namespace Grid.Communication;

/// <summary>Portable named-agent preference; installed voice availability is checked on playback.</summary>
public sealed record VoiceProfile(Guid AgentId, string VoiceName, int Rate = 0, int Volume = 80)
{
    public static void Validate(IReadOnlyList<VoiceProfile> profiles)
    {
        if (profiles is null || profiles.Count > 128 || profiles.Any(p => p is null || p.AgentId == Guid.Empty ||
            string.IsNullOrWhiteSpace(p.VoiceName) || p.VoiceName.Length > 160 || p.VoiceName.Any(char.IsControl) || p.Rate is < -10 or > 10 || p.Volume is < 0 or > 100) ||
            profiles.Select(p => p.AgentId).Distinct().Count() != profiles.Count)
            throw new InvalidDataException("Invalid saved voice profiles.");
    }
}
