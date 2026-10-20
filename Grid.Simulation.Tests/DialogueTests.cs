using Grid.Core;
using Grid.Communication;
using Xunit;
namespace Grid.Simulation.Tests;
public sealed class DialogueTests
{
    [Fact]
    public void TurnIsAtomicAtCapacityAndCancellationAndDoesNotExecuteRequests()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        var coordinator = engine.EnsureCoordinator(); var id = engine.StartConversation(engine.LocalOperator, coordinator);
        Assert.Throws<OperationCanceledException>(() => engine.Talk(id, engine.LocalOperator, "status", new(true)));
        Assert.Empty(engine.GetConversation(id).Messages);
        var pair = engine.Talk(id, engine.LocalOperator, "start");
        Assert.Contains("No task", pair[1].Content); Assert.False(engine.IsRunning); Assert.Equal(0, engine.TickCount);
        for (var i = 2; i < CommunicationState.MaxMessages - 1; i++) engine.SendMessage(id, engine.LocalOperator, "filler");
        Assert.Throws<InvalidOperationException>(() => engine.Talk(id, engine.LocalOperator, "status"));
        Assert.Equal(CommunicationState.MaxMessages - 1, engine.GetConversation(id).Messages.Count);
    }
    [Fact]
    public void CoordinatorNameCollisionDoesNotPromoteExistingCitizen()
    {
        var world = new GridWorld(); var district = new GridDistrict { Name = "Core" }; world.AddDistrict(district);
        var civilian = new GridProgram { Name = "ARCHON", Occupation = "Unassigned", Kind = ResidentKind.Special };
        world.AddProgram(civilian, district.Id); var engine = new GridSimulationEngine(world);
        Assert.Throws<InvalidOperationException>(() => engine.EnsureCoordinator());
        Assert.Equal(ResidentKind.Special, engine.GetSnapshot().Programs.Single().Kind);
    }
    [Fact]
    public void RepliesAreDeterministicAndOutsidersCannotWriteTurns()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis()); var agent = engine.EnsureCoordinator();
        var id = engine.StartConversation(engine.LocalOperator, agent);
        var restored = GridSimulationEngine.Restore(engine.GetSnapshot());
        Assert.Equal(engine.Talk(id, engine.LocalOperator, "status")[1].Content, restored.Talk(id, restored.LocalOperator, "status")[1].Content);
        Assert.Throws<ArgumentException>(() => engine.Talk(id, new(ParticipantKind.Human, Guid.NewGuid(), "Other"), "hi"));
        engine.EndConversation(id, engine.LocalOperator);
        Assert.Throws<InvalidOperationException>(() => engine.Talk(id, engine.LocalOperator, "hi"));
    }
}
