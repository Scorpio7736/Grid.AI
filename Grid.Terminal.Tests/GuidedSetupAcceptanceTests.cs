using System.Text.Json;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class GuidedSetupAcceptanceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "GRID setup ' test " + Guid.NewGuid());
    public GuidedSetupAcceptanceTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root, true);
    private string World => Path.Combine(root, "world.db");

    [Fact]
    public async Task ReviewedProfileLaunchKeepsIdentityOutOfProfileAndWorldUnopened()
    {
        var path = Path.Combine(root, "preferences.json");
        var output = new StringWriter(); var launched = false;
        // Test outputs are under the source tree, so the clone installer choice is offered.
        var input = $"pc\n{World}\nkeep\noff\nreview\nsave\nlaunch\n";
        var result = await GuidedSetup.RunAsync(["--setup", path], new StringReader(input), output, p =>
        {
            Assert.Equal(path, p); Assert.False(File.Exists(World)); launched = true; return Task.FromResult(0);
        });
        Assert.Equal(0, result); Assert.True(launched);
        Assert.Equal("off", GuidedSetup.Read(path, Environment.MachineName, null).AiMode);
        Assert.False(File.Exists(World)); Assert.Contains("not password recovery", output.ToString());
        Assert.Contains("screen stop", output.ToString()); Assert.Contains("no grants", output.ToString());
        using var profile = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal(5, profile.RootElement.EnumerateObject().Count());
    }

    [Theory]
    [InlineData("skip")]
    [InlineData("")]
    public async Task DecliningSaveOrEofNeverLaunchesOrTouchesExistingWorld(string answer)
    {
        File.WriteAllText(World, "existing world");
        var path = Path.Combine(root, "preferences.json"); var launches = 0;
        Assert.Equal(0, await GuidedSetup.RunAsync(["--setup", path],
            new StringReader($"pc\n{World}\nkeep\noff\nskip\n{answer}\n"), new StringWriter(), _ => { launches++; return Task.FromResult(0); }));
        Assert.Equal(0, launches); Assert.False(File.Exists(path)); Assert.Equal("existing world", File.ReadAllText(World));
    }

    [Fact]
    public void FirstRunOfferNeverInterceptsAutomationOrExistingWorld()
    {
        Assert.True(GuidedSetup.OfferFirstRun([], true, World));
        Assert.False(GuidedSetup.OfferFirstRun([], false, World));
        Assert.False(GuidedSetup.OfferFirstRun(["--world", World], true, World));
        File.WriteAllText(World, "existing");
        Assert.False(GuidedSetup.OfferFirstRun([], true, World));
    }

    [Fact]
    public async Task InstallerRequiresExactReviewAndNeverRetriesOfflineFailureOnline()
    {
        var plan = new SetupInstallPlan(root, false, 123, 1000, 2000);
        var calls = 0;
        Task<int> Execute(System.Diagnostics.ProcessStartInfo command)
        {
            calls++; Assert.DoesNotContain("-Download", command.ArgumentList);
            Assert.Contains(Path.Combine(root, "Setup-GridClone.ps1"), command.ArgumentList);
            Assert.False(command.UseShellExecute); return Task.FromResult(1);
        }
        Assert.False(await SetupInstallation.ReviewAsync(plan, new StringReader("yes\n"), new StringWriter(), Execute));
        Assert.Equal(0, calls);
        var error = await Assert.ThrowsAsync<IOException>(() => SetupInstallation.ReviewAsync(plan, new StringReader("install\n"), new StringWriter(), Execute));
        Assert.Contains("never retry online", error.Message); Assert.Equal(1, calls);
        Assert.Empty(Directory.GetFileSystemEntries(root));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(999L)]
    public async Task UnknownOrLowSpaceCannotStartInstaller(long? available)
    {
        var calls = 0;
        await Assert.ThrowsAsync<IOException>(() => SetupInstallation.ReviewAsync(new(root, true, 0, 1000, available),
            new StringReader("install\n"), new StringWriter(), _ => { calls++; return Task.FromResult(0); }));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task OnlineFlagIsOnlyPassedAfterReviewedOnlinePlan()
    {
        Assert.True(await SetupInstallation.ReviewAsync(new(root, true, 0, 1000, 2000),
            new StringReader("install\n"), new StringWriter(), command =>
            { Assert.Contains("-Download", command.ArgumentList); return Task.FromResult(0); }));
    }

    [Fact]
    public void InventoryIncludesRetainedAndStagedFiles()
    {
        File.WriteAllText(Path.Combine(root, "Setup-GridClone.ps1"), "fixture");
        foreach (var folder in new[] { ".grid-runtime", ".grid-data", ".grid-app", ".grid-install-old", ".grid-install-retained-a" })
        { Directory.CreateDirectory(Path.Combine(root, folder)); File.WriteAllBytes(Path.Combine(root, folder, "file"), new byte[10]); }
        var plan = SetupInstallation.Plan(root, true);
        Assert.Equal(50, plan.ExistingBytes); Assert.Equal(20_000_000_000, plan.RequiredFreeBytes);
    }

    [Fact]
    public void StorageProbePreservesWorldAndCleansTemporaryFile()
    {
        File.WriteAllText(World, "sentinel");
        Assert.True(SetupHealth.Storage(World).Passed);
        Assert.Equal("sentinel", File.ReadAllText(World)); Assert.Single(Directory.GetFiles(root));
        File.SetAttributes(World, FileAttributes.ReadOnly);
        try { Assert.False(SetupHealth.Storage(World).Passed); }
        finally { File.SetAttributes(World, FileAttributes.Normal); }
        Assert.Single(Directory.GetFiles(root));
    }

    [Fact]
    public async Task HealthReportsFailuresIndependentlyAndNeverOpensWorld()
    {
        var checks = await SetupHealth.RunAsync(null, World, default,
            _ => throw new IOException("No recognizer"), _ => Task.FromResult("synthetic OCR passed"),
            _ => throw new IOException("Network blocked; missing local model"), () => "metadata only");
        Assert.Equal(5, checks.Count); Assert.Equal(2, checks.Count(c => !c.Passed));
        Assert.Contains(checks, c => c.Name == "Screen OCR" && c.Passed);
        Assert.False(File.Exists(World)); Assert.Empty(Directory.GetFiles(root));
        Assert.Contains("No global/cloud fallback", SetupHealth.Format(checks));
    }

    [Fact]
    public async Task CancellationIsNotReportedAsSuccessfulProbe()
    {
        using var stop = new CancellationTokenSource(); stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SetupHealth.RunAsync(null, World, stop.Token,
            t => Task.FromCanceled<string>(t)));
    }

    [Theory]
    [InlineData(false, "")]
    [InlineData(true, "unrelated text")]
    public void OcrHelperExitSuccessDoesNotMaskFailedRecognition(bool available, string text) =>
        Assert.Throws<IOException>(() => SetupHealth.ValidateOcr(JsonSerializer.Serialize(new ScreenObservation(available, text, "fixture", DateTimeOffset.UtcNow))));

    [Fact]
    public void OcrAndVoiceValidateActualCapabilities()
    {
        Assert.Contains("No desktop captured", SetupHealth.ValidateOcr(JsonSerializer.Serialize(new ScreenObservation(true, "GRID SCREEN COBALT 42", "fixture", DateTimeOffset.UtcNow))));
        Assert.Throws<IOException>(() => SetupHealth.ValidateVoice("Offline recognizers: \nInstalled voices: David"));
        Assert.Throws<IOException>(() => SetupHealth.ValidateVoice("Offline recognizers: en-US\nInstalled voices: "));
        Assert.Contains("not activated", SetupHealth.ValidateVoice("Offline recognizers: en-US\nInstalled voices: David"));
    }

    [Fact]
    public async Task HealthRequiresExplicitProfileAndRejectsChangedDeviceBeforeProbes()
    {
        Assert.Equal(1, await GuidedSetup.RunAsync(["--doctor-check"], new StringReader(""), new StringWriter()));
        var path = Path.Combine(root, "profile.json");
        GuidedSetup.SaveNew(path, new(1, "another-device", "pc", World, "off"));
        var output = new StringWriter();
        Assert.Equal(1, await GuidedSetup.RunAsync(["--doctor-check", path], new StringReader(""), output));
        Assert.Contains("changed device", output.ToString()); Assert.False(File.Exists(World));
    }
}
