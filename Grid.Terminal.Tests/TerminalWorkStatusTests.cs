using Xunit;

namespace Grid.Terminal.Tests;

public sealed class TerminalWorkStatusTests
{
    [Fact]
    public async Task FastOperationDoesNotProduceWaitingMessage()
    {
        var output = new StringWriter();
        var result = await TerminalWorkStatus.AwaitWithNoticeAsync(
            Task.FromResult("verified"), output, CancellationToken.None, TimeSpan.Zero);
        Assert.Equal("verified", result);
        Assert.Empty(output.ToString());
    }

    [Fact]
    public async Task SlowOperationGetsOneSafeNoticeAndReturnsOriginalResult()
    {
        var output = new StringWriter();
        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var awaiting = TerminalWorkStatus.AwaitWithNoticeAsync(
            completion.Task, output, CancellationToken.None, TimeSpan.Zero);
        Assert.Contains("Working...", output.ToString());
        Assert.DoesNotContain("model", output.ToString(), StringComparison.OrdinalIgnoreCase);
        completion.SetResult("PRIVATE COMPLETE RESULT");
        Assert.Equal("PRIVATE COMPLETE RESULT", await awaiting);
        Assert.Equal(1, output.ToString().Split("Working...", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("PRIVATE", output.ToString());
    }

    [Fact]
    public async Task CancellationNeverDisplaysIncompleteModelContent()
    {
        var output = new StringWriter();
        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var stop = new CancellationTokenSource();
        var awaiting = TerminalWorkStatus.AwaitWithNoticeAsync(
            completion.Task, output, stop.Token, TimeSpan.FromHours(1));
        stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => awaiting);
        Assert.Empty(output.ToString());
    }

    [Fact]
    public async Task FailedOperationRetainsFailureAndDoesNotClaimCompletion()
    {
        var output = new StringWriter();
        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var awaiting = TerminalWorkStatus.AwaitWithNoticeAsync(
            completion.Task, output, CancellationToken.None, TimeSpan.Zero);
        completion.SetException(new IOException("private source content"));
        await Assert.ThrowsAsync<IOException>(() => awaiting);
        Assert.DoesNotContain("private", output.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("complete", output.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
