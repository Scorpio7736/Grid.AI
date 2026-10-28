using Xunit;
namespace Grid.Terminal.Tests;

public sealed class TypewriterTests
{
    [Fact]
    public async Task AnimationPreservesCompleteUnicodeTextAndOnlyDelaysResponse()
    {
        var output = new StringWriter(); var delays = 0; var checks = 0;
        var text = "User: hi\nMCP: A😀e\u0301";
        await TypewriterRenderer.WriteAsync(output, text, 9, true, () => false, () => { checks++; return Task.CompletedTask; }, default,
            _ => { delays++; return Task.CompletedTask; });
        Assert.Equal(text + Environment.NewLine, output.ToString());
        Assert.Equal(8, delays); Assert.True(checks >= 2);
    }
    [Fact]
    public async Task SkipFlushesRemainingResponseOnceWithoutDelays()
    {
        var output = new StringWriter();
        await TypewriterRenderer.WriteAsync(output, "User\nMCP: hello", 5, true, () => true, () => Task.CompletedTask, default,
            _ => throw new InvalidOperationException("No delay after skip"));
        Assert.Equal("User\nMCP: hello" + Environment.NewLine, output.ToString());
    }
    [Fact]
    public async Task InstantOutputDoesNotPollKeyboardOrDelay()
    {
        var output = new StringWriter();
        await TypewriterRenderer.WriteAsync(output, "MCP: reply", 0, false, () => throw new InvalidOperationException(), () => Task.CompletedTask, default,
            _ => throw new InvalidOperationException());
        Assert.Equal("MCP: reply" + Environment.NewLine, output.ToString());
        Assert.False(TypewriterRenderer.Enabled(true, false, null, null));
        Assert.False(TypewriterRenderer.Enabled(false, true, null, null));
        Assert.False(TypewriterRenderer.Enabled(false, false, "dumb", null));
        Assert.False(TypewriterRenderer.Enabled(false, false, null, "OFF"));
        Assert.True(TypewriterRenderer.Enabled(false, false, null, null));
    }
    [Fact]
    public async Task CancellationStopsAnimationWithoutWritingRemainingText()
    {
        var output = new StringWriter(); using var stop = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TypewriterRenderer.WriteAsync(output, "abcdef", 0, true,
            () => false, () => Task.CompletedTask, stop.Token, _ => { stop.Cancel(); return Task.CompletedTask; }));
        Assert.Equal("a", output.ToString());
    }
    [Fact]
    public async Task RevokedSessionStopsOutputBeforeFurtherCharacters()
    {
        var output = new StringWriter(); var checks = 0;
        await Assert.ThrowsAsync<IOException>(() => TypewriterRenderer.WriteAsync(output, "private", 0, true,
            () => false, () => ++checks == 2 ? Task.FromException(new IOException("revoked")) : Task.CompletedTask, default));
        Assert.Equal("", output.ToString());
    }
    [Fact]
    public void PrintableSkipKeyBecomesTypeaheadButControlKeysDoNot()
    {
        ConsoleInputBuffer.Clear();
        try
        {
            Assert.True(TypewriterRenderer.HandleSkipKey(new ConsoleKeyInfo('c', ConsoleKey.C, false, false, false)));
            Assert.True(ConsoleInputBuffer.TryRead(out var key)); Assert.Equal('c', key.KeyChar);
            Assert.False(TypewriterRenderer.HandleSkipKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false)));
            Assert.True(TypewriterRenderer.HandleSkipKey(new ConsoleKeyInfo('\u001b', ConsoleKey.Escape, false, false, false)));
            Assert.False(ConsoleInputBuffer.TryRead(out _));
            ConsoleInputBuffer.Preserve(new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false));
            ConsoleInputBuffer.Clear(); Assert.False(ConsoleInputBuffer.TryRead(out _));
        }
        finally { ConsoleInputBuffer.Clear(); }
    }

}
