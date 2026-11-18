using Grid.Communication;
using Grid.Core;
using Grid.TestSupport;
using Xunit;

namespace Grid.Simulation.Tests;

public sealed class ChallengeSessionTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static (GridSimulationEngine Engine, Clock Clock, Dictionary<Guid, string> Answers,
        PairedChallengeEndpoint Endpoint, Grid.Simulation.Editing.EditSessionReference Master) Fixture()
    {
        var clock = new Clock();
        var world = GridWorldFactory.CreateGenesis();
        var authority = new TestEditAuthority(world.InstanceId);
        var engine = new GridSimulationEngine(world, timeProvider: clock, editAuthorizer: authority);
        var entries = new[] { ("First", "alpha"), ("Second", "beta"), ("Third", "gamma") };
        var set = ChallengeCredentialSet.Create(entries, "Owner", "Ab1!xy");
        engine.SetLocalCredentials(PasswordCredential.Create("Owner", "Ab1!xy"), set);
        var endpoint = engine.PairChallengeEndpoint(authority.Session, "phone-1");
        return (engine, clock, set.Challenges.Zip(entries).ToDictionary(x => x.First.Id, x => x.Second.Item2),
            endpoint, authority.Session);
    }

    [Fact]
    public void TwoDistinctResponsesCreateOnlyAnExactShortLivedClaim()
    {
        var (engine, clock, answers, endpoint, _) = Fixture();
        var first = engine.BeginRemoteChallenge(endpoint, "call-1", "remote-call-identity");
        var binding = first.Binding!;
        Assert.Equal(ChallengeSessionStatus.Pending, first.Status);
        var second = engine.SubmitRemoteChallenge(first.NextPrompt!.SessionId, binding,
            first.NextPrompt.ChallengeId, answers[first.NextPrompt.ChallengeId]);
        Assert.Equal(ChallengeSessionStatus.Pending, second.Status);
        Assert.NotEqual(first.NextPrompt.ChallengeId, second.NextPrompt!.ChallengeId);
        var passed = engine.SubmitRemoteChallenge(second.NextPrompt.SessionId, binding,
            second.NextPrompt.ChallengeId, answers[second.NextPrompt.ChallengeId]);
        Assert.Equal(ChallengeSessionStatus.Passed, passed.Status);
        Assert.True(engine.ValidateRemoteChallengeClaim(passed.ClaimId!.Value, binding));
        Assert.False(engine.ValidateRemoteChallengeClaim(passed.ClaimId.Value, binding with { Purpose = "task-approval" }));
        Assert.DoesNotContain(engine.CaptureHistory().Events, e =>
            e.Detail.Contains("alpha", StringComparison.OrdinalIgnoreCase) ||
            e.ContextJson.Contains("alpha", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(ChallengeSessionStatus.Expired, engine.SubmitRemoteChallenge(second.NextPrompt.SessionId, binding,
            second.NextPrompt.ChallengeId, answers[second.NextPrompt.ChallengeId]).Status);
        clock.Now = clock.Now.AddMinutes(6);
        Assert.False(engine.ValidateRemoteChallengeClaim(passed.ClaimId.Value, binding));
    }

    [Fact]
    public void ExpiredCrossCallAndRestartedSessionsFailClosed()
    {
        var (engine, clock, answers, endpoint, _) = Fixture();
        var issued = engine.BeginRemoteChallenge(endpoint, "call-1", "remote-call-identity");
        var binding = issued.Binding!;
        Assert.Equal(ChallengeSessionStatus.Cancelled, engine.SubmitRemoteChallenge(issued.NextPrompt!.SessionId,
            binding with { ContactId = "other-call" }, issued.NextPrompt.ChallengeId, answers[issued.NextPrompt.ChallengeId]).Status);

        issued = engine.BeginRemoteChallenge(endpoint, binding.ContactId, binding.Purpose);
        clock.Now = clock.Now.AddMinutes(3);
        Assert.Equal(ChallengeSessionStatus.Expired, engine.SubmitRemoteChallenge(issued.NextPrompt!.SessionId,
            binding, issued.NextPrompt.ChallengeId, answers[issued.NextPrompt.ChallengeId]).Status);

        var snapshot = engine.GetSnapshot();
        var restored = GridSimulationEngine.Restore(snapshot, clock);
        Assert.Equal(ChallengeSessionStatus.Expired, restored.SubmitRemoteChallenge(Guid.NewGuid(),
            binding, Guid.NewGuid(), "unused").Status);
    }

    [Fact]
    public void ThreeFailuresLockAccountAndEndpointAcrossRestore()
    {
        var (engine, clock, _, endpoint, _) = Fixture();
        ChallengeSessionBinding? binding = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var issued = engine.BeginRemoteChallenge(endpoint, "call-1", "remote-call-identity");
            binding = issued.Binding!;
            Assert.Equal(ChallengeSessionStatus.Failed, engine.SubmitRemoteChallenge(issued.NextPrompt!.SessionId,
                binding, issued.NextPrompt.ChallengeId, "wrong").Status);
        }
        Assert.Equal(ChallengeSessionStatus.Locked,
            engine.BeginRemoteChallenge(endpoint, "new-call", binding!.Purpose).Status);
        Assert.Equal(2, engine.GetSnapshot().Identities.ChallengeRateLimits.Buckets.Count);

        clock.Now = clock.Now.AddMinutes(16);
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var issued = engine.BeginRemoteChallenge(endpoint, "later-call", binding.Purpose);
            Assert.Equal(ChallengeSessionStatus.Failed, engine.SubmitRemoteChallenge(issued.NextPrompt!.SessionId,
                issued.Binding!, issued.NextPrompt.ChallengeId, "wrong").Status);
        }
        clock.Now = clock.Now.AddMinutes(16);
        Assert.Equal(ChallengeSessionStatus.Locked,
            engine.BeginRemoteChallenge(endpoint, "still-cooling-down", binding.Purpose).Status);

        var restored = GridSimulationEngine.Restore(engine.GetSnapshot(), clock);
        Assert.Equal(2, restored.GetSnapshot().Identities.ChallengeRateLimits.Buckets.Count);
        Assert.Equal(ChallengeSessionStatus.Cancelled,
            restored.BeginRemoteChallenge(endpoint, "after-restart", binding!.Purpose).Status);
        Assert.Throws<UnauthorizedAccessException>(() => restored.PairChallengeEndpoint(
            new(Guid.NewGuid(), 0), "another-endpoint"));
    }

    [Fact]
    public void ForgedAndRevokedEndpointGrantsFailClosed()
    {
        var (engine, _, _, endpoint, master) = Fixture();
        var forged = endpoint with { GrantId = Guid.NewGuid() };
        Assert.Equal(ChallengeSessionStatus.Cancelled,
            engine.BeginRemoteChallenge(forged, "call", "remote-call-identity").Status);
        engine.RevokeChallengeEndpoint(master, endpoint.GrantId);
        Assert.Equal(ChallengeSessionStatus.Cancelled,
            engine.BeginRemoteChallenge(endpoint, "call", "remote-call-identity").Status);
    }

    [Fact]
    public void CredentialRotationInvalidatesAttemptsAndClearsOldFailureLimits()
    {
        var (engine, _, _, endpoint, _) = Fixture();
        var issued = engine.BeginRemoteChallenge(endpoint, "call-1", "remote-call-identity");
        var binding = issued.Binding!;
        var nextSet = ChallengeCredentialSet.Create(
            [("New one", "one"), ("New two", "two"), ("New three", "three")], "Owner", "Cd2@yz");
        engine.SetLocalCredentials(PasswordCredential.Create("Owner", "Cd2@yz"), nextSet);
        Assert.Equal(ChallengeSessionStatus.Expired, engine.SubmitRemoteChallenge(issued.NextPrompt!.SessionId,
            binding, issued.NextPrompt.ChallengeId, "alpha").Status);
        Assert.Empty(engine.GetSnapshot().Identities.ChallengeRateLimits.Buckets);
    }
}
