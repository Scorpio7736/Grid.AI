using System.Text.Json;
using Grid.AI;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class TrainingDatasetTests
{
    private static TrainingSource Source(string id = "source") => new(id, "authored fixture", "synthetic", "CC0-1.0", "fixture author", "2026-11-12", true, true, true);
    private static TrainingExample Example(string id = "one") => new(id, "source", "topic-one", "train", "instruction", "Say hello", "Hello");
    private static TrainingRequest Request() => new("fixture-v1", [Source()], [Example()], []);

    [Fact]
    public void RepeatedBuildProducesIdenticalVerifiedManifest()
    {
        var first = TrainingDataset.Build(Request());
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(TrainingDataset.Build(Request())));
        TrainingDataset.Verify(first);
    }

    [Theory]
    [InlineData("license")]
    [InlineData("privacy")]
    [InlineData("permission")]
    public void EverySourceRequiresReviewAndPermission(string missing)
    {
        var source = Source() with { Kind = "conversation", LicenseApproved = missing != "license", PrivacyApproved = missing != "privacy", TrainingPermission = missing != "permission" };
        Assert.Throws<InvalidDataException>(() => TrainingDataset.Build(Request() with { Sources = [source] }));
    }

    [Fact]
    public void EquivalentPromptsDeduplicateDeterministically()
    {
        var manifest = TrainingDataset.Build(Request() with { Examples = [Example("z"), Example("a") with { Prompt = " SAY   hello " }] });
        Assert.Equal("a", Assert.Single(manifest.Examples).Id);
        Assert.Equal("z", Assert.Single(manifest.DuplicateIds));
        TrainingDataset.Verify(manifest);
    }

    [Fact]
    public void ConflictingAnswersRequireReview()
    {
        Assert.Throws<InvalidDataException>(() => TrainingDataset.Build(Request() with { Examples = [Example(), Example("two") with { Response = "Goodbye" }] }));
    }

    [Theory]
    [InlineData("source")]
    [InlineData("group")]
    [InlineData("prompt")]
    public void HeldOutSplitsRejectContamination(string overlap)
    {
        var other = Example("two") with { SourceId = overlap == "source" ? "source" : "other", Group = overlap == "group" ? "topic-one" : "topic-two", Prompt = overlap == "prompt" ? "say HELLO" : "Say goodbye", Response = "Goodbye", Split = "test" };
        Assert.Throws<InvalidDataException>(() => TrainingDataset.Build(Request() with { Sources = [Source(), Source("other")], Examples = [Example(), other] }));
    }

    [Fact]
    public void SeparateReviewedSourcesCanPopulateAllSplits()
    {
        var request = new TrainingRequest("v1", [Source("a"), Source("b"), Source("c")],
            [new("a", "a", "a", "train", "greeting", "hello", "hi"), new("b", "b", "b", "validation", "math", "2+2", "4"), new("c", "c", "c", "test", "color", "sky", "blue")], []);
        TrainingDataset.Verify(TrainingDataset.Build(request));
    }

    [Fact]
    public void ExclusionRemovesContentAndSourceMetadataFromNewVersion()
    {
        var request = Request() with { Sources = [Source(), Source("withdrawn") with { TrainingPermission = false }],
            Examples = [Example(), new("private", "withdrawn", "private", "test", "private", "private prompt", "private answer")], Exclusions = [new("withdrawn", "Consent withdrawn")] };
        var result = TrainingDataset.Build(request);
        Assert.Single(result.Examples);
        Assert.Single(result.Sources);
        Assert.DoesNotContain("private prompt", JsonSerializer.Serialize(result));
        TrainingDataset.Verify(result);
    }

    [Fact]
    public void DetectsTampering()
    {
        var result = TrainingDataset.Build(Request());
        Assert.Throws<InvalidDataException>(() => TrainingDataset.Verify(result with { Examples = [Example() with { Response = "changed" }] }));
    }

    [Theory]
    [InlineData("password=supersecret123")]
    [InlineData("hello\u001b[31m")]
    public void RejectsRecognizableSecretsAndControlSequences(string content)
    {
        Assert.Throws<InvalidDataException>(() => TrainingDataset.Build(Request() with { Examples = [Example() with { Response = content }] }));
    }

    [Fact]
    public void BundledEvaluationPromptsCannotBecomeTrainingData()
    {
        Assert.Throws<InvalidDataException>(() => TrainingDataset.Build(Request() with
        {
            Examples = [Example() with { Prompt = TextBenchmarkSuite.Baseline.Cases[0].Prompt }]
        }));
    }

    [Fact]
    public void RevisionPinsPreviousSplitsAndLinksManifest()
    {
        var previous = TrainingDataset.Build(Request());
        var next = TrainingDataset.Build(Request() with { Version = "fixture-v2" }, previous);
        Assert.Equal(previous.ContentSha256, next.ParentSha256);
        TrainingDataset.Verify(next);
        Assert.Throws<InvalidDataException>(() => TrainingDataset.Build(Request(), previous));
        Assert.Throws<InvalidDataException>(() => TrainingDataset.Build(Request() with
        {
            Version = "fixture-v2", Examples = [Example() with { Split = "test" }]
        }, previous));
    }

    [Fact]
    public void RejectsMalformedAndUnreviewedInputs()
    {
        Assert.Throws<InvalidDataException>(() => TrainingDataset.Build(Request() with { Examples = [] }));
        Assert.Throws<InvalidDataException>(() => TrainingDataset.Build(Request() with { Sources = [Source(), Source()] }));
        Assert.Throws<InvalidDataException>(() => TrainingDataset.Build(Request() with { Examples = [Example(), Example()] }));
        Assert.Throws<InvalidDataException>(() => TrainingDataset.Build(Request() with { Sources = [Source() with { ReviewDate = "bad" }] }));
        Assert.Throws<InvalidDataException>(() => TrainingDataset.Build(Request() with { Examples = [Example() with { Split = "other" }] }));
    }

    [Fact]
    public void PublicationNeverOverwritesExistingVersion()
    {
        var path = Path.Combine(Path.GetTempPath(), "grid-dataset-" + Guid.NewGuid() + ".json");
        try
        {
            var manifest = TrainingDataset.Build(Request());
            TrainingDataset.WriteNew(path, manifest);
            var original = File.ReadAllBytes(path);
            Assert.Throws<IOException>(() => TrainingDataset.WriteNew(path, manifest));
            Assert.Equal(original, File.ReadAllBytes(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void CommandBuildAndVerifyUseOfflineFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "grid-dataset-" + Guid.NewGuid());
        Directory.CreateDirectory(root);
        try
        {
            var input = Path.Combine(root, "request.json");
            var output = Path.Combine(root, "dataset.json");
            File.WriteAllText(input, JsonSerializer.Serialize(Request()));
            Assert.Equal(0, TrainingDatasetCommands.Run(["--dataset-build", input, output]));
            Assert.Equal(0, TrainingDatasetCommands.Run(["--dataset-verify", output]));
            Assert.Equal(1, TrainingDatasetCommands.Run(["--dataset-build", input, output]));
            Assert.Equal(2, TrainingDatasetCommands.Run(["--dataset-build"]));
        }
        finally { Directory.Delete(root, true); }
    }
}
