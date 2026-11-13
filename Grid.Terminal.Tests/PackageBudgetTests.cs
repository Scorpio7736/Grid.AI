using Xunit;

namespace Grid.Terminal.Tests;

public sealed class PackageBudgetTests
{
    [Theory]
    [InlineData("runtime/models/blob")]
    [InlineData("runtime/ollama/lib/runner.dll")]
    [InlineData("runtime/home/cache")]
    [InlineData("runtime/temp/cache")]
    [InlineData("old-versions/release/runtime/models/blob")]
    [InlineData("staging/update/runtime/models/blob")]
    public void ModelsDependenciesCachesAndRetainedVersionsShareModelLimit(string path)
    {
        var allowed = PackageBudget.Inventory(new[] { (path, PackageBudget.ModelLimit) });
        PackageBudget.RequireBase(allowed);
        Assert.Equal(PackageBudget.ModelLimit, allowed.ModelBytes);
        var refused = PackageBudget.Inventory(new[] { (path, PackageBudget.ModelLimit + 1) });
        Assert.Throws<IOException>(() => PackageBudget.RequireBase(refused));
    }

    [Fact]
    public void DotnetAndSavedDataCountOnlyTowardInstallationTarget()
    {
        var size = PackageBudget.Inventory(new[] { ("runtime/dotnet/sdk", 10L), ("data/world.db", 20L), ("runtime/models/blob", 30L) });
        Assert.Equal(60, size.TotalBytes);
        Assert.Equal(30, size.ModelBytes);
        Assert.Equal(20, size.MutableBytes);
    }

    [Theory]
    [InlineData("Flash")]
    [InlineData("PC")]
    public void AllModesRequireActualFreeSpaceIncludingReserve(string mode)
    {
        var size = PackageBudget.Inventory(new[] { ("app/" + mode, 1000L), ("data/world.db", 2000L) });
        PackageBudget.RequireSpace(size, 3000 + PackageBudget.Reserve);
        Assert.Throws<IOException>(() => PackageBudget.RequireSpace(size, 2999 + PackageBudget.Reserve));
    }

    [Fact]
    public void MutableAndStagedGrowthCannotBypassInstallationCeiling()
    {
        var size = PackageBudget.Inventory(new[] { ("app/main", 20_000_000_000L), ("data/history", 80_000_000_000L), ("staging/update", 30_000_000_000L) });
        Assert.Throws<IOException>(() => PackageBudget.RequireBase(size));
    }
}
