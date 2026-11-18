using Grid.Core;
using Grid.Simulation;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class DevelopmentBreakGlassRegistryTests
{
    [Fact]
    public void RegistryIsVersionedUniqueAndSeparateFromVisibleRecoveryPaths()
    {
        DevelopmentBreakGlassRegistry.Validate();

        Assert.True(DevelopmentBreakGlassRegistry.Version > 0);
        Assert.Contains(DevelopmentBreakGlassRegistry.VisibleRecoveryPaths, path => path.Syntax == "nge");
        Assert.Contains(DevelopmentBreakGlassRegistry.VisibleRecoveryPaths, path => path.Syntax.StartsWith("Grid.Terminal --recover", StringComparison.Ordinal));
        Assert.All(DevelopmentBreakGlassRegistry.HiddenContracts,
            contract => Assert.True(DevelopmentBreakGlassRegistry.IsReservedInput(contract.Syntax.Split(' ')[0])));
    }

    [Fact]
    public void ReservedFamiliesCannotEnterOrdinaryCommandRegistry()
    {
        var commands = new CommandDispatcher();

        var error = Assert.Throws<ArgumentException>(() => commands.Register(
            new("::grid-undocumented", "::grid-undocumented", "test", _ => new("unsafe"))));

        Assert.Contains("cannot be registered", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(commands.Commands);
    }

    [Fact]
    public void ReservedInputIsGenericallyDeniedAndNeverRoutedToConversation()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        var commands = DefaultCommands.Create(engine);
        var talked = false;
        commands.Register(new("talk", "talk <message>", "test", _ => { talked = true; return new("talked"); }) { Capability = "chat" });
        commands.ConversationConnected = () => true;

        var result = commands.ExecuteUserInput("::grid-breakglass status");

        Assert.Equal(DevelopmentBreakGlassRegistry.GenericDenial, result.Text);
        Assert.False(talked);
    }

    [Fact]
    public void ReservedNamesAreAbsentFromHelpCompletionAndOrdinaryCommands()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        var commands = DefaultCommands.Create(engine);
        var help = commands.Execute("help").Text;
        var completions = CommandHelp.Complete("::grid-", commands.Commands);

        Assert.DoesNotContain("::grid-", help, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(completions);
        Assert.DoesNotContain(commands.Commands, command => DevelopmentBreakGlassRegistry.IsReservedInput(command.Name));
    }
}
