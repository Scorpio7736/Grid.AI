using System.Diagnostics;
using System.Text.Json;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class FreshCloneSetupTests
{
    private sealed class Clone : IDisposable
    {
        public readonly string Root = Path.Combine(Path.GetTempPath(), "Grid clone ' fixture-" + Guid.NewGuid());
        public Clone()
        {
            var source = new DirectoryInfo(AppContext.BaseDirectory);
            while (source is not null && !File.Exists(Path.Combine(source.FullName, "Grid7736.slnx"))) source = source.Parent;
            Assert.NotNull(source); Directory.CreateDirectory(Root);
            foreach (var file in new[] { "Setup-GridClone.ps1", "Start-GridClone.ps1", "Grid-Clone-Dependencies.json" }) File.Copy(Path.Combine(source!.FullName, file), Path.Combine(Root, file));
        }
        public async Task<(int Exit, string Output)> Run(string script, params string[] args)
        {
            var start = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Root };
            foreach (var arg in new[] { "-NoProfile", "-NonInteractive", "-File", Path.Combine(Root, script) }.Concat(args)) start.ArgumentList.Add(arg);
            start.Environment.Remove("GRID_AI_DISABLED");
            // PowerShell 7's inherited module paths can hide Windows PowerShell's built-in modules.
            start.Environment.Remove("PSModulePath");
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try { await process.WaitForExitAsync(deadline.Token); }
            catch { if (!process.HasExited) process.Kill(true); throw; }
            return (process.ExitCode, await output + await errors);
        }
        public void Dispose() => Directory.Delete(Root, true);
    }
    [Fact]
    public void TrackedModelManifestAndMetadataMatchRuntimePins()
    {
        var source = new DirectoryInfo(AppContext.BaseDirectory);
        while (source is not null && !File.Exists(Path.Combine(source.FullName, "Grid7736.slnx"))) source = source.Parent;
        Assert.NotNull(source);
        using var dependencies = JsonDocument.Parse(File.ReadAllText(Path.Combine(source!.FullName, "Grid-Clone-Dependencies.json")));
        var model = dependencies.RootElement.GetProperty("Model");
        var manifest = Path.Combine(source.FullName, model.GetProperty("Manifest").GetString()!);
        Assert.Equal(model.GetProperty("ManifestSha256").GetString(), GridTransfer.Hash(manifest));
        Assert.Equal("359D7DD4BCDAB3D86B87D73AC27966F4DBB9F5EFDFCC75D34A8764A09474FAE7", GridTransfer.Hash(Path.Combine(source.FullName, model.GetProperty("MetadataBlob").GetString()!)));
        using var parsed = JsonDocument.Parse(File.ReadAllText(manifest));
        Assert.Contains(parsed.RootElement.GetProperty("layers").EnumerateArray(), layer => layer.GetProperty("digest").GetString() == "sha256:3e4cb14174460404e7a233e531675303b2fbf7749c02f91864fe311ab6344e4f");
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PlanIsReadOnlyAndNetworkRequiresExplicitFlag(bool download)
    {
        if (!OperatingSystem.IsWindows()) return;
        using var clone = new Clone();
        var result = await clone.Run("Setup-GridClone.ps1", download ? ["-Plan", "-Download"] : ["-Plan"]);
        Assert.Equal(0, result.Exit);
        using var json = JsonDocument.Parse(result.Output);
        Assert.Equal(download, json.RootElement.GetProperty("Network").GetBoolean());
        Assert.Equal(Path.Combine(clone.Root, ".grid-data", "world.db"), json.RootElement.GetProperty("World").GetString());
        Assert.Empty(Directory.GetDirectories(clone.Root));
    }
    [Fact]
    public async Task MissingDependenciesNeverImportOrOverwriteWorld()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var clone = new Clone(); var folder = Path.Combine(clone.Root, ".grid-data"); Directory.CreateDirectory(folder);
        var world = Path.Combine(folder, "world.db"); File.WriteAllText(world, "retained world sentinel");
        var result = await clone.Run("Setup-GridClone.ps1");
        Assert.NotEqual(0, result.Exit); Assert.Contains("Dependencies are absent", result.Output);
        Assert.Equal("retained world sentinel", File.ReadAllText(world));
        Assert.False(Directory.Exists(Path.Combine(clone.Root, ".grid-runtime")));
        Assert.False(Directory.Exists(Path.Combine(clone.Root, ".grid-app")));
    }
    [Fact]
    public async Task LauncherCannotStartUnverifiedInstallOrCreateWorld()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var clone = new Clone(); var result = await clone.Run("Start-GridClone.ps1");
        Assert.NotEqual(0, result.Exit); Assert.Contains("Run Setup-GridClone.ps1 first", result.Output);
        Assert.False(Directory.Exists(Path.Combine(clone.Root, ".grid-data")));
    }
    [Fact]
    public async Task PartialRuntimeProvidesRepairCommandWithoutChangingFiles()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var clone = new Clone();
        var runtime = Path.Combine(clone.Root, ".grid-runtime"); Directory.CreateDirectory(runtime);
        var sentinel = Path.Combine(runtime, "keep.txt"); File.WriteAllText(sentinel, "retain");
        var result = await clone.Run("Setup-GridClone.ps1");
        Assert.NotEqual(0, result.Exit); Assert.Contains("-Download -RepairRuntime", result.Output);
        var refused = await clone.Run("Setup-GridClone.ps1", "-RepairRuntime");
        Assert.NotEqual(0, refused.Exit); Assert.Contains("requires -Download", refused.Output);
        Assert.Equal("retain", File.ReadAllText(sentinel));
        Assert.Empty(Directory.GetDirectories(clone.Root, ".grid-install-*"));
    }
    [Fact]
    public async Task CorruptModelStopsBeforeBuildOrExecutableRuns()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var clone = new Clone();
        foreach (var file in new[] { "dotnet/dotnet.exe", "ollama/ollama.exe", "assets.json", "models/manifests/registry.ollama.ai/library/qwen3/4b" })
        {
            var path = Path.Combine(clone.Root, ".grid-runtime", file); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, "invalid fixture");
        }
        var result = await clone.Run("Setup-GridClone.ps1");
        Assert.NotEqual(0, result.Exit); Assert.True(result.Output.Contains("Pinned model manifest verification failed", StringComparison.Ordinal), result.Output);
        Assert.False(Directory.Exists(Path.Combine(clone.Root, ".grid-app")));
    }
}
