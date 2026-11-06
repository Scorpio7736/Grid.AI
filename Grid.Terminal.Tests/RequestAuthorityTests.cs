using Grid.Agent;
using Grid.AI;
using Xunit;
namespace Grid.Terminal.Tests;
public sealed class RequestAuthorityTests
{
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static TrustedRequestContext Scope() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0, Guid.NewGuid(), "general", "ai.chat");
    [Fact]
    public void ContextCannotAuthenticateItselfAndActionsAreNotGranted()
    {
        var gate = new RequestAuthority(); var scope = Scope();
        Assert.Throws<UnauthorizedAccessException>(() => gate.Acquire(scope, () => false));
        Assert.Throws<UnauthorizedAccessException>(() => gate.Acquire(scope with { Capability = "files.write SECRET" }, () => true));
        Assert.DoesNotContain("SECRET", System.Text.Json.JsonSerializer.Serialize(gate.Audit));
    }
    [Fact]
    public void RevocationAndDisposedLeaseCannotAuthorizeResults()
    {
        var gate = new RequestAuthority(); var valid = true;
        var lease = gate.Acquire(Scope(), () => valid); Assert.True(lease.IsCurrent);
        valid = false; Assert.False(lease.IsCurrent); lease.Dispose(); valid = true; Assert.False(lease.IsCurrent);
        Assert.Contains(gate.Audit, e => e.Outcome == "revoked-or-expired");
    }
    [Fact]
    public void ConcurrentQuotaIsSharedAcrossSessionsForTheSameWorldOwner()
    {
        var gate = new RequestAuthority(); var scope = Scope();
        using var first = gate.Acquire(scope, () => true);
        using var second = gate.Acquire(scope with { SessionId = Guid.NewGuid() }, () => true);
        Assert.Throws<InvalidOperationException>(() => gate.Acquire(scope with { SessionId = Guid.NewGuid() }, () => true));
        first.Dispose(); first.Dispose(); using var next = gate.Acquire(scope, () => true);
        Assert.True(next.IsCurrent);
    }
    [Fact]
    public void RateQuotaExpiresAndDoesNotLeakAcrossWorlds()
    {
        var clock = new Clock(); var gate = new RequestAuthority(clock); var scope = Scope();
        for(var i=0;i<60;i++) gate.Acquire(scope, () => true).Dispose();
        Assert.Throws<InvalidOperationException>(() => gate.Acquire(scope, () => true));
        gate.Acquire(scope with { WorldId = Guid.NewGuid() }, () => true).Dispose();
        clock.Now += TimeSpan.FromMinutes(1); gate.Acquire(scope, () => true).Dispose();
        Assert.True(gate.Audit.Count <= 128);
    }
    [Fact]
    public void MissingAndForgedScopeIsRejected()
    {
        var gate = new RequestAuthority(); var scope = Scope();
        Assert.Throws<UnauthorizedAccessException>(() => gate.Acquire(scope with { UserId = Guid.Empty }, () => true));
        Assert.Throws<UnauthorizedAccessException>(() => gate.Acquire(scope with { Project = "../private" }, () => true));
        Assert.Throws<UnauthorizedAccessException>(() => gate.Acquire(scope with { Generation = 1 }, () => scope.Generation == 1));
        Assert.DoesNotContain(gate.Audit, e => e.Outcome == "allowed");
    }
}
