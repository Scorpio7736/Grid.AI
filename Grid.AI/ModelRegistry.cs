using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Grid.AI;

public sealed record ModelAsset(string Path, long Bytes, string Sha256, string Purpose);
public sealed record RuntimeCompatibility(string Provider, string Version, string OS, string Architecture, string Backend);
public sealed record ModelManifest(int SchemaVersion, string Id, string Revision, string License,
    string Source, string Quantization, string Tokenizer, string ChatTemplate,
    ModelOutputKind Kind, long RequiredRamBytes, long RequiredVramBytes,
    RuntimeCompatibility[] Runtimes, ModelAsset[] Assets)
{
    public ModelDeviceProfile MinimumProfile { get; init; } = ModelDeviceProfile.Lite;
}
public sealed record ModelRegistration(ModelManifest Manifest, string? PreviousRevision);
public sealed record ModelRegistryState(int SchemaVersion, ModelRegistration[] Versions,
    Dictionary<string, string> Active, bool ForceLite)
{
    public static ModelRegistryState Empty => new(1, [], new(StringComparer.Ordinal), false);
}
public sealed record RegistryVerification(long UniqueAssetBytes, int UniqueFiles, int Versions);

/// <summary>Local metadata transactions. Does not download, delete, load or execute model assets.</summary>
public sealed class ModelRegistry
{
    private readonly string root;
    private readonly string statePath;
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    public ModelRegistry(string assetRoot)
    {
        root = System.IO.Path.GetFullPath(assetRoot);
        CheckLinks(root);
        statePath = System.IO.Path.Combine(root, "model-registry.json");
    }

    public ModelRegistryState Read()
    {
        CheckLinks(statePath);
        if (!File.Exists(statePath)) return ModelRegistryState.Empty;
        if (new FileInfo(statePath).Length > 4_194_304) throw new InvalidDataException("Registry exceeds metadata limit.");
        var state = JsonSerializer.Deserialize<ModelRegistryState>(File.ReadAllBytes(statePath), Json)
            ?? throw new InvalidDataException("Empty registry.");
        ValidateState(state);
        return state;
    }

    public static void ValidateManifest(ModelManifest manifest)
    {
        static bool Label(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 512 && !value.Any(char.IsControl);
        if (manifest.SchemaVersion != 1 || !Key(manifest.Id) || !Key(manifest.Revision) ||
            !Label(manifest.License) || !Label(manifest.Source) || !Label(manifest.Quantization) ||
            !Label(manifest.Tokenizer) || !Label(manifest.ChatTemplate) || !Enum.IsDefined(manifest.Kind) ||
            manifest.RequiredRamBytes <= 0 || manifest.RequiredVramBytes < 0 || !Enum.IsDefined(manifest.MinimumProfile) || manifest.MinimumProfile == ModelDeviceProfile.Unavailable ||
            manifest.Runtimes is not { Length: > 0 and <= 16 } || manifest.Assets is not { Length: > 0 and <= 512 })
            throw new InvalidDataException("Incomplete or unsupported model manifest. Exact artifact metadata is required.");
        foreach (var runtime in manifest.Runtimes)
            if (runtime is null || !Label(runtime.Provider) || !Label(runtime.Version) || !Label(runtime.OS) ||
                !Label(runtime.Architecture) || !Label(runtime.Backend)) throw new InvalidDataException("Incomplete runtime compatibility.");
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var asset in manifest.Assets)
        {
            if (asset is null || !Relative(asset.Path) || !paths.Add(asset.Path.Replace('\\', '/')) || asset.Bytes < 0 ||
                asset.Sha256 is null || !Regex.IsMatch(asset.Sha256, "\\A[0-9A-Fa-f]{64}\\z") || !Label(asset.Purpose))
                throw new InvalidDataException("Invalid or duplicate model asset.");
        }
    }

    public async Task ActivateAsync(ModelManifest manifest, CancellationToken token = default)
    {
        // Freeze caller-owned arrays before any awaits.
        manifest = JsonSerializer.Deserialize<ModelManifest>(JsonSerializer.Serialize(manifest, Json), Json)!;
        ValidateManifest(manifest);
        using var lease = Lock();
        var state = Read();
        var existing = state.Versions.SingleOrDefault(v => v.Manifest.Id == manifest.Id && v.Manifest.Revision == manifest.Revision);
        if (existing is not null && JsonSerializer.Serialize(existing.Manifest, Json) != JsonSerializer.Serialize(manifest, Json))
            throw new InvalidDataException("An existing revision is immutable; use a new revision.");
        var previous = state.Active.GetValueOrDefault(manifest.Id);
        var versions = existing is null ? state.Versions.Append(new ModelRegistration(manifest, previous)).ToArray() : state.Versions;
        var active = new Dictionary<string, string>(state.Active) { [manifest.Id] = manifest.Revision };
        var next = state with { Versions = versions, Active = active };
        ValidateState(next);
        await VerifyAsync(next, token);
        Save(next, token);
    }

    public async Task RollbackAsync(string id, CancellationToken token = default)
    {
        using var lease = Lock();
        var state = Read();
        if (!state.Active.TryGetValue(id, out var current)) throw new InvalidDataException("No active model registration.");
        var version = state.Versions.Single(v => v.Manifest.Id == id && v.Manifest.Revision == current);
        if (version.PreviousRevision is not { } previous) throw new InvalidDataException("No previous revision recorded.");
        var active = new Dictionary<string, string>(state.Active) { [id] = previous };
        var next = state with { Active = active };
        var target = state.Versions.Single(v => v.Manifest.Id == id && v.Manifest.Revision == previous);
        await VerifyAsync(new(1, [target with { PreviousRevision = null }], new() { [id] = previous }, state.ForceLite), token);
        Save(next, token);
    }

