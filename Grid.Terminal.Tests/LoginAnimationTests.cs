using System.Diagnostics;
using Xunit;
namespace Grid.Terminal.Tests;

[Trait("Phase", "6.7")]
public sealed class LoginAnimationTests
{
    [Fact]
    public async Task TransitionUsesElapsedDeadlineAndNeverWaitsForInput()
    {
        var elapsed = TimeSpan.Zero; var waits = new List<TimeSpan>(); var frames = 0;
        var output = new StringWriter();
        await GridLoginAnimation.RunAsync(output, "Welcome, Owner\nAuthority level: Master Control", true,
            () => { frames++; return true; }, default, () => false, () => elapsed,
            (duration, _) => { waits.Add(duration); elapsed += duration + TimeSpan.FromMilliseconds(75); return Task.CompletedTask; }, 100);
        Assert.InRange(elapsed.TotalSeconds, 5, 5.075);
        Assert.True(waits.Sum(t => t.TotalSeconds) < 5); // Slow frames do not add five more seconds of waiting.
        Assert.True(frames > 2); Assert.Contains(GridBanner.Render(100), output.ToString());
        Assert.Contains("Welcome, Owner", output.ToString()); Assert.DoesNotContain("Press Enter", output.ToString());
    }
    [Fact]
    public async Task RealClockTransitionCompletesAutomaticallyAfterFiveSeconds()
    {
        var output = new StringWriter(); var clock = Stopwatch.StartNew();
        await GridLoginAnimation.RunAsync(output, "Welcome, Owner", true, () => true, default, () => false, width: 100);
        Assert.InRange(clock.Elapsed.TotalSeconds, 4.9, 8);
        Assert.EndsWith("Welcome, Owner" + Environment.NewLine, output.ToString());
    }
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task StaticOrUnsupportedConsoleDoesNotPollOrDelay(bool animate, bool clearWorks)
    {
        var output = new StringWriter();
        await GridLoginAnimation.RunAsync(output, "Welcome", animate, () => clearWorks, default,
            () => throw new Exception("Unexpected key read"), delay: (_, _) => throw new Exception("Unexpected delay"), width: 100);
        Assert.Equal(GridBanner.Render(100) + Environment.NewLine + "Welcome" + Environment.NewLine, output.ToString());
    }
    [Fact]
    public async Task SkipImmediatelyShowsCompleteLogoAndIdentity()
    {
        var output = new StringWriter();
        await GridLoginAnimation.RunAsync(output, "Authority level: Master Control", true, () => true, default, () => true,
            delay: (_, _) => throw new Exception("Unexpected delay"), width: 100);
        Assert.Contains(GridBanner.Render(100), output.ToString()); Assert.Contains("Authority level", output.ToString());
        Assert.DoesNotContain("ENTERING GRID", output.ToString());
    }
    [Fact]
    public async Task CancellationStopsTransition()
    {
        using var stop = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => GridLoginAnimation.RunAsync(new StringWriter(), "Welcome", true,
            () => true, stop.Token, () => false, delay: (_, token) => { stop.Cancel(); token.ThrowIfCancellationRequested(); return Task.CompletedTask; }));
    }
    [Theory]
    [InlineData(false, "xterm", null, false)]
    [InlineData(true, "dumb", null, false)]
    [InlineData(true, "xterm", "off", false)]
    [InlineData(true, "xterm", "OFF", false)]
    [InlineData(true, "xterm", null, true)]
    public void MotionPreferenceAndConsoleSupport(bool interactive, string term, string? preference, bool enabled)
        => Assert.Equal(enabled, GridLoginAnimation.Enabled(interactive, term, preference));
    [Fact]
    public void AnimatedArtworkHasReadableNarrowFallbackAndHonestShutdownStatus()
    {
        Assert.Contains("██████╗ ██████╗ ██╗██████╗", TerminalSessionUi.Opening(100));
        Assert.NotEqual(GridBanner.Render(100, 0), GridBanner.Render(100, 1));
        Assert.NotEqual(GridBanner.Render(100, .25), GridBanner.Render(100, .75));
        Assert.All(GridBanner.Render(100, .5).Split('\n'), line => Assert.True(TerminalLayout.Width(line) <= 100));
        Assert.Contains("GRID7736", GridBanner.Render(30));
        Assert.Equal("GRID7736", GridBanner.Render(20));
        Assert.Contains(GridBanner.Render(100), TerminalPresentation.Farewell(true, true, 100));
        Assert.Contains("SHUTDOWN ERROR", TerminalPresentation.Farewell(false, true, 100));
        Assert.DoesNotContain("WORLD SAVED", TerminalPresentation.Farewell(false, true, 100));
    }
    private sealed class Output : StringWriter, IConversationOutput
    {
        public List<(string Text, ConsoleColor Color)> Spans = [];
        public Task WriteConversationAsync(string text, ConsoleColor color) { Spans.Add((text, color)); return WriteAsync(text); }
    }
    [Fact]
    public async Task PromptsAreExplicitlyBlueAndUnstyledConversationMessagesAreYellow()
    {
        var output = new Output();
        await TerminalTheme.PromptAsync(output, "You> ");
        Assert.Equal(ConsoleColor.Cyan, Assert.Single(output.Spans).Color);
        output.Spans.Clear();
        var reply = ConversationPresentation.AgentReply("MCP", "Ready", "Sources and diagnostics");
        await TypewriterRenderer.WriteAsync(output, reply.Text, null, false, () => false, () => Task.CompletedTask, default, presentation: reply.Presentation);
        Assert.Equal("MCP", string.Concat(output.Spans.Where(s => s.Color == ConsoleColor.Cyan).Select(s => s.Text)));
        Assert.Contains("Sources and diagnostics", string.Concat(output.Spans.Where(s => s.Color == ConsoleColor.Yellow).Select(s => s.Text)));
    }
}
