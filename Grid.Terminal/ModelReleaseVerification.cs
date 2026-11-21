using Grid.AI;
using System.Text.Json;

namespace Grid.Terminal;

public static class ModelReleaseVerification
{
    public static async Task<int> RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "grid-release-verification-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "world.db"), "synthetic world canary");
            var policy = new ReleasePolicy("synthetic-suite-v1", 0.9, 1000, 1000000, 1.2, 1.2, 0.02, 30);
            var first = Create(root, "v1", policy);
            var second = Create(root, "v2", policy);
            var rejected = Create(root, "v3", policy);
            rejected = rejected with { Evidence = rejected.Evidence with
            {
                Checks = rejected.Evidence.Checks.Select(c => c with { LatencyMilliseconds = 500 }).ToArray()
            } };
            var path = Path.Combine(root, "request.json");
            foreach (var (candidate, baseline, expected) in new[] { (first, (string?)null, 0), (second, "v1", 0), (rejected, "v2", 1) })
            {
                File.WriteAllText(path, JsonSerializer.Serialize(new ReleaseRequest(policy, baseline, candidate)));
                if (await ReleaseCommands.RunAsync(["--release-promote", root, path]) != expected)
                    throw new InvalidDataException("Unexpected promotion decision.");
            }
            var catalog = new ModelReleaseCatalog(root);
            if (catalog.Read()!.Active != "v2" || catalog.Read()!.Attempts.Last().Passed)
                throw new InvalidDataException("Rejected candidate changed the active release.");
            if (await ReleaseCommands.RunAsync(["--release-rollback", root, "v2"]) != 0 || catalog.Read()!.Active != "v1")
                throw new InvalidDataException("Rollback did not restore the baseline.");
            if (File.ReadAllText(Path.Combine(root, "world.db")) != "synthetic world canary")
                throw new InvalidDataException("Synthetic world changed.");
            Console.WriteLine("6.30 synthetic release lifecycle passed: promote, compare/reject, retained evidence, rollback and world preservation.");
            return 0;
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or JsonException)
        {
            Console.Error.WriteLine("Release verification failed: " + error.Message);
            return 1;
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static ReleaseCandidate Create(string root, string id, ReleasePolicy policy)
    {
        var assets = new List<ModelAsset>();
        foreach (var purpose in new[] { "model", "prompt", "adapter", "evidence" })
        {
            var content = "Synthetic release lifecycle fixture " + id + " " + purpose;
            var name = id + "-" + purpose + ".txt";
            File.WriteAllText(Path.Combine(root, name), content);
            assets.Add(new(name, System.Text.Encoding.UTF8.GetByteCount(content), ModelAcceptance.Hash(content), purpose));
        }
        var runtime = new RuntimeCompatibility("synthetic-fixture", "1", "Windows", "x64", "cpu");
        var model = new ModelManifest(1, "release-fixture", id, "Project-authored fixture", "Synthetic verification", "none",
            "fixture", "fixture", ModelOutputKind.Text, 1024, 0, [runtime], assets.ToArray());
        var definition = new ReleaseDefinition(id, "synthetic-lifecycle", model, ModelAcceptance.Hash("fixture dataset"),
            ModelAcceptance.Hash("release lifecycle suite v1"), ModelAcceptance.Hash("synthetic device"), runtime, 1, "fixture-host-v1");
        return new(definition, new(ReleaseEvaluation.Hash(definition), ReleaseEvaluation.Hash(policy), DateTimeOffset.UtcNow,
            ReleaseEvaluation.RequiredChecks.Select(name => new ReleaseCheck(name, true, 1, 100, 1000)).ToArray()));
    }
}