    public void SetForceLite(bool enabled)
    {
        using var lease = Lock();
        Save(Read() with { ForceLite = enabled }, default);
    }

    public async Task<ModelManifest> ResolveActiveAsync(string id, RuntimeCompatibility runtime, CancellationToken token = default)
    {
        var state = Read();
        if (!state.Active.TryGetValue(id, out var revision)) throw new InvalidDataException("Model is not registered.");
        var manifest = state.Versions.Single(v => v.Manifest.Id == id && v.Manifest.Revision == revision).Manifest;
        if (!Compatible(manifest, runtime)) throw new InvalidDataException("Model/runtime compatibility has not been registered.");
        await VerifyAsync(new(1, [new(manifest, null)], new() { [id] = revision }, state.ForceLite), token);
        return manifest;
    }

    public async Task<RegistryVerification> VerifyAsync(ModelRegistryState state, CancellationToken token = default)
    {
        ValidateState(state);
        var files = new Dictionary<string, ModelAsset>(StringComparer.OrdinalIgnoreCase);
        foreach (var version in state.Versions)
            foreach (var asset in version.Manifest.Assets)
            {
                var path = Resolve(asset.Path);
                if (files.TryGetValue(path, out var other) && (other.Bytes != asset.Bytes || !other.Sha256.Equals(asset.Sha256, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidDataException("Conflicting references to a shared asset.");
                files[path] = asset;
            }
        long bytes = 0;
        foreach (var (path, asset) in files)
        {
            token.ThrowIfCancellationRequested();
            CheckLinks(path);
            await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 131072, true);
            if (file.Length != asset.Bytes) throw new InvalidDataException("Wrong asset size: " + asset.Path);
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(file, token));
            if (!hash.Equals(asset.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Corrupt model asset: " + asset.Path);
            bytes = checked(bytes + file.Length);
        }
        return new(bytes, files.Count, state.Versions.Length);
    }

    public static bool Compatible(ModelManifest manifest, RuntimeCompatibility available) => manifest.Runtimes.Contains(available);
    private static bool Key(string? value) => value is not null && Regex.IsMatch(value, "\\A[a-zA-Z0-9][a-zA-Z0-9._:-]{0,127}\\z");
    private static bool Relative(string? path) => !string.IsNullOrWhiteSpace(path) && path.Length <= 1024 &&
        !System.IO.Path.IsPathRooted(path) && !path.Contains(':') && !path.Any(char.IsControl) &&
        path.Replace('\\', '/').Split('/').All(p => p.Length > 0 && p is not "." and not ".." && !p.EndsWith(' ') && !p.EndsWith('.') &&
            p.IndexOfAny(['<', '>', '"', '|', '?', '*']) < 0 &&
            !Regex.IsMatch(p, "\\A(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\\.|\\z)", RegexOptions.IgnoreCase) &&
            !p.StartsWith("model-registry", StringComparison.OrdinalIgnoreCase));
    private string Resolve(string relative)
    {
        if (!Relative(relative)) throw new InvalidDataException("Assets must use contained relative paths.");
        var path = System.IO.Path.GetFullPath(System.IO.Path.Combine(root, relative));
        if (!path.StartsWith(root.TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Asset escapes runtime root.");
        return path;
    }
    private static void ValidateState(ModelRegistryState state)
    {
        if (state.SchemaVersion != 1 || state.Versions is null || state.Active is null || state.Versions.Length > 128 || state.Active.Count > 128)
            throw new InvalidDataException("Unsupported or oversized registry.");
        var keys = new HashSet<(string, string)>();
        foreach (var version in state.Versions)
        {
            if (version?.Manifest is null) throw new InvalidDataException("Missing model manifest.");
            ValidateManifest(version.Manifest);
            if (!keys.Add((version.Manifest.Id, version.Manifest.Revision))) throw new InvalidDataException("Duplicate revision.");
            if (version.PreviousRevision is { } previous && (!keys.Contains((version.Manifest.Id, previous)) || previous == version.Manifest.Revision))
                throw new InvalidDataException("Rollback reference must identify an earlier retained revision.");
        }
        if (state.Active.Any(pair => !keys.Contains((pair.Key, pair.Value)))) throw new InvalidDataException("Missing active revision.");
    }
    private FileStream Lock()
    {
        CheckLinks(root); Directory.CreateDirectory(root);
        var path = System.IO.Path.Combine(root, "model-registry.lock"); CheckLinks(path);
        return new(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }
    private void Save(ModelRegistryState state, CancellationToken token)
    {
        ValidateState(state);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(state, Json);
        if (bytes.Length > 4_194_304) throw new InvalidDataException("Registry exceeds metadata limit.");
        var temporary = statePath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { stream.Write(bytes); stream.Flush(true); }
            token.ThrowIfCancellationRequested(); CheckLinks(statePath);
            File.Move(temporary, statePath, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    private static void CheckLinks(string path)
    {
        for (var current = System.IO.Path.GetFullPath(path); current is not null; current = System.IO.Path.GetDirectoryName(current))
            if ((File.Exists(current) || Directory.Exists(current)) && File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
                throw new IOException("Registry and model assets cannot use links.");
    }
}
