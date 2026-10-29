using Xunit;
namespace Grid.Terminal.Tests;

public sealed class VisualThemeTests
{
    [Fact]
    public void ExactPaletteAndSemanticRoles()
    {
        Assert.Equal(new[] { "#101820", "#172C3C", "#234A5F", "#59D8F3", "#57D7F1", "#FFFFFF", "#C2CBD4", "#8C9FAE", "#34505F", "#F3C76A", "#63DB9C", "#F07878", "#C8AD72" }, Enum.GetValues<TerminalRole>().Select(r => GridTerminalTheme.Palette[r].Hex));
        Assert.Equal(TerminalRole.Error, GridTerminalTheme.Role("Transfer refused: unavailable"));
        Assert.Equal(TerminalRole.Success, GridTerminalTheme.Role("Applied file"));
        Assert.Equal(TerminalRole.Approval, GridTerminalTheme.Role("Risk: HIGH"));
        Assert.Equal(TerminalRole.Notice, GridTerminalTheme.Role("Nothing executed."));
    }
    [Fact]
    public async Task ColorSegmentsResetAndPlainModePreservesExactText()
    {
        using var buffer = new StringWriter();
        var output = new TerminalOutput(buffer, true, new(TerminalColorMode.TrueColor));
        const string text = "PROJECT GRID\nSIMULATION CONTROL\nCOMMAND | OPTIONS | DESCRIPTION\ngrid | -start | Resume simulation\nError: failure\nApplied file\nRisk: HIGH\nNotice\r\n";
        await output.WriteAsync(text);
        var rendered = buffer.ToString();
        Assert.Equal(text, TerminalLayout.Plain(rendered));
        foreach (var role in new[] { TerminalRole.Header, TerminalRole.Section }) Assert.Contains("\x1b[48;2;" + GridTerminalTheme.Palette[role].Rgb + "m", rendered);
        foreach (var role in new[] { TerminalRole.Title, TerminalRole.Command, TerminalRole.Description, TerminalRole.Border, TerminalRole.Input, TerminalRole.Error, TerminalRole.Success, TerminalRole.Approval, TerminalRole.Notice }) Assert.Contains("\x1b[38;2;" + GridTerminalTheme.Palette[role].Rgb + "m", rendered);
        Assert.DoesNotContain("\x1b[48;2;16;24;32m", rendered);
        Assert.Contains("\x1b[39;49m", rendered);
        buffer.GetStringBuilder().Clear();
        await new TerminalOutput(buffer, false, new(TerminalColorMode.TrueColor)).WriteAsync(text);
        Assert.Equal(text, buffer.ToString());
        buffer.GetStringBuilder().Clear();
        GridTerminalRenderer.Segment(buffer, "live", TerminalRole.Title, new(TerminalColorMode.TrueColor, true));
        Assert.Contains("\x1b[48;2;16;24;32m", buffer.ToString());
    }
    [Theory]
    [InlineData("e\u0301", 1)]
    [InlineData("界", 2)]
    [InlineData("👩‍💻", 2)]
    [InlineData("\u001b[31mabc\u001b[0m", 3)]
    public void WidthAndWrappingUseVisibleGraphemes(string text, int width)
    {
        Assert.Equal(width, TerminalLayout.Width(text));
        Assert.Equal(TerminalLayout.Plain(text), string.Concat(TerminalLayout.Wrap(text, 2)));
    }
    [Theory]
    [InlineData(20)] [InlineData(60)] [InlineData(120)]
    public void TablesFitAndPreserveUnicode(int width)
    {
        var text = TerminalTable.Render(["Name", "Detail"], [["界e\u0301", "👩‍💻 " + new string('x', 180)]], width);
        Assert.All(text.Split('\n'), line => Assert.True(TerminalLayout.Width(line) <= width));
        Assert.Contains("界e\u0301", text); Assert.Contains("👩‍💻", text); Assert.Equal(180, text.Count(c => c == 'x'));
    }
    [Fact]
    public void InputNavigationAndViewportRetainWholeGraphemes()
    {
        const string text = "A界e\u0301👩‍💻Z";
        var index = text.Length;
        while (index > 0) { var before = TerminalLayout.Previous(text, index); Assert.Equal(index, TerminalLayout.Next(text, before)); index = before; }
        var window = TerminalLayout.InputWindow(text, text.Length, 5, false);
        Assert.Equal(5, TerminalLayout.Width(window.Text)); Assert.InRange(window.Cursor, 0, 4);
        Assert.Equal("*****     ", TerminalLayout.InputWindow(text, text.Length, 10, true).Text);
    }
}
