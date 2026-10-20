using Grid.Core;
using Grid.Simulation.Applications;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class ApplicationCatalogTests
{
    [Fact]
    public void CompleteProcessNameInventoryCanProveAbsenceDespiteUnrelatedAccessDenials()
    {
        var catalog = new ApplicationCatalog();
        catalog.AddInstalled([new("editor", "Editor", [], null)
            { ExecutableName = "editor", Activity = ApplicationActivity.Unknown }]);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "protected-service" };
        Assert.Equal(ApplicationActivity.Sleeping,
            Assert.Single(catalog.Observe([], false, new HashSet<ProcessLifetime>(), names)).Activity);
        names.Add("EDITOR");
        Assert.Equal(ApplicationActivity.Unknown,
            Assert.Single(catalog.Observe([], false, new HashSet<ProcessLifetime>(), names)).Activity);
    }

    [Fact]
    public void InstalledExecutableSleepsWhileUnmappedInventoryStaysUnknown()
    {
        var catalog = new ApplicationCatalog();
        catalog.AddInstalled([
            new("executable", "Editor", [], null) { Activity = ApplicationActivity.Unknown },
            new("inventory:unmapped", "Unmapped", [], null) { Activity = ApplicationActivity.Unknown }
        ]);
        var apps = catalog.Observe([], true, new HashSet<ProcessLifetime>());
        Assert.Equal(ApplicationActivity.Sleeping, apps.Single(a => a.Key == "executable").Activity);
        Assert.Equal(ApplicationActivity.Unknown, apps.Single(a => a.Key.StartsWith("inventory:")).Activity);
    }

    [Fact]
    public void PartialObservationDoesNotTurnPreviouslyRunningOrSleepingAppIntoFalseSleep()
    {
        var catalog = new ApplicationCatalog();
        var lifetime = new ProcessLifetime(100, DateTimeOffset.UtcNow.AddMinutes(-1));
        catalog.Observe([new("app", "App", [lifetime], 100)], true, new HashSet<ProcessLifetime>());
        var unknown = Assert.Single(catalog.Observe([], false, new HashSet<ProcessLifetime>()));
        Assert.Equal(ApplicationActivity.Unknown, unknown.Activity);
        Assert.Equal(lifetime, Assert.Single(unknown.Processes));
        var asleep = Assert.Single(catalog.Observe([], true, new HashSet<ProcessLifetime> { lifetime }));
        Assert.Equal(ApplicationActivity.Sleeping, asleep.Activity);
        Assert.Empty(asleep.Processes);
        Assert.Equal(ApplicationActivity.Unknown, Assert.Single(catalog.Observe([], false, new HashSet<ProcessLifetime>())).Activity);
        var awake = Assert.Single(catalog.Observe([new("app", "App", [lifetime], 100) { Activity = ApplicationActivity.OnCall }],
            true, new HashSet<ProcessLifetime>()));
        Assert.Equal(ApplicationActivity.OnCall, awake.Activity);
    }
}
