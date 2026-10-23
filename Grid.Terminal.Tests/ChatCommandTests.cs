using Grid.Core;
using Grid.Communication;
using Grid.Simulation;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class ChatCommandTests
{
    private static (GridSimulationEngine Engine, CommandDispatcher Commands) Fixture()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        return (engine, Commands(engine));
    }
    private static CommandDispatcher Commands(GridSimulationEngine engine)
    {
        var commands = DefaultCommands.Create(engine); ChatCommands.Register(commands, engine.PrepareChatFixture()); return commands;
    }
    [Fact]
    public void DefaultCoordinatorSupportsConversationAndOrdinaryCommands()
    {
        var (engine, commands) = Fixture();
        Assert.Contains(engine.GetSnapshot().Programs, p => p.Name == "MCP");
        Assert.Contains("Connected to MCP", commands.Execute("connect").Text);
        Assert.Contains("VERIFIED GRID STATE", commands.Execute("talk status").Text);
        Assert.Contains("MCP", commands.Execute("talk who are you").Text);
        Assert.Contains("SIMULATED DIALOGUE", commands.Execute("talk delete my files").Text);
        engine.Pause(); commands.Execute("step"); engine.Start(); Assert.Equal(1, engine.TickCount);
        Assert.Contains("delete my files", commands.Execute("transcript").Text);
        Assert.Contains("CONVERSATIONS", commands.Execute("help").Text);
        Assert.Contains("MCP", commands.Execute("conversations").Text);
        Assert.Contains("Ended conversation", commands.Execute("disconnect").Text);
        Assert.Contains("No conversation", commands.Execute("talk hi").Text);
        Assert.Single(engine.GetSnapshot().Programs, p => p.Name == "MCP");
        Assert.Equal(6, engine.GetSnapshot().Communications.Conversations[0].Messages.Count);
    }
    [Fact]
    public void RestoreResumesOpenConversationWithSameOperatorAndCoordinator()
    {
        var (engine, commands) = Fixture(); commands.Execute("connect"); commands.Execute("talk hello");
        var state = engine.GetSnapshot(); var id = state.Communications.Conversations[0].Id;
        var restored = GridSimulationEngine.Restore(state); var next = Commands(restored);
        Assert.Equal(engine.LocalOperator, restored.LocalOperator);
        Assert.Contains("No conversation", next.Execute("talk hi").Text);
        Assert.Contains("Resumed", next.Execute("resume " + id).Text);
        Assert.Contains("VERIFIED", next.Execute("talk status").Text);
        next.Execute("disconnect"); Assert.Contains("has ended", next.Execute("resume " + id).Text);
        next.Execute("connect"); Assert.Single(restored.GetSnapshot().Programs, p => p.Name == "MCP");
        Assert.Equal(4, restored.GetConversation(id).Messages.Count);
    }
    [Fact]
    public void InvalidCommandsAreHelpfulAndDoNotMutateState()
    {
        var (engine, commands) = Fixture();
        Assert.Contains("Error", commands.Execute("connect missing").Text);
        Assert.Contains("Usage", commands.Execute("talk").Text);
        Assert.Contains("Usage", commands.Execute("resume bad").Text);
        Assert.Contains("No conversation", commands.Execute("transcript " + Guid.NewGuid()).Text);
        Assert.Contains("Usage", commands.Execute("disconnect extra").Text);
        Assert.Empty(engine.GetSnapshot().Communications.Conversations);
        commands.Execute("connect");
        Assert.Contains("Already connected", commands.Execute("connect").Text);
        Assert.Contains("Error", commands.Execute("talk \"\"").Text);
        Assert.Empty(engine.GetSnapshot().Communications.Conversations[0].Messages);
    }
    [Fact]
    public void DirectedCivilianDialogueIsLabeledAndUnicodeTranscriptIsPreserved()
    {
        var (engine, commands) = Fixture(); var target = engine.GetSnapshot().Programs[0];
        Assert.Contains("Connected", commands.Execute("connect " + target.Id).Text);
        Assert.Contains("SIMULATED DIALOGUE", commands.Execute("talk \"Hello café 世界\"").Text);
        Assert.Contains("Hello café 世界", commands.Execute("transcript").Text);
        Assert.Contains(engine.GetSnapshot().Programs, p => p.Name == "MCP");
    }
}
