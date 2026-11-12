using Grid.AI;
using Grid.Communication;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class InferenceCacheTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 11, 12, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class Backend : IInferenceBackend
    {
        public int Calls;
        public string Version = "model-hash/persona-v1";
        public string? CacheVersion(string agent) => Version;
        public Task<string> GenerateAsync(string prompt, CancellationToken token)
        {
            Calls++;
            return Task.FromResult("Captured answer " + Calls);
        }
    }
    private static InferenceCacheScope Scope() => new(new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        Guid.NewGuid(), 0, Guid.NewGuid(), "project", "ai.analyze"), 0, 0, 0);

    [Fact]
    public async Task ExactHitsExpireAndRevalidateLiveScope()
    {
        var backend = new Backend();
        var clock = new Clock();
        await using var service = new SharedLocalInference(backend, clock: clock);
        var allowed = true;
        var client = service.WithCacheScope(Scope(), () => allowed);
        var first = await client.GenerateAsync("same source and question");
        Assert.Equal(first.Text, (await client.GenerateAsync("same source and question")).Text);
        Assert.Equal(1, backend.Calls);
        Assert.Contains(service.Diagnostics.Snapshot(), row => row.Outcome == InferenceOutcome.CacheHit);
        allowed = false;
        Assert.False((await client.GenerateAsync("same source and question")).Available);
        Assert.Equal(1, backend.Calls);
        allowed = true;
        clock.Now = clock.Now.AddSeconds(31);
        await client.GenerateAsync("same source and question");
        Assert.Equal(2, backend.Calls);
        backend.Version = "model-hash/persona-v2";
        await client.GenerateAsync("same source and question");
        Assert.Equal(3, backend.Calls);
    }

    [Theory]
    [InlineData("world")]
    [InlineData("session")]
    [InlineData("agent")]
    [InlineData("conversation")]
    [InlineData("project")]
    [InlineData("context")]
    [InlineData("memory")]
    [InlineData("activity")]
    public async Task ScopeAndSourceRevisionsNeverShareAnswers(string field)
    {
        var backend = new Backend();
        await using var service = new SharedLocalInference(backend);
        var original = Scope();
        var changed = field switch
        {
            "world" => original with { Identity = original.Identity with { WorldId = Guid.NewGuid() } },
            "session" => original with { Identity = original.Identity with { Generation = 1 } },
            "agent" => original with { Identity = original.Identity with { AgentId = Guid.NewGuid() } },
            "conversation" => original with { Identity = original.Identity with { ConversationId = Guid.NewGuid() } },
            "project" => original with { Identity = original.Identity with { Project = "different" } },
            "context" => original with { ContextRevision = 1 },
            "memory" => original with { MemoryRevision = 1 },
            _ => original with { Activity = 1 }
        };
        await service.WithCacheScope(original, () => true).GenerateAsync("question");
        await service.WithCacheScope(changed, () => true).GenerateAsync("question");
        Assert.Equal(2, backend.Calls);
        await service.WithCacheScope(changed, () => true).GenerateAsync("changed source text");
        Assert.Equal(3, backend.Calls);
    }

    [Fact]
    public async Task StructuredAndUnscopedRequestsBypassCache()
    {
        var backend = new Backend();
        await using var service = new SharedLocalInference(backend);
        await service.GenerateAsync("same");
        await service.GenerateAsync("same");
        var scoped = service.WithCacheScope(Scope(), () => true);
        await scoped.GenerateStructuredAsync("same");
        await scoped.GenerateStructuredAsync("same");
        Assert.Equal(4, backend.Calls);
    }

    [Fact]
    public void CacheEvictsWithinCapacityAndOnClockRollback()
    {
        var clock = new Clock();
        var cache = new ScopedInferenceCache(clock);
        for (var i = 0; i < 100; i++) cache.Store(i.ToString(), new string('x', 8192));
        Assert.Null(cache.Read("0"));
        Assert.NotNull(cache.Read("99"));
        clock.Now = clock.Now.AddMinutes(-1);
        Assert.Null(cache.Read("99"));
    }
}
