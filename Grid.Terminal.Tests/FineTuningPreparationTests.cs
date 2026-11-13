using Grid.AI;
using System.Text.Json;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class FineTuningPreparationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "grid-finetune-" + Guid.NewGuid().ToString("N"));
    public FineTuningPreparationTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root, true);
    private static FineTuningConfiguration Config() => new(1, "fixture", "lora", 42, 8, 16, 0.05, 0.0002, 1, 10,
        128, 1, 1, 5, ["q_proj", "v_proj"], "cpu", "fixture-base", "v1", new string('A', 64), "fixture only", true,
        new[] { "python", "torch", "transformers", "peft", "accelerate", "safetensors" }.Select(n => new TrainingPackage(n, "0.0-fixture", "fixture only")).ToArray(),
        1000000, 1000, "peft-safetensors", "ollama", "fixture");
    private static TrainingManifest Dataset()
    {
        var splits = new[] { "train", "validation", "test" };
        return TrainingDataset.Build(new("fixture-v1",
            splits.Select(s => new TrainingSource(s, "synthetic fixture", "synthetic", "fixture only", "author", "2026-11-13", true, true, true)).ToArray(),
            splits.Select(s => new TrainingExample(s, s, s, s, "fixture", "prompt " + s, "answer " + s)).ToArray(), []));
    }

    [Fact]
    public void PreparationIsReproducibleAndKeepsSplitsSeparate()
    {
        var first = FineTuningPreparation.Prepare(Config(), Dataset(), Path.Combine(root, "first"));
        var second = FineTuningPreparation.Prepare(Config(), Dataset(), Path.Combine(root, "second"));
        Assert.Equal(first.Digest, second.Digest);
        Assert.Equal(first.Digest, FineTuningPreparation.Verify(Path.Combine(root, "first")).Digest);
        var train = File.ReadAllText(Path.Combine(root, "first", "train.jsonl"));
        Assert.DoesNotContain("prompt test", train);
        Assert.DoesNotContain("prompt validation", train);
        Assert.Equal("Prepared", first.State);
    }

    [Theory]
    [InlineData("permission")]
    [InlineData("seed")]
    [InlineData("learning-rate")]
    [InlineData("rank")]
    [InlineData("budget")]
    [InlineData("target")]
    public void InvalidConfigurationsAreRejected(string field)
    {
        var config = field switch
        {
            "permission" => Config() with { BaseTrainingPermission = false },
            "seed" => Config() with { Seed = -1 },
            "learning-rate" => Config() with { LearningRate = double.NaN },
            "rank" => Config() with { Rank = 0 },
            "budget" => Config() with { CheckpointBudgetBytes = 1000000 },
            _ => Config() with { TargetModules = [] }
        };
        Assert.Throws<InvalidDataException>(() => FineTuningPreparation.Validate(config));
    }

    [Fact]
    public void QloraRequiresCudaAndBitsandbytesInventory()
    {
        var config = Config() with { Method = "qlora" };
        Assert.Throws<InvalidDataException>(() => FineTuningPreparation.Validate(config));
        config = config with { Device = "cuda" };
        Assert.Throws<InvalidDataException>(() => FineTuningPreparation.Validate(config));
        FineTuningPreparation.Validate(config with { Packages = [..config.Packages, new("bitsandbytes", "0.0-fixture", "fixture only")] });
    }

    [Fact]
    public void MissingOrUnpinnedDependenciesAreRejected()
    {
        Assert.Throws<InvalidDataException>(() => FineTuningPreparation.Validate(Config() with { Packages = [] }));
        var packages = Config().Packages;
        packages[0] = packages[0] with { Version = ">=3.11" };
        Assert.Throws<InvalidDataException>(() => FineTuningPreparation.Validate(Config() with { Packages = packages }));
    }

    [Fact]
    public void IncompleteDatasetCannotBecomeTrainingJob()
    {
        var dataset = TrainingDataset.Build(new("v1", [new("s", "fixture", "synthetic", "fixture", "author", "2026-11-13", true, true, true)],
            [new("e", "s", "g", "train", "fixture", "hello", "hi")], []));
        Assert.Throws<InvalidDataException>(() => FineTuningPreparation.Prepare(Config(), dataset, Path.Combine(root, "job")));
        Assert.Empty(Directory.GetFileSystemEntries(root));
    }

    [Fact]
    public void BudgetFailureLeavesNoPublishedOrTemporaryJob()
    {
        Assert.Throws<IOException>(() => FineTuningPreparation.Prepare(Config() with { DevelopmentBudgetBytes = 1001 }, Dataset(), Path.Combine(root, "job")));
        Assert.Empty(Directory.GetFileSystemEntries(root));
    }

    [Fact]
    public void ExistingJobSurvivesRetryAndTamperingIsDetected()
    {
        var path = Path.Combine(root, "job");
        var job = FineTuningPreparation.Prepare(Config(), Dataset(), path);
        Assert.Throws<IOException>(() => FineTuningPreparation.Prepare(Config(), Dataset(), path));
        Assert.Equal(job.Digest, FineTuningPreparation.Verify(path).Digest);
        File.AppendAllText(Path.Combine(path, "test.jsonl"), "tampered");
        Assert.Throws<InvalidDataException>(() => FineTuningPreparation.Verify(path));
    }

    [Fact]
    public void CommandUsesPreparedStateAndReportsMalformedArguments()
    {
        var config = Path.Combine(root, "config.json");
        var dataset = Path.Combine(root, "dataset.json");
        var job = Path.Combine(root, "job");
        File.WriteAllText(config, JsonSerializer.Serialize(Config()));
        TrainingDataset.WriteNew(dataset, Dataset());
        Assert.Equal(0, FineTuningCommands.Run(["--finetune-prepare", config, dataset, job]));
        Assert.Equal(0, FineTuningCommands.Run(["--finetune-verify", job]));
        Assert.Equal(2, FineTuningCommands.Run(["--finetune-prepare"]));
        Assert.Equal(1, FineTuningCommands.Run(["--finetune-verify", Path.Combine(root, "missing")]));
    }
}
