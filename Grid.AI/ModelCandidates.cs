namespace Grid.AI;

public sealed record ModelCandidate(string Id, string Purpose, ModelOutputKind Kind, string Status);
public static class ModelCandidates
{
    public static IReadOnlyList<ModelCandidate> All { get; } = Array.AsReadOnly(new[]
    {
        new ModelCandidate("qwen3-4b", "Reference planner/document model", ModelOutputKind.Text, "Pinned baseline; register and verify installed assets explicitly"),
        new ModelCandidate("qwen3-8b", "General text/planning", ModelOutputKind.Text, "Candidate: exact artifact and evaluation pending"),
        new ModelCandidate("qwen2.5-coder-3b", "Coding", ModelOutputKind.Text, "Candidate: exact artifact and evaluation pending"),
        new ModelCandidate("qwen2.5-coder-7b", "Coding", ModelOutputKind.Text, "Candidate: exact artifact and evaluation pending"),
        new ModelCandidate("qwen3-coder-30b-a3b", "Performance coding/coordinator", ModelOutputKind.Text, "Candidate: exact artifact and evaluation pending"),
        new ModelCandidate("sdxl-base-1.0", "Images", ModelOutputKind.Image, "Candidate: complete dependencies, runtime and evaluation pending"),
        new ModelCandidate("flux.2-klein-4b", "Images", ModelOutputKind.Image, "Candidate: complete dependencies and measured memory/latency acceptance pending")
    });
}

public enum ModelDeviceProfile { Unavailable, Lite, Standard, Performance }
public sealed record ModelDevice(string OS, string Architecture, long? TotalRam, long? AvailableRam,
    long? TotalVram, long? AvailableVram, long? FreeDisk, RuntimeCompatibility[] Runtimes);
public sealed record ProfileDecision(ModelDeviceProfile Profile, string Reason);
public static class ModelProfileEligibility
{
    // Caller supplies models whose assets AND device-specific smoke tests have passed.
    // Hardware discovery alone never promotes a candidate or enables an image model.
    public static ProfileDecision Select(ModelDevice device, IEnumerable<ModelManifest> verifiedAndBenchmarked, bool forceLite)
    {
        var ceiling = !forceLite && device.TotalRam >= 32_000_000_000 && device.TotalVram >= 12_000_000_000 && device.AvailableVram >= 12_000_000_000
            ? ModelDeviceProfile.Performance
            : !forceLite && device.TotalRam >= 32_000_000_000 && device.TotalVram >= 8_000_000_000 && device.AvailableVram >= 8_000_000_000
                ? ModelDeviceProfile.Standard : ModelDeviceProfile.Lite;
        var eligible = verifiedAndBenchmarked.Where(m => m.Kind == ModelOutputKind.Text &&
            m.MinimumProfile >= ModelDeviceProfile.Lite && m.MinimumProfile <= ceiling &&
            device.AvailableRam >= m.RequiredRamBytes &&
            (m.RequiredVramBytes == 0 || device.AvailableVram >= m.RequiredVramBytes) &&
            device.Runtimes.Any(r => r.OS == device.OS && r.Architecture == device.Architecture && ModelRegistry.Compatible(m, r))).ToArray();
        if (eligible.Length == 0) return new(ModelDeviceProfile.Unavailable, "No installed, verified, device-benchmarked text model fits the available resources.");
        return new(eligible.Max(m => m.MinimumProfile), forceLite ? "Persistent force-Lite override." :
            "Highest accepted installed text profile within measured headroom. Image models require separate acceptance.");
    }
}
