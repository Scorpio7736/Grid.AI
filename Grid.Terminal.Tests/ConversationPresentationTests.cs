using Xunit;
namespace Grid.Terminal.Tests;

public sealed class ConversationPresentationTests
{
    private sealed class RecordingOutput : StringWriter, IConversationOutput
    {
        public readonly List<(string Text, ConsoleColor Color)> Styled = [];
        public Task WriteConversationAsync(string text, ConsoleColor color)
        {
            Styled.Add((text, color)); Write(text); return Task.CompletedTask;
        }
        public string OfColor(ConsoleColor color) => string.Concat(Styled.Where(s => s.Color == color).Select(s => s.Text));
    }
    [Fact]
    public async Task AnimationColorsDoNotDependOnMessageWordsAndPreserveCodeAndUnicode()
    {
        const string user = "You: MCP: fake error";
        const string body = "Error means failure.\n```css\n  div { color: red; }\n```\n😀e\u0301";
        var text = user + "\n\nMCP: " + body;
        ConversationSpan[] spans = [new(0, 5, ConversationTone.Heading), new(5, user.Length - 5, ConversationTone.User), new(user.Length + 2, 5, ConversationTone.Heading), new(user.Length + 7, body.Length, ConversationTone.Agent)];
        var output = new RecordingOutput(); var delays = 0;
        await TypewriterRenderer.WriteAsync(output, text, user.Length + 2, true, () => false, () => Task.CompletedTask, default, _ => { delays++; return Task.CompletedTask; }, spans);
        Assert.Equal(text + Environment.NewLine, output.ToString());
        Assert.Equal("MCP: fake error", output.OfColor(ConsoleColor.White));
        Assert.Contains(body, output.OfColor(ConsoleColor.Yellow));
        Assert.Empty(output.OfColor(ConsoleColor.Red));
        Assert.Equal(System.Globalization.StringInfo.ParseCombiningCharacters(body).Length, delays);
    }
    [Fact]
    public async Task DisabledAnimationKeepsStylesAndDoesNotPollOrDelay()
    {
        var result = ConversationPresentation.AgentReply("MCP", "Ready", "Sources"); var output = new RecordingOutput();
        await TypewriterRenderer.WriteAsync(output, result.Text, result.AnimateFrom, false, () => throw new Exception("keyboard"), () => Task.CompletedTask, default, _ => throw new Exception("delay"), result.Presentation);
        Assert.Contains("Ready", output.OfColor(ConsoleColor.Yellow));
        Assert.Contains("Sources", output.OfColor(ConsoleColor.Yellow));
        Assert.Equal(result.Text + Environment.NewLine, output.ToString());
    }
    [Fact]
    public async Task SkippedReplyKeepsRemainingColorsAndWritesExactlyOnce()
    {
        var result = ConversationPresentation.AgentReply("MCP", "A response", "Footer"); var output = new RecordingOutput();
        await TypewriterRenderer.WriteAsync(output, result.Text, result.AnimateFrom, true, () => true, () => Task.CompletedTask, default, _ => throw new Exception("delay"), result.Presentation);
        Assert.Contains("A response", output.OfColor(ConsoleColor.Yellow));
        Assert.Contains("Footer", output.OfColor(ConsoleColor.Yellow));
        Assert.Equal(result.Text + Environment.NewLine, output.ToString());
    }
    [Fact]
    public async Task PlainAndNoColorWritersPreserveTextWithoutEscapeCodes()
    {
        var result = ConversationPresentation.AgentReply("MCP", "```\n  code\n```", "Footer");
        foreach (var themed in new[] { false, true })
        {
            var sink = new StringWriter(); TextWriter output = themed ? new TerminalOutput(sink, false) : sink;
            await TypewriterRenderer.WriteAsync(output, result.Text, null, false, () => throw new Exception(), () => Task.CompletedTask, default, presentation: result.Presentation);
            Assert.Equal(result.Text + Environment.NewLine, sink.ToString()); Assert.DoesNotContain('\x1b', sink.ToString());
        }
    }
    [Fact]
    public async Task InvalidSpansNeverWriteAnyText()
    {
        var output = new RecordingOutput();
        await Assert.ThrowsAsync<ArgumentException>(() => TypewriterRenderer.WriteAsync(output, "secret", 0, true, () => false, () => Task.CompletedTask, default, presentation: [new(0, 100, ConversationTone.Agent)]));
        Assert.Equal("", output.ToString());
    }
    [Fact]
    public async Task TranscriptUsesColorsWithoutAnimation()
    {
        var output = new RecordingOutput();
        await TypewriterRenderer.WriteAsync(output, "You: Hi\nMCP: Ready", null, true, () => throw new Exception(), () => Task.CompletedTask, default, presentation: [new(0, 5, ConversationTone.Heading), new(5, 2, ConversationTone.User), new(8, 5, ConversationTone.Heading), new(13, 5, ConversationTone.Agent)]);
        Assert.Equal("Hi", output.OfColor(ConsoleColor.White)); Assert.Contains("Ready", output.OfColor(ConsoleColor.Yellow));
    }
    [Theory]
    [InlineData(ConsoleKey.Enter, false)]
    [InlineData(ConsoleKey.Escape, true)]
    [InlineData(ConsoleKey.Spacebar, true)]
    [InlineData(ConsoleKey.A, true)]
    public void EnterCannotAccidentallySkipResponse(ConsoleKey key, bool skip) => Assert.Equal(skip, TypewriterRenderer.IsSkipKey(key));
    [Theory]
    [InlineData("AI response rejected; no proposal created.")]
    [InlineData("Local AI unavailable.")]
    public void CommandFailuresUseErrorColor(string text) => Assert.Equal(ConsoleColor.Red,TerminalTheme.Color(text));

    [Fact]
    public void ConversationRolesUseRequestedPalette()
    {
        Assert.Equal(ConsoleColor.Cyan, ConversationPresentation.Color(ConversationTone.Heading));
        Assert.Equal(ConsoleColor.White, ConversationPresentation.Color(ConversationTone.User));
        Assert.Equal(ConsoleColor.Yellow, ConversationPresentation.Color(ConversationTone.Agent));
        Assert.Equal(ConsoleColor.Red, ConversationPresentation.Color(ConversationTone.Error));
    }
}
