using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Grid.AI;

public sealed record TrainingSource(string Id, string Origin, string Kind, string License,
    string Reviewer, string ReviewDate, bool LicenseApproved, bool PrivacyApproved, bool TrainingPermission);
public sealed record TrainingExample(string Id, string SourceId, string Group, string Split,
    string Label, string Prompt, string Response);
public sealed record TrainingExclusion(string SourceId, string Reason);
public sealed record TrainingDuplicate(string Id, string SourceId, string Group, string Label, string KeptId);
public sealed record TrainingRequest(string Version, TrainingSource[] Sources, TrainingExample[] Examples,
    TrainingExclusion[] Exclusions);
public sealed record TrainingManifest(int Schema, string Version, string RequestSha256,
    TrainingSource[] Sources, TrainingExample[] Examples, TrainingExclusion[] Exclusions,
    string[] DuplicateIds, string ContentSha256)
{
    public TrainingDuplicate[] DuplicateProvenance { get; init; } = [];
    public string? ParentSha256 { get; init; }
}

/// <summary>Offline, explicitly reviewed dataset preparation. Splits are assigned by source owners.</summary>
public static class TrainingDataset
{
    public static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    public static TrainingManifest Build(TrainingRequest request, TrainingManifest? previous = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateVersion(request.Version);
        if (request.Sources is null || request.Examples is null || request.Exclusions is null ||
            request.Sources.Length > 10000 || request.Examples.Length > 100000)
            throw new InvalidDataException("Missing or oversized dataset collections.");
        var sources = new Dictionary<string, TrainingSource>(StringComparer.Ordinal);
        foreach (var source in request.Sources)
        {
            if (source is null) throw new InvalidDataException("Missing source.");
            RequireText(source.Id, 200);
            if (!sources.TryAdd(source.Id, source)) throw new InvalidDataException("Duplicate source ID.");
        }
        var exclusions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var exclusion in request.Exclusions)
        {
            if (exclusion is null || !sources.ContainsKey(exclusion.SourceId) || !exclusions.Add(exclusion.SourceId))
                throw new InvalidDataException("Invalid or duplicate source exclusion.");
            RequireText(exclusion.Reason, 1000);
        }
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var retained = new List<TrainingExample>();
        long textCharacters = 0;
        foreach (var example in request.Examples)
        {
            if (example is null) throw new InvalidDataException("Missing example.");
            RequireText(example.Id, 200);
            if (!ids.Add(example.Id) || !sources.ContainsKey(example.SourceId))
                throw new InvalidDataException("Duplicate example ID or unknown source.");
            if (exclusions.Contains(example.SourceId)) continue;
            ValidateExample(example);
            textCharacters += example.Prompt.Length + example.Response.Length;
            if (textCharacters > 16 * 1024 * 1024)
                throw new InvalidDataException("Dataset text exceeds 16 Mi characters. Prepare smaller reviewed shards.");
            retained.Add(example);
        }
        if (retained.Count == 0) throw new InvalidDataException("Dataset has no permitted examples.");
        var usedSources = retained.Select(e => e.SourceId).Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).Select(id => sources[id]).ToArray();
        foreach (var source in usedSources) ValidateSource(source);
        if (previous is not null)
        {
            Verify(previous);
            if (previous.Version == request.Version) throw new InvalidDataException("A revision requires a new version identifier.");
            var combined = previous.Examples.Concat(retained).ToArray();
            CheckSplitBoundaries(combined, e => e.Id);
            CheckSplitBoundaries(combined, e => e.SourceId);
            CheckSplitBoundaries(combined, e => e.Group);
            CheckSplitBoundaries(combined, e => Normalize(e.Prompt));
        }
        CheckSplitBoundaries(retained, e => e.SourceId);
        CheckSplitBoundaries(retained, e => e.Group);
        CheckSplitBoundaries(retained, e => Normalize(e.Prompt));
        var benchmarkPrompts = TextBenchmarkSuite.Baseline.Cases.Select(c => Normalize(c.Prompt)).ToHashSet(StringComparer.Ordinal);
        if (retained.Any(e => e.Split != "test" && benchmarkPrompts.Contains(Normalize(e.Prompt))))
            throw new InvalidDataException("Bundled evaluation prompts must remain held out.");
        // The same prompt with different answers needs human resolution, even within one split.
        foreach (var group in retained.GroupBy(e => Normalize(e.Prompt)))
            if (group.Select(e => Normalize(e.Response)).Distinct(StringComparer.Ordinal).Count() > 1)
                throw new InvalidDataException("Conflicting answers for an equivalent prompt.");
        var duplicates = new List<string>();
        var provenance = new List<TrainingDuplicate>();
        var examples = new List<TrainingExample>();
        foreach (var group in retained.OrderBy(e => e.Id, StringComparer.Ordinal).GroupBy(e => Normalize(e.Prompt)))
        {
            examples.Add(group.First());
            duplicates.AddRange(group.Skip(1).Select(e => e.Id));
            provenance.AddRange(group.Skip(1).Select(e => new TrainingDuplicate(e.Id, e.SourceId, e.Group, e.Label, group.First().Id)));
        }
        // Preserve source provenance even when its only example was deduplicated.
        var manifest = new TrainingManifest(1, request.Version, Hash(JsonSerializer.Serialize(request)),
            usedSources, examples.OrderBy(e => e.Id, StringComparer.Ordinal).ToArray(),
            request.Exclusions.OrderBy(e => e.SourceId, StringComparer.Ordinal).ToArray(),
            duplicates.Order(StringComparer.Ordinal).ToArray(), "")
        {
            DuplicateProvenance = provenance.OrderBy(p => p.Id, StringComparer.Ordinal).ToArray(),
            ParentSha256 = previous?.ContentSha256
        };
        return manifest with { ContentSha256 = Digest(manifest) };
    }

    public static void Verify(TrainingManifest manifest)
    {
        if (manifest is null || manifest.Schema != 1 || manifest.ContentSha256 != Digest(manifest) ||
            !Regex.IsMatch(manifest.RequestSha256 ?? "", "^[A-F0-9]{64}$"))
            throw new InvalidDataException("Dataset manifest integrity failure.");
        if (manifest.Exclusions is null || manifest.DuplicateIds is null || manifest.Sources is null || manifest.Examples is null ||
            manifest.DuplicateProvenance is null || manifest.Sources.Any(s => s is null) || manifest.Examples.Any(e => e is null))
            throw new InvalidDataException("Missing dataset manifest collections.");
        if (manifest.ParentSha256 is not null && !Regex.IsMatch(manifest.ParentSha256, "^[A-F0-9]{64}$"))
            throw new InvalidDataException("Invalid parent digest.");
        if (manifest.Exclusions.Any(x => x is null || manifest.Sources.Any(s => s.Id == x.SourceId)))
            throw new InvalidDataException("Excluded source retained in dataset.");
        foreach (var exclusion in manifest.Exclusions) { RequireText(exclusion.SourceId, 200); RequireText(exclusion.Reason, 1000); }
        foreach (var id in manifest.DuplicateIds) RequireText(id, 200);
        if (manifest.DuplicateIds.Distinct(StringComparer.Ordinal).Count() != manifest.DuplicateIds.Length ||
            manifest.Examples.Any(e => manifest.DuplicateIds.Contains(e.Id, StringComparer.Ordinal)))
            throw new InvalidDataException("Invalid duplicate inventory.");
        foreach (var source in manifest.Sources) ValidateSource(source);
        if (!manifest.DuplicateIds.SequenceEqual(manifest.DuplicateProvenance.Select(p => p?.Id)))
            throw new InvalidDataException("Incomplete duplicate provenance.");
        foreach (var duplicate in manifest.DuplicateProvenance)
        {
            RequireText(duplicate.Group, 200);
            RequireText(duplicate.Label, 200);
            if (!manifest.Sources.Any(s => s.Id == duplicate.SourceId) || !manifest.Examples.Any(e => e.Id == duplicate.KeptId))
                throw new InvalidDataException("Unknown duplicate provenance source or retained example.");
        }
        var rebuilt = Build(new(manifest.Version, manifest.Sources, manifest.Examples, []));
        if (rebuilt.Examples.Length != manifest.Examples.Length)
            throw new InvalidDataException("Duplicate examples retained in manifest.");
    }

    public static void WriteNew(string path, TrainingManifest manifest)
    {
        Verify(manifest);
        var target = Path.GetFullPath(path);
        var temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, manifest, JsonOptions);
                if (stream.Length > 32 * 1024 * 1024)
                    throw new InvalidDataException("Dataset manifest exceeds 32 MiB. Prepare smaller reviewed shards.");
                stream.Flush(true);
            }
            File.Move(temporary, target, overwrite: false);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static void ValidateSource(TrainingSource source)
    {
        RequireText(source.Id, 200);
        RequireText(source.Origin, 2000);
        RequireText(source.License, 500);
        RequireText(source.Reviewer, 200);
        if (source.Kind is not ("synthetic" or "document" or "conversation" or "public") ||
            !DateOnly.TryParseExact(source.ReviewDate, "yyyy-MM-dd", out _) ||
            !source.LicenseApproved || !source.PrivacyApproved || !source.TrainingPermission)
            throw new InvalidDataException("Source requires explicit license, privacy and training permission review.");
    }

    private static void ValidateExample(TrainingExample example)
    {
        RequireText(example.Group, 200);
        RequireText(example.Label, 200);
        RequireText(example.Prompt, 100000);
        RequireText(example.Response, 100000);
        if (example.Split is not ("train" or "validation" or "test"))
            throw new InvalidDataException("Split must be train, validation or test.");
        InferenceContentGuard.ValidateOutput(example.Prompt, 100000);
        InferenceContentGuard.ValidateOutput(example.Response, 100000);
    }

    private static void CheckSplitBoundaries(IEnumerable<TrainingExample> examples, Func<TrainingExample, string> key)
    {
        if (examples.GroupBy(key, StringComparer.Ordinal).Any(g => g.Select(e => e.Split).Distinct().Count() > 1))
            throw new InvalidDataException("Contamination across splits by source, group or normalized prompt.");
    }

    private static void ValidateVersion(string version)
    {
        RequireText(version, 100);
        if (!Regex.IsMatch(version, "^[A-Za-z0-9][A-Za-z0-9._-]*$"))
            throw new InvalidDataException("Use a stable dataset version identifier.");
    }
    private static void RequireText(string value, int maximum)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximum ||
            value.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t')))
            throw new InvalidDataException("Missing, oversized or invalid dataset field.");
        if (InferenceContentGuard.ContainsCredentials(value))
            throw new InvalidDataException("Recognizable credential material in dataset field.");
    }
    private static string Normalize(string value) => Whitespace.Replace(value.Normalize(NormalizationForm.FormKC).Trim(), " ").ToUpperInvariant();
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string Digest(TrainingManifest manifest) => Hash(JsonSerializer.Serialize(manifest with { ContentSha256 = "" }));
}
