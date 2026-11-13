using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Grid.AI;

public sealed record TrainingPackage(string Name, string Version, string License);
public sealed record FineTuningConfiguration(int Schema, string RunId, string Method, int Seed,
    int Rank, int Alpha, double Dropout, double LearningRate, int Epochs, int MaxSteps,
    int SequenceLength, int BatchSize, int GradientAccumulation, int CheckpointSteps,
    string[] TargetModules, string Device, string BaseModelId, string BaseRevision,
    string BaseManifestSha256, string BaseLicense, bool BaseTrainingPermission,
    TrainingPackage[] Packages, long DevelopmentBudgetBytes, long CheckpointBudgetBytes,
    string ExportFormat, string TargetProvider, string TargetProviderVersion);
public sealed record TrainingJobFile(string Path, long Bytes, string Sha256);
public sealed record FineTuningJob(int Schema, FineTuningConfiguration Configuration, string DatasetSha256,
    TrainingJobFile[] Files, string State, string Digest);

/// <summary>Creates reviewed offline training inputs. Runtime execution and promotion require separate acceptance.</summary>
public static class FineTuningPreparation
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private static readonly string[] Splits = ["train", "validation", "test"];

    public static void Validate(FineTuningConfiguration config)
    {
        if (config is null || config.Schema != 1 || !Key(config.RunId) ||
            config.Method is not ("lora" or "qlora") || config.Device is not ("cpu" or "cuda") ||
            config.Method == "qlora" && config.Device != "cuda" || config.Seed < 0 ||
            config.Rank is < 1 or > 256 || config.Alpha is < 1 or > 1024 ||
            !double.IsFinite(config.Dropout) || config.Dropout is < 0 or >= 1 ||
            !double.IsFinite(config.LearningRate) || config.LearningRate is <= 0 or > 0.1 ||
            config.Epochs is < 1 or > 100 || config.MaxSteps is < 1 or > 1000000 ||
            config.SequenceLength is < 32 or > 32768 || config.BatchSize is < 1 or > 256 ||
            config.GradientAccumulation is < 1 or > 1024 || config.CheckpointSteps is < 1 ||
            config.CheckpointSteps > config.MaxSteps || config.ExportFormat != "peft-safetensors" ||
            !Key(config.BaseModelId) || !Key(config.BaseRevision) || !Digest(config.BaseManifestSha256) ||
            !Text(config.BaseLicense) || !config.BaseTrainingPermission || !Text(config.TargetProvider) ||
            !Text(config.TargetProviderVersion) || config.DevelopmentBudgetBytes <= 0 ||
            config.CheckpointBudgetBytes <= 0 || config.CheckpointBudgetBytes >= config.DevelopmentBudgetBytes)
            throw new InvalidDataException("Incomplete or unsupported fine-tuning configuration, permission or resource budget.");
        if (config.TargetModules is not { Length: > 0 and <= 64 } ||
            config.TargetModules.Any(m => !Key(m)) || config.TargetModules.Distinct(StringComparer.Ordinal).Count() != config.TargetModules.Length)
            throw new InvalidDataException("List unique, exact base-model module names for LoRA targets.");
        if (config.Packages is not { Length: > 0 and <= 100 } ||
            config.Packages.Any(p => p is null || !Key(p.Name) || !Text(p.Version) || !Text(p.License)) ||
            config.Packages.Select(p => p.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != config.Packages.Length)
            throw new InvalidDataException("Exact training package versions and licenses are required.");
        var required = new List<string> { "python", "torch", "transformers", "peft", "accelerate", "safetensors" };
        if (config.Method == "qlora") required.Add("bitsandbytes");
        if (required.Any(name => !config.Packages.Any(p => p.Name == name)))
            throw new InvalidDataException("Training package inventory is incomplete.");
        if (config.Packages.Any(p => p.Version.IndexOfAny(['*', '<', '>', '=', ',', ' ']) >= 0))
            throw new InvalidDataException("Pin exact versions instead of version ranges.");
    }

    public static FineTuningJob Prepare(FineTuningConfiguration config, TrainingManifest dataset, string destination)
    {
        // Freeze caller-owned arrays before validation and publication.
        config = JsonSerializer.Deserialize<FineTuningConfiguration>(JsonSerializer.Serialize(config))!;
        dataset = JsonSerializer.Deserialize<TrainingManifest>(JsonSerializer.Serialize(dataset))!;
        Validate(config);
        TrainingDataset.Verify(dataset);
        foreach (var split in Splits)
            if (!dataset.Examples.Any(e => e.Split == split))
                throw new InvalidDataException("Training job requires train, validation and test examples.");
        var target = Path.GetFullPath(destination);
        CheckLinks(target);
        if (File.Exists(target) || Directory.Exists(target)) throw new IOException("Choose a new job directory.");
        var parent = Path.GetDirectoryName(target) ?? throw new IOException("Job directory requires a parent.");
        if (!Directory.Exists(parent)) throw new DirectoryNotFoundException("Create the development parent directory first.");
        var staging = target + ".preparing-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(staging);
        try
        {
            TrainingDataset.WriteNew(Path.Combine(staging, "dataset.json"), dataset);
            foreach (var split in Splits)
            {
                using var stream = new FileStream(Path.Combine(staging, split + ".jsonl"), FileMode.CreateNew, FileAccess.Write, FileShare.None);
                using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false), leaveOpen: true))
                    foreach (var example in dataset.Examples.Where(e => e.Split == split))
                        writer.WriteLine(JsonSerializer.Serialize(example));
                stream.Flush(true);
            }
            var files = new[] { "dataset.json", "train.jsonl", "validation.jsonl", "test.jsonl" }
                .Select(name => Describe(staging, name)).ToArray();
            var job = new FineTuningJob(1, config, dataset.ContentSha256, files, "Prepared", "");
            job = job with { Digest = Hash(job) };
            var jobBytes = JsonSerializer.SerializeToUtf8Bytes(job, Json);
            var preparedBytes = checked(files.Sum(f => f.Bytes) + jobBytes.LongLength);
            if (preparedBytes > config.DevelopmentBudgetBytes - config.CheckpointBudgetBytes)
                throw new IOException("Prepared inputs and checkpoint reservation exceed the development budget.");
            var free = new DriveInfo(Path.GetPathRoot(target)!).AvailableFreeSpace;
            if (free < checked(config.CheckpointBudgetBytes + jobBytes.LongLength))
                throw new IOException("Insufficient free space for the checkpoint reservation.");
            using (var stream = new FileStream(Path.Combine(staging, "job.json"), FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(jobBytes);
                stream.Flush(true);
            }
            Verify(staging);
            CheckLinks(target);
            Directory.Move(staging, target);
            return job;
        }
        finally { if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true); }
    }

    public static FineTuningJob Verify(string directory)
    {
        var root = Path.GetFullPath(directory);
        CheckLinks(root);
        var job = Read<FineTuningJob>(Path.Combine(root, "job.json"), 1024 * 1024);
        Validate(job.Configuration);
        if (job.Schema != 1 || job.State != "Prepared" || job.Digest != Hash(job) || !Digest(job.DatasetSha256) ||
            job.Files is not { Length: 4 } || job.Files.Any(f => f is null) ||
            !job.Files.Select(f => f.Path).Order(StringComparer.Ordinal).SequenceEqual(new[] { "dataset.json", "test.jsonl", "train.jsonl", "validation.jsonl" }))
            throw new InvalidDataException("Invalid training job inventory or digest.");
        foreach (var file in job.Files)
            if (Describe(root, file.Path) != file) throw new InvalidDataException("Training job file changed: " + file.Path);
        var dataset = Read<TrainingManifest>(Path.Combine(root, "dataset.json"), 32 * 1024 * 1024);
        TrainingDataset.Verify(dataset);
        if (dataset.ContentSha256 != job.DatasetSha256) throw new InvalidDataException("Dataset binding changed.");
        foreach (var split in Splits)
        {
            var expected = dataset.Examples.Where(e => e.Split == split).ToArray();
            var actual = File.ReadLines(Path.Combine(root, split + ".jsonl")).Select(line => JsonSerializer.Deserialize<TrainingExample>(line)).ToArray();
            if (expected.Length == 0 || !expected.SequenceEqual(actual)) throw new InvalidDataException("Split export differs from reviewed dataset.");
        }
        return job;
    }

    public static T Read<T>(string path, long maximumBytes = 1024 * 1024)
    {
        CheckLinks(path);
        using var stream = File.OpenRead(path);
        if (stream.Length > maximumBytes) throw new InvalidDataException("Training metadata exceeds its size limit.");
        return JsonSerializer.Deserialize<T>(stream, new JsonSerializerOptions { UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow })
            ?? throw new InvalidDataException("Training metadata is empty.");
    }
    private static TrainingJobFile Describe(string root, string name)
    {
        var path = Path.Combine(root, name);
        CheckLinks(path);
        using var stream = File.OpenRead(path);
        if (stream.Length > 32 * 1024 * 1024) throw new InvalidDataException("Training input exceeds its size limit.");
        return new(name, stream.Length, Convert.ToHexString(SHA256.HashData(stream)));
    }
    private static string Hash(FineTuningJob job) => ModelAcceptance.Hash(JsonSerializer.Serialize(job with { Digest = "" }));
    private static bool Digest(string? value) => value is { Length: 64 } && value.All(Uri.IsHexDigit);
    private static bool Text(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 500 && !value.Any(char.IsControl);
    private static bool Key(string? value) => value is not null && Regex.IsMatch(value, "\\A[A-Za-z0-9][A-Za-z0-9._:/-]{0,127}\\z");
    private static void CheckLinks(string path)
    {
        for (var current = Path.GetFullPath(path); current is not null; current = Path.GetDirectoryName(current))
            if ((File.Exists(current) || Directory.Exists(current)) && File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
                throw new IOException("Training jobs require ordinary directories and files.");
    }
}
