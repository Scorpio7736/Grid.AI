using System.Text.Json;
using Grid.AI;
using Grid.Communication;

namespace Grid.Terminal;

public static class ModelRegistryCommands
{
    public const string Usage = "ai registry [list|device|verify|acceptance|register-baseline|import <absolute-manifest.json>|rollback <id>|force-lite <on|off>]";
    public static CommandResult Execute(IReadOnlyList<string> args, TerminalIdentitySession identity)
    {
        if (!identity.IsMaster || identity.IsAttachment) return new("Use the original host terminal as Master Control for the model registry.");
        var root = BundledAiRuntime.FindRoot(AppContext.BaseDirectory);
        if (root is null) return new("No bundled runtime root. Provision a complete GRID package first.");
        var operation = args.Count == 1 ? "list" : args[1];
        if (!(args.Count <= 2 && operation is "list" or "device" or "verify" or "acceptance" or "register-baseline") &&
            !(args.Count == 3 && (operation is "rollback" or "import" || operation == "force-lite" && args[2] is "on" or "off")))
            return new(Usage);
        var reference = identity.Reference;
        return new("", BrokerOperation: async token =>
        {
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
            if (!identity.IsMaster || identity.Reference != reference) return new("Registry session revoked.");
            var work = RunAsync(root, operation, args.Count == 3 ? args[2] : null, stop.Token);
            while (!work.IsCompleted)
            {
                await Task.WhenAny(work, Task.Delay(50, CancellationToken.None));
                if (token.IsCancellationRequested || !identity.IsMaster || identity.Reference != reference) stop.Cancel();
            }
            try { return new(await work); }
            catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException or OperationCanceledException)
            { return new("Model registry: " + e.Message + " No runtime/model download or automatic model switch was performed."); }
        });
    }

    public static async Task<string> RunAsync(string root, string operation, string? value, CancellationToken token)
    {
        var registry = new ModelRegistry(root);
        token.ThrowIfCancellationRequested();
        switch (operation)
        {
            case "acceptance":
                return ModelAcceptanceVerification.Describe(root);
            case "device":
                return await ModelDeviceDiscovery.DescribeAsync(root, token);
            case "import":
                if (value is null || !Path.IsPathFullyQualified(value)) throw new IOException("Give an absolute local manifest path.");
                GridTransfer.CheckPath(value);
                if (new FileInfo(value).Length > 1_048_576) throw new IOException("Model manifest exceeds 1 MiB.");
                var imported = JsonSerializer.Deserialize<ModelManifest>(await File.ReadAllBytesAsync(value, token)) ?? throw new InvalidDataException("Empty model manifest.");
                await registry.ActivateAsync(imported, token);
                return "Model revision registered after asset verification. Compatibility declarations are metadata, not benchmark acceptance. Running chat model unchanged.";
            case "register-baseline":
                await registry.ActivateAsync(Baseline(root), token);
                return "Pinned Qwen3 4B registered; all asset hashes verified. Agent profiles share these weights. Existing chat model unchanged.";
            case "verify":
                var measured = await registry.VerifyAsync(registry.Read(), token);
                return $"Registry verified: {measured.Versions} revisions; {measured.UniqueFiles} distinct asset paths; {measured.UniqueAssetBytes:N0} bytes. Shared paths counted once; retained revisions included. Unregistered assets/caches are outside this model inventory; ai storage reports the package.";
            case "rollback":
                await registry.RollbackAsync(value!, token);
                return "Registry active revision rolled back after verifying its assets. Running chat remains on the pinned baseline; registry changes do not switch running providers.";
            case "force-lite":
                registry.SetForceLite(value == "on");
                return "Force-Lite " + value + ". Saved with this runtime package. Restart GRID to apply reduced-context/CPU requests and process-level GPU visibility settings. The running backend is unchanged.";
            default:
                var state = registry.Read();
                var rows = state.Versions.Select(v => $"{v.Manifest.Id} | {v.Manifest.Revision} | {(state.Active.GetValueOrDefault(v.Manifest.Id) == v.Manifest.Revision ? "ACTIVE metadata" : "retained")} | previous: {v.PreviousRevision ?? "none"}");
                return "MODEL REGISTRY\n" + string.Join("\n", rows) + $"\nForce-Lite: {state.ForceLite}\nMetadata only; use ai registry verify for a fresh asset check.\n\nCANDIDATES\n" +
                    string.Join("\n", ModelCandidates.All.Select(c => $"{c.Id}: {c.Purpose}. {c.Status}"));
        }
    }

    public static ModelManifest Baseline(string root)
    {
        PackageBudget.VerifyModel(root, hashes: false); // Trust only the existing pinned manifest, never a mutable model tag.
        const string relative = "models/manifests/registry.ollama.ai/library/qwen3/4b";
        var bytes = File.ReadAllBytes(Path.Combine(root, relative));
        var revision = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
        using var doc = JsonDocument.Parse(bytes);
        var assets = doc.RootElement.GetProperty("layers").EnumerateArray().Prepend(doc.RootElement.GetProperty("config"))
            .Select(a => new ModelAsset("models/blobs/" + a.GetProperty("digest").GetString()!.Replace(':', '-'),
                a.GetProperty("size").GetInt64(), a.GetProperty("digest").GetString()![7..], a.GetProperty("mediaType").GetString()!))
            .Append(new ModelAsset(relative, bytes.Length, revision, "pinned Ollama manifest")).ToArray();
        return new(1, "qwen3-4b", revision, "Apache-2.0; bundled license blob", "Installed pinned Ollama qwen3:4b manifest",
            "Q4_K_M", "Embedded GGUF tokenizer; covered by weights SHA256", "Ollama template blob; covered by asset SHA256",
            ModelOutputKind.Text, 8_000_000_000, 0,
            [new("Ollama", "0.40.9", "Windows", "X64", "CPU"), new("llama.cpp", "b12110", "Windows", "X64", "CPU")], assets);
    }

    public static string LoadOverride(string? root)
    {
        AgentModelProfiles.ForceLite = false;
        if (root is null) return "Model registry unavailable; pinned baseline unchanged.";
        try { AgentModelProfiles.ForceLite = new ModelRegistry(root).Read().ForceLite; return "Model registry force-Lite: " + AgentModelProfiles.ForceLite; }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        { AgentModelProfiles.ForceLite = true; return "Model registry unreadable; using conservative force-Lite: " + e.Message; }
    }
}
