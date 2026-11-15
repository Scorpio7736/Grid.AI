using Xunit;
namespace Grid.Terminal.Tests;

public sealed class GuidedSetupTests
{
    [Theory]
    [InlineData(7_999_999_999L, 999_000_000L, true, false)]
    [InlineData(8_000_000_000L, 255_999_999L, true, false)]
    [InlineData(8_000_000_000L, 256_000_000L, true, true)]
    [InlineData(32_000_000_000L, 999_000_000L, false, false)]
    public void LiteNeedsMeasuredResourcesAndSupportedHost(long ram, long disk, bool host, bool expected)
    { Assert.Equal(expected, GuidedSetup.CanUseLite(new(ram, disk, 4, host))); }
    [Fact]
    public void UnknownMemoryIsNotReportedCompatible()
    {
        Assert.False(GuidedSetup.CanUseLite(new(null, null, 2, true)));
        Assert.Contains("Standard: unavailable", GuidedSetup.Choices(new(null, null, 2, true)));
    }
    [Fact]
    public void FlashRelocatesOnlyInsidePackageAndChangedDeviceRequiresReview()
    {
        var profile = new SetupProfile(1, "pc1", "flash", "data/world.db", "off");
        GuidedSetup.Validate(profile, "pc1", Path.GetTempPath());
        Assert.EndsWith(Path.Combine("data", "world.db"), GuidedSetup.ResolveWorld(profile, Path.GetTempPath()));
        Assert.Throws<InvalidDataException>(() => GuidedSetup.Validate(profile, "pc2", Path.GetTempPath()));
        Assert.Throws<InvalidDataException>(() => GuidedSetup.Validate(profile, "pc1", null));
        Assert.Throws<InvalidDataException>(() => GuidedSetup.Validate(profile with { WorldPath = "../other.db" }, "pc1", Path.GetTempPath()));
    }
    [Fact]
    public void SavedProfileRoundTripsAndCannotOverwrite()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        var profile = new SetupProfile(1, "pc1", "pc", Path.Combine(Path.GetTempPath(), "test.db"), "off");
        try
        {
            GuidedSetup.SaveNew(path, profile);
            Assert.Equal(profile, GuidedSetup.Read(path, "pc1", null));
            Assert.Throws<IOException>(() => GuidedSetup.SaveNew(path, profile with { AiMode = "lite" }));
            Assert.Equal(profile, GuidedSetup.Read(path, "pc1", null));
            Assert.Throws<InvalidDataException>(() => GuidedSetup.Read(path, "pc2", null));
        }
        finally { File.Delete(path); }
    }
    [Theory]
    [InlineData("relative.db")]
    [InlineData("\\\\server\\share\\world.db")]
    [InlineData("C:\\tmp\\world.db:evil.db")]
    public void AmbiguousOrRemotePathsAreRejected(string path) => Assert.Throws<InvalidDataException>(() => GuidedSetup.ValidatePath(path));
    [Fact]
    public async Task CancellingSetupCreatesNothing()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        var output = new StringWriter();
        Assert.Equal(0, await GuidedSetup.RunAsync(["--setup", path], new StringReader("skip\n"), output));
        Assert.False(File.Exists(path));
        Assert.Contains("no cloud provider", output.ToString());
    }
    [Fact]
    public void MissingOfflineAssetsCannotSelectLite()
    {
        Assert.Throws<IOException>(() => GuidedSetup.VerifyLite(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), new(16_000_000_000, 5_000_000_000, 8, true)));
    }
}
