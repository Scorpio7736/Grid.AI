namespace Grid.Terminal;

/// <summary>
/// Presentation-only notice for requests that take longer than an ordinary interactive response.
/// No model text, task contents or private arguments are exposed while work is in progress.
/// </summary>
public static class TerminalWorkStatus
{
    public static async Task<T> AwaitWithNoticeAsync<T>(
        Task<T> work,
        TextWriter output,
        CancellationToken token,
        TimeSpan? noticeAfter = null)
    {
        ArgumentNullException.ThrowIfNull(work);
        ArgumentNullException.ThrowIfNull(output);
        var delay = noticeAfter ?? TimeSpan.FromMilliseconds(700);
        if (delay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(noticeAfter));
        token.ThrowIfCancellationRequested();

        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
        try
        {
            var notice = Task.Delay(delay, stop.Token);
            if (await Task.WhenAny(work, notice) == notice && !work.IsCompleted && !token.IsCancellationRequested)
            {
                await output.WriteLineAsync("GRID | Working... the result will appear when ready.");
                await output.FlushAsync();
            }
            return await work.WaitAsync(token);
        }
        finally { stop.Cancel(); }
    }
}
