using System.Text.Json;
using Grid.Agent;
using Grid.AI;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class AgentMemoryMigrationTests
{
    [Fact]
    public void LegacyMemoryJsonRetainsShapeAndDefaultsWithoutRuntimeTypeMetadata()
    {
        const string json = """
        {"Id":"11111111-1111-1111-1111-111111111111","AgentId":"22222222-2222-2222-2222-222222222222","Project":"general","Key":"release","Kind":2,"Content":"Cobalt","UpdatedAtUtc":"2026-01-01T00:00:00+00:00","Sources":[{"ConversationId":"33333333-3333-3333-3333-333333333333","MessageId":"44444444-4444-4444-4444-444444444444"}],"Forgotten":false}
        """;
        var memory = JsonSerializer.Deserialize<ConversationMemory>(json)!;
        Assert.Equal(MemoryKind.Fact, memory.Kind); Assert.Empty(memory.Grants); Assert.Null(memory.Provenance);
        Assert.False(memory.IsStale(memory.UpdatedAtUtc.AddDays(29)));
        Assert.True(memory.IsStale(memory.UpdatedAtUtc.AddDays(30)));
        var stored = JsonSerializer.Serialize(memory);
        Assert.DoesNotContain("Grid.Agent", stored); Assert.DoesNotContain("$type", stored);
        Assert.Equal(memory.Content, JsonSerializer.Deserialize<ConversationMemory>(stored)!.Content);
    }
    [Fact]
    public void AgentPolicyRejectsWrongScopeRevokedExpiredAndForgottenShares()
    {
        var owner = Guid.NewGuid(); var recipient = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        var memory = new ConversationMemory(Guid.NewGuid(), owner, "owner", "release", MemoryKind.Fact, "Cobalt", now, [])
            { Grants = [new(recipient, "project")], ExpiresAtUtc = now.AddMinutes(1) };
        Assert.Single(MemoryPolicy.Relevant([memory], recipient, "project", "remember", now));
        Assert.Empty(MemoryPolicy.Relevant([memory], recipient, "other", "remember", now));
        Assert.Empty(MemoryPolicy.Relevant([memory], Guid.NewGuid(), "project", "remember", now));
        Assert.Empty(MemoryPolicy.Relevant([memory], recipient, "project", "remember", now.AddMinutes(1)));
        Assert.Empty(MemoryPolicy.Relevant([memory with { Forgotten = true }], recipient, "project", "remember", now));
        Assert.Empty(MemoryPolicy.Relevant([memory with { Grants = [new(recipient, "project", true)] }], recipient, "project", "remember", now));
    }
    [Fact]
    public void ScopeComparisonIncludesParticipantKindsMeetingAndProject()
    {
        var id = Guid.NewGuid();
        Assert.True(MemoryPolicy.SameRecallScope("p", false, [(0,id)], "p", false, [(0,id)]));
        Assert.False(MemoryPolicy.SameRecallScope("p", false, [(0,id)], "p", false, [(1,id)]));
        Assert.False(MemoryPolicy.SameRecallScope("p", false, [(0,id)], "p", true, [(0,id)]));
        Assert.False(MemoryPolicy.SameRecallScope("p", false, [(0,id)], "q", false, [(0,id)]));
    }
    [Fact]
    public void GenericExtractiveSummaryReturnsOriginalRecordsInSequenceOrder()
    {
        var messages = new[] { (Seq: 1L, Text: "old background"), (Seq: 2L, Text: "Cobalt release"), (Seq: 3L, Text: "Cobalt detail"), (Seq: 4L, Text: "unrelated") };
        var excerpts = RecallText.SummaryExcerpts(messages, "Cobalt", m => m.Text, m => m.Seq);
        Assert.Equal(new[] { messages[1], messages[2] }, excerpts);
        Assert.Contains("Cobalt", RecallText.Excerpt(new string('x',500) + " Cobalt", "Cobalt"));
    }
}
