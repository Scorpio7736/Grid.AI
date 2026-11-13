using System.Text.Json;
using Grid.AI;

namespace Grid.Terminal;

public static class TrainingDatasetCommands
{
    public static int Run(string[] args)
    {
        try
        {
            if (args.Length is 3 or 4 && args[0] == "--dataset-build")
            {
                var request = Read<TrainingRequest>(args[1]);
                var previous = args.Length == 4 ? Read<TrainingManifest>(args[3]) : null;
                var manifest = TrainingDataset.Build(request, previous);
                TrainingDataset.WriteNew(args[2], manifest);
                Console.WriteLine($"Dataset {manifest.Version}: {manifest.Examples.Length} examples, {manifest.DuplicateIds.Length} duplicates removed, {manifest.Exclusions.Length} sources excluded.");
                Console.WriteLine($"Manifest SHA256: {manifest.ContentSha256}");
                Console.WriteLine(Path.GetFullPath(args[2]));
                return 0;
            }
            if (args.Length == 2 && args[0] == "--dataset-verify")
            {
                var manifest = Read<TrainingManifest>(args[1]);
                TrainingDataset.Verify(manifest);
                Console.WriteLine($"Verified {manifest.Version}: {manifest.Examples.Length} examples. SHA256 {manifest.ContentSha256}");
                return 0;
            }
            Console.Error.WriteLine("Usage: --dataset-build request.json new-manifest.json [previous-manifest.json] | --dataset-verify manifest.json");
            return 2;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            Console.Error.WriteLine("Dataset operation failed: " + error.Message);
            return 1;
        }
    }

    private static T Read<T>(string path)
    {
        using var stream = File.OpenRead(path);
        if (stream.Length > 32 * 1024 * 1024) throw new InvalidDataException("Dataset input exceeds 32 MiB.");
        return JsonSerializer.Deserialize<T>(stream, new JsonSerializerOptions { UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow })
            ?? throw new InvalidDataException("Empty dataset input.");
    }
}
