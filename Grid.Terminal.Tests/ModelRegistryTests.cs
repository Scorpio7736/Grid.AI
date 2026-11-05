using System.Security.Cryptography;
using System.Text.Json;
using Grid.AI;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class ModelRegistryTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "grid-registry-" + Guid.NewGuid().ToString("N"));
    private static readonly RuntimeCompatibility Runtime = new("test", "1", "Windows", "X64", "CPU");
    public ModelRegistryTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root, true);
    private ModelManifest Manifest(string revision = "one", string file = "weights", string content = "model", string id = "test")
    {
        File.WriteAllText(Path.Combine(root, file), content);
        var bytes = File.ReadAllBytes(Path.Combine(root, file));
        return new(1, id, revision, "test-license", "local test fixture", "test", "embedded", "template",
            ModelOutputKind.Text, 4_000_000_000, 0, [Runtime], [new(file, bytes.Length, Convert.ToHexString(SHA256.HashData(bytes)), "weights")]);
    }

    [Fact]
    public async Task SharedAssetsAndRetainedVersionsAreCountedOncePerPath()
    {
        var registry = new ModelRegistry(root);
        await registry.ActivateAsync(Manifest());
        await registry.ActivateAsync(Manifest("two"));
        await registry.ActivateAsync(Manifest(id: "other"));
        var report = await registry.VerifyAsync(registry.Read());
        Assert.Equal(5, report.UniqueAssetBytes); Assert.Equal(1, report.UniqueFiles); Assert.Equal(3, report.Versions);
        Assert.Equal(2, registry.Read().Active.Count);
        Assert.Equal("one", registry.Read().Versions[1].PreviousRevision);
    }

    [Fact]
    public async Task CorruptUpdateLeavesCurrentMetadataUnchanged()
    {
        var registry = new ModelRegistry(root);
        await registry.ActivateAsync(Manifest());
        var before = File.ReadAllText(Path.Combine(root, "model-registry.json"));
        var update = Manifest("two", "new");
        File.WriteAllText(Path.Combine(root, "new"), "other");
        await Assert.ThrowsAsync<InvalidDataException>(() => registry.ActivateAsync(update));
        Assert.Equal(before, File.ReadAllText(Path.Combine(root, "model-registry.json")));
    }

    [Fact]
    public async Task RollbackRecoversEvenWhenNewRevisionIsCorrupt()
    {
        var registry = new ModelRegistry(root);
        await registry.ActivateAsync(Manifest());
        await registry.ActivateAsync(Manifest("two", "new", "changed"));
        File.WriteAllText(Path.Combine(root, "new"), "bad");
        await registry.RollbackAsync("test");
        Assert.Equal("one", registry.Read().Active["test"]);
        Assert.Equal("one", (await registry.ResolveActiveAsync("test", Runtime)).Revision);
        await Assert.ThrowsAsync<InvalidDataException>(() => registry.VerifyAsync(registry.Read()));
    }

    [Fact]
    public async Task CorruptRollbackTargetDoesNotChangeActiveRevision()
    {
        var registry = new ModelRegistry(root);
        await registry.ActivateAsync(Manifest());
        await registry.ActivateAsync(Manifest("two", "new"));
        File.Delete(Path.Combine(root, "weights"));
        await Assert.ThrowsAsync<FileNotFoundException>(() => registry.RollbackAsync("test"));
        Assert.Equal("two", registry.Read().Active["test"]);
    }

    [Fact]
    public async Task CancellationDoesNotPublishRegistry()
    {
        var registry = new ModelRegistry(root);
        var manifest = Manifest();
        using var stop = new CancellationTokenSource(); stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => registry.ActivateAsync(manifest, stop.Token));
        Assert.Empty(registry.Read().Versions);
        Assert.Empty(Directory.GetFiles(root, "*.tmp-*"));
    }

    [Fact]
    public async Task ExistingRevisionCannotBeRewritten()
    {
        var registry = new ModelRegistry(root); var manifest = Manifest();
        await registry.ActivateAsync(manifest);
        await Assert.ThrowsAsync<InvalidDataException>(() => registry.ActivateAsync(manifest with { License = "replacement" }));
        Assert.Equal("test-license", registry.Read().Versions[0].Manifest.License);
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("C:\\outside\\model")]
    [InlineData("weights:stream")]
    [InlineData("models/../weights")]
    [InlineData("NUL")]
    [InlineData("model-registry.json")]
    public async Task InvalidPathsCannotBeRegistered(string path)
    {
        var manifest = Manifest(); manifest = manifest with { Assets = [manifest.Assets[0] with { Path = path }] };
        await Assert.ThrowsAsync<InvalidDataException>(() => new ModelRegistry(root).ActivateAsync(manifest));
    }

    [Fact]
    public async Task RuntimeMismatchAndMissingAssetsRefuseResolution()
    {
        var registry = new ModelRegistry(root); await registry.ActivateAsync(Manifest());
        await Assert.ThrowsAsync<InvalidDataException>(() => registry.ResolveActiveAsync("test", Runtime with { Version = "2" }));
        File.Delete(Path.Combine(root, "weights"));
        await Assert.ThrowsAsync<FileNotFoundException>(() => registry.ResolveActiveAsync("test", Runtime));
    }

    [Fact]
    public void ForceLitePersistsWithoutChangingModelInventory()
    {
        new ModelRegistry(root).SetForceLite(true);
        var reopened = new ModelRegistry(root); Assert.True(reopened.Read().ForceLite); Assert.Empty(reopened.Read().Versions);
        reopened.SetForceLite(false); Assert.False(new ModelRegistry(root).Read().ForceLite);
    }

    [Fact]
    public void CorruptRegistryIsNotSilentlyReset()
    {
        File.WriteAllText(Path.Combine(root, "model-registry.json"), "{broken");
        Assert.Throws<JsonException>(() => new ModelRegistry(root).SetForceLite(true));
        Assert.Equal("{broken", File.ReadAllText(Path.Combine(root, "model-registry.json")));
    }

    [Fact]
    public void ProfilesRequireAcceptedModelsAndKnownHeadroom()
    {
        var lite = Manifest();
        var standard = lite with { MinimumProfile = ModelDeviceProfile.Standard, RequiredVramBytes = 6_000_000_000 };
        var device = new ModelDevice("Windows", "X64", 32_000_000_000, 20_000_000_000, 8_000_000_000, 8_000_000_000, 100_000_000_000, [Runtime]);
        Assert.Equal(ModelDeviceProfile.Unavailable, ModelProfileEligibility.Select(device, [], false).Profile);
        Assert.Equal(ModelDeviceProfile.Standard, ModelProfileEligibility.Select(device, [lite, standard], false).Profile);
        Assert.Equal(ModelDeviceProfile.Lite, ModelProfileEligibility.Select(device, [lite, standard], true).Profile);
        Assert.Equal(ModelDeviceProfile.Unavailable, ModelProfileEligibility.Select(device, [standard], true).Profile);
        Assert.Equal(ModelDeviceProfile.Lite, ModelProfileEligibility.Select(device with { TotalRam = 16_000_000_000, TotalVram = null, AvailableVram = null }, [lite, standard], false).Profile);
        Assert.Equal(ModelDeviceProfile.Unavailable, ModelProfileEligibility.Select(device with { AvailableRam = null }, [lite], false).Profile);
    }

    [Fact]
    public void TwelveGbVramDoesNotEnableUnacceptedImagesOrPerformanceModels()
    {
        var device = new ModelDevice("Windows", "X64", 32_000_000_000, 20_000_000_000, 12_000_000_000, 12_000_000_000, 100_000_000_000, [Runtime]);
        var lite = Manifest();
        Assert.Equal(ModelDeviceProfile.Lite, ModelProfileEligibility.Select(device, [lite], false).Profile);
        Assert.Equal(ModelDeviceProfile.Unavailable, ModelProfileEligibility.Select(device, [lite with { Kind = ModelOutputKind.Image }], false).Profile);
    }

    [Fact]
    public void GpuCollectorConvertsMibAndDoesNotPoolAdapters()
    {
        var gpu = ModelDeviceDiscovery.ParseNvidia("NVIDIA A, 8192, 6000\nNVIDIA B, 4096, 1000\n");
        Assert.Equal(2, gpu.Count); Assert.Equal(8_589_934_592, gpu[0].Total); Assert.Equal(6_291_456_000, gpu[0].Free);
        Assert.Throws<InvalidDataException>(() => ModelDeviceDiscovery.ParseNvidia("GPU, [N/A], [N/A]"));
        Assert.Throws<InvalidDataException>(() => ModelDeviceDiscovery.ParseNvidia("GPU, 10, 11"));
    }

    [Fact]
    public void PortableSealKeepsModelWeightsButExcludesMutableRegistrySettings()
    {
        PortableAssets.WriteManifest(root, [new("a", "runtime/model-registry.json", 10, "hash"),
            new("b", "runtime/model-registry.lock", 0, "hash"), new("c", "runtime/models/weights", 20, "hash")]);
        var entries = JsonSerializer.Deserialize<PortableAsset[]>(File.ReadAllText(Path.Combine(root, PortableAssets.Manifest)))!;
        Assert.Single(entries); Assert.Equal("runtime/models/weights", entries[0].Path);
        Assert.Equal("mutable", PackageBudget.Category("runtime/model-registry.json.tmp-123"));
        Assert.Equal("base", PackageBudget.Category("runtime/unrecognized-model-library"));
    }
}
