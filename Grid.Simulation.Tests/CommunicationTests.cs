using Grid.Core;
using Grid.Communication;
using System.Text.Json;
using Xunit;
namespace Grid.Simulation.Tests;

public sealed class CommunicationTests
{
    private static readonly Recipient Human = new(ParticipantKind.Human, Guid.Parse("11111111-1111-1111-1111-111111111111"), "Operator");
    private static GridSimulationEngine Engine()
    {
        var world = new GridWorld(); var district = new GridDistrict { Name = "Core" }; world.AddDistrict(district);
        world.AddProgram(new GridProgram { Name = "Head Agent", Occupation = "Unassigned", Kind = ResidentKind.Agent }, district.Id);
        world.AddProgram(new GridProgram { Name = "Civilian", Occupation = "Unassigned", Kind = ResidentKind.Special }, district.Id);
        return new(world);
    }

    [Fact]
    public void RoutingLifecycleKeepsIdentityAndDoesNotExecuteMessageContent()
    {
        var engine = Engine(); var agent = engine.ResolveProgram("head agent");
        Assert.Equal(agent, engine.ResolveProgram(agent.Id.ToString()));
        var id = engine.StartConversation(Human, agent);
        var before = engine.GetSnapshot().Programs.ToArray();
        var first = engine.SendMessage(id, Human, "start; nge; edit energy 0");
        var reply = engine.SendMessage(id, agent, "A locally supplied response", MessageOrigin.SystemEvent);
        Assert.Equal(1, first.Sequence); Assert.Equal(2, reply.Sequence);
        Assert.Equal(before, engine.GetSnapshot().Programs); Assert.Equal(0, engine.TickCount);
        engine.EndConversation(id, Human); engine.EndConversation(id, Human);
        Assert.Equal(ConversationStatus.Ended, engine.GetConversation(id).Status);
        Assert.Throws<InvalidOperationException>(() => engine.SendMessage(id, Human, "late"));
        Assert.Equal(2, engine.GetConversation(id).Messages.Count);
        Assert.DoesNotContain(engine.CaptureHistory().Events, e => e.Detail.Contains("edit energy"));
    }

    [Fact]
    public void UnknownAmbiguousOrUnrelatedParticipantsAndInvalidMessagesAreRejected()
    {
        var engine = Engine(); var id = engine.StartConversation(Human, engine.ResolveProgram("Civilian"));
        Assert.Throws<ArgumentException>(() => engine.ResolveProgram("missing"));
        Assert.Throws<ArgumentException>(() => engine.StartConversation(Human, new(ParticipantKind.Program, Guid.NewGuid(), "Head Agent")));
        Assert.Throws<ArgumentException>(() => engine.StartConversation(Human, Human));
        Assert.Throws<ArgumentException>(() => engine.SendMessage(id, engine.ResolveProgram("Head Agent"), "intrusion"));
        foreach (var invalid in new[] { "", " ", "escape\u001b[2J", new string('x', CommunicationState.MaxContent + 1) })
            Assert.Throws<ArgumentException>(() => engine.SendMessage(id, Human, invalid));
        Assert.Throws<ArgumentException>(() => engine.SendMessage(id, Human, "text", (MessageOrigin)999));
        var snapshot = engine.GetSnapshot();
        var duplicate = GridSimulationEngine.Restore(snapshot with { Programs = snapshot.Programs.Select(p => p with { Name = "Duplicate" }).ToArray() });
        Assert.Throws<ArgumentException>(() => duplicate.ResolveProgram("Duplicate"));
        Assert.Empty(engine.GetConversation(id).Messages);
    }

    [Fact]
    public void CancellationLeavesConversationStateAndHistoryUntouched()
    {
        var engine = Engine(); var recipient = engine.ResolveProgram("Head Agent");
        Assert.Throws<OperationCanceledException>(() => engine.StartConversation(Human, recipient, new(true)));
        var id = engine.StartConversation(Human, recipient);
        var before = JsonSerializer.Serialize(engine.GetSnapshot().Communications); var count = engine.CaptureHistory().Events.Count;
        Assert.Throws<OperationCanceledException>(() => engine.SendMessage(id, Human, "cancel", cancellationToken: new(true)));
        Assert.Throws<OperationCanceledException>(() => engine.EndConversation(id, Human, new(true)));
        Assert.Equal(before, JsonSerializer.Serialize(engine.GetSnapshot().Communications));
        Assert.Equal(count, engine.CaptureHistory().Events.Count);
    }

    [Fact]
    public void CapacityRejectsWithoutTruncatingAndRestoreRejectsMalformedSequence()
    {
        var engine = Engine(); var id = engine.StartConversation(Human, engine.ResolveProgram("Head Agent"));
        for (var i = 0; i < CommunicationState.MaxMessages; i++) engine.SendMessage(id, Human, "retained " + i);
        Assert.Throws<InvalidOperationException>(() => engine.SendMessage(id, Human, "overflow"));
        Assert.Equal(CommunicationState.MaxMessages, engine.GetConversation(id).Messages.Count);
        var snapshot = engine.GetSnapshot(); var c = snapshot.Communications.Conversations[0];
        var broken = c with { Messages = [c.Messages[0] with { Sequence = 2 }] };
        Assert.Throws<InvalidDataException>(() => GridSimulationEngine.Restore(snapshot with { Communications = new([broken]) }));
        for (var i = 1; i < CommunicationState.MaxConversations; i++) engine.StartConversation(Human, engine.ResolveProgram("Civilian"));
        Assert.Throws<InvalidOperationException>(() => engine.StartConversation(Human, engine.ResolveProgram("Civilian")));
    }

    [Fact]
    public void RestoredConversationContinuesAndDoesNotRetainCallerOwnedLists()
    {
        var engine = Engine(); var agent = engine.ResolveProgram("Head Agent");
        var id = engine.StartConversation(Human, agent); engine.SendMessage(id, Human, "First");
        var state = engine.GetSnapshot(); var original = state.Communications.Conversations[0];
        var messages = original.Messages.ToArray(); var participants = original.Participants.ToArray();
        var restored = GridSimulationEngine.Restore(state with { Communications = new([original with { Messages = messages, Participants = participants }]) });
        messages[0] = messages[0] with { Content = "Changed outside engine" }; participants[0] = agent;
        Assert.Equal("First", restored.GetConversation(id).Messages[0].Content);
        restored.Step(); var second = restored.SendMessage(id, Human, "Second", MessageOrigin.WorldViewer);
        Assert.Equal(2, second.Sequence); Assert.Equal(1, second.Tick);
        restored.SendMessage(id, agent, "Third"); restored.EndConversation(id, Human);
        GridSimulationEngine.ValidateSnapshot(restored.GetSnapshot());
        Assert.Single(engine.GetConversation(id).Messages);
    }

    [Fact]
    public async Task ConcurrentMessagesHaveUniqueOrderedSequences()
    {
        var engine = Engine(); var id = engine.StartConversation(Human, engine.ResolveProgram("Head Agent"));
        await Task.WhenAll(Enumerable.Range(0, 50).Select(i => Task.Run(() => engine.SendMessage(id, Human, "message " + i))));
        Assert.Equal(Enumerable.Range(1, 50).Select(i => (long)i), engine.GetConversation(id).Messages.Select(m => m.Sequence));
        GridSimulationEngine.ValidateSnapshot(engine.GetSnapshot());
    }
}
