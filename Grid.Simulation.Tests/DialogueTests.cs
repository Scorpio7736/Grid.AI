using Grid.Core;
using Grid.Communication;
using Xunit;
namespace Grid.Simulation.Tests;
public sealed class DialogueTests
{
    [Fact]
    public void LegacyAgentRenamesPreserveIdsRegistrationAndHistoricalMessages()
    {
        var world = new GridWorld(); var district = new GridDistrict { Name = "Core" }; world.AddDistrict(district);
        foreach (var name in new[] { "ARCHON", "FORGE", "ARCHIVIST" }) world.AddProgram(new GridProgram { Name = name, Occupation = "Unassigned", Kind = ResidentKind.Agent }, district.Id);
        var engine = new GridSimulationEngine(world); engine.Start(); var old = engine.ResolveProgram("ARCHON");
        engine.BindLocalOwner("test-owner"); engine.RegisterAgentIdentity(old.Id);
        var conversation = engine.StartConversation(engine.LocalOperator, old); engine.Talk(conversation, engine.LocalOperator, "hello ARCHON");
        var state = engine.GetSnapshot();
        var restored = GridSimulationEngine.Restore(state);
        Assert.Equal(old.Id, restored.CreateRunningCoordinator().Id);
        Assert.Equal(new[] { "JN", "MAXIS", "MCP" }, restored.GetSnapshot().Programs.Select(p => p.Name).Order());
        Assert.Contains(old.Id, restored.GetSnapshot().Identities.RegisteredAgents);
        Assert.Contains(restored.GetConversation(conversation).Participants, p => p.Id == old.Id && p.Name == "MCP");
        Assert.Equal("hello ARCHON", restored.GetConversation(conversation).Messages[0].Content);
        Assert.Equal("ARCHON", restored.GetConversation(conversation).Messages[1].Sender.Name);
        var again = GridSimulationEngine.Restore(restored.GetSnapshot());
        Assert.Equal(restored.GetSnapshot().Programs.Select(p => p.Revision), again.GetSnapshot().Programs.Select(p => p.Revision));
    }
    [Fact]
    public void RenameCollisionDoesNotMergeOrPromoteResidents()
    {
        var world = new GridWorld(); var district = new GridDistrict { Name = "Core" }; world.AddDistrict(district);
        world.AddProgram(new GridProgram { Name = "ARCHON", Occupation = "Unassigned", Kind = ResidentKind.Agent }, district.Id);
        world.AddProgram(new GridProgram { Name = "MCP", Occupation = "Unassigned", Kind = ResidentKind.Special }, district.Id);
        var restored = GridSimulationEngine.Restore(new GridSimulationEngine(world).GetSnapshot());
        Assert.Equal(2, restored.GetSnapshot().Programs.Count);
        Assert.Equal(ResidentKind.Agent, restored.GetSnapshot().Programs.Single(p => p.Name == "ARCHON").Kind);
        Assert.Throws<InvalidOperationException>(() => restored.CreateRunningCoordinator());
        Assert.Contains(restored.CaptureHistory().Events, e => e.Kind == "AgentRenameSkipped");
    }
    [Fact]
    public void TurnIsAtomicAtCapacityAndCancellationAndDoesNotExecuteRequests()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        var coordinator = engine.CreateRunningCoordinator(); var id = engine.StartConversation(engine.LocalOperator, coordinator);
        Assert.Throws<OperationCanceledException>(() => engine.Talk(id, engine.LocalOperator, "status", new(true)));
        Assert.Empty(engine.GetConversation(id).Messages);
        var pair = engine.Talk(id, engine.LocalOperator, "start");
        Assert.Contains("No task", pair[1].Content); Assert.True(engine.IsRunning); Assert.Equal(0, engine.TickCount);
        for (var i = 2; i < CommunicationState.MaxMessages - 1; i++) engine.SendMessage(id, engine.LocalOperator, "filler");
        Assert.Throws<InvalidOperationException>(() => engine.Talk(id, engine.LocalOperator, "status"));
        Assert.Equal(CommunicationState.MaxMessages - 1, engine.GetConversation(id).Messages.Count);
    }
    [Fact]
    public void CoordinatorNameCollisionDoesNotPromoteExistingCitizen()
    {
        var world = new GridWorld(); var district = new GridDistrict { Name = "Core" }; world.AddDistrict(district);
        var civilian = new GridProgram { Name = "MCP", Occupation = "Unassigned", Kind = ResidentKind.Special };
        world.AddProgram(civilian, district.Id); var engine = new GridSimulationEngine(world);
        Assert.Throws<InvalidOperationException>(() => engine.CreateRunningCoordinator());
        Assert.Equal(ResidentKind.Special, engine.GetSnapshot().Programs.Single().Kind);
    }
    [Fact]
    public void RepliesAreDeterministicAndOutsidersCannotWriteTurns()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis()); var agent = engine.CreateRunningCoordinator();
        var id = engine.StartConversation(engine.LocalOperator, agent);
        var restored = GridSimulationEngine.Restore(engine.GetSnapshot()); restored.Start();
        Assert.Equal(engine.Talk(id, engine.LocalOperator, "status")[1].Content, restored.Talk(id, restored.LocalOperator, "status")[1].Content);
        Assert.Throws<ArgumentException>(() => engine.Talk(id, new(ParticipantKind.Human, Guid.NewGuid(), "Other"), "hi"));
        engine.EndConversation(id, engine.LocalOperator);
        Assert.Throws<InvalidOperationException>(() => engine.Talk(id, engine.LocalOperator, "hi"));
    }
}
