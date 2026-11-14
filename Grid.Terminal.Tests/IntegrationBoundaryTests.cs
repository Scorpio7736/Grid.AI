using System.Reflection;
using Xunit;

namespace Grid.Terminal.Tests;

/// <summary>Prevent provider/policy layers from acquiring host dependencies during migration.</summary>
public sealed class IntegrationBoundaryTests
{
    // This allowlist is deliberately narrower than "any reference without a cycle".
    // For example, AI must never gain a dependency on the otherwise independent Core.
    private static readonly IReadOnlyDictionary<string, string[]> Allowed = new Dictionary<string, string[]>
    {
        ["Grid.Core"] = [],
        ["Grid.AI"] = [],
        ["Grid.Agent"] = ["Grid.AI"],
        ["Grid.Communication"] = ["Grid.AI", "Grid.Agent"],
        ["Grid.Simulation"] = ["Grid.Core", "Grid.Communication", "Grid.Agent", "Grid.AI"],
        ["Grid.Persistence"] = ["Grid.Core", "Grid.Simulation", "Grid.Communication", "Grid.Agent", "Grid.AI"],
        ["Grid.Terminal"] = ["Grid.Core", "Grid.Simulation", "Grid.Persistence", "Grid.Communication", "Grid.Agent", "Grid.AI"]
    };

    [Theory]
    [InlineData("Grid.Core")]
    [InlineData("Grid.AI")]
    [InlineData("Grid.Agent")]
    [InlineData("Grid.Communication")]
    [InlineData("Grid.Simulation")]
    [InlineData("Grid.Persistence")]
    [InlineData("Grid.Terminal")]
    public void CompiledLayerCannotReferenceAnUnapprovedGridAssembly(string name)
    {
        var assembly = Assembly.Load(name);
        foreach (var reference in assembly.GetReferencedAssemblies().Where(r => r.Name!.StartsWith("Grid.", StringComparison.Ordinal)))
            Assert.True(Allowed[name].Contains(reference.Name!, StringComparer.Ordinal),
                $"{name} references {reference.Name}, which violates the integration dependency boundary.");
    }
}
