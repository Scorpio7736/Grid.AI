using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
namespace Grid.Terminal;

public static class TerminalLayout
{
    public static int Previous(string text, int cursor) => StringInfo.ParseCombiningCharacters(text).LastOrDefault(i => i < cursor);
    public static int Next(string text, int cursor) => StringInfo.ParseCombiningCharacters(text).FirstOrDefault(i => i > cursor, text.Length);
    public static (string Text, int Cursor) InputWindow(string text, int cursor, int width, bool secret)
    {
        var start = 0;
        string Display(string value) => secret ? new string('*', Elements(value).Count()) : value;
        while (start < cursor && Width(Display(text[start..cursor])) >= width) start = Next(text, start);
        return (Pad(Fit(Display(text[start..]), width), width), Width(Display(text[start..cursor])));
    }
    public static string Plain(string text) => Regex.Replace(text, @"\x1b\[[0-?]*[ -/]*[@-~]|\x1b\][^\x07]*(?:\x07|\x1b\\)", "");
    public static IEnumerable<string> Elements(string text)
    {
        var iterator = StringInfo.GetTextElementEnumerator(Plain(text));
        while (iterator.MoveNext()) yield return iterator.GetTextElement();
    }
    private static int ElementWidth(string element)
    {
        var width = 0;
        foreach (var rune in element.EnumerateRunes())
        {
            var n = rune.Value;
            if (Rune.GetUnicodeCategory(rune) is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark or UnicodeCategory.Format || Rune.IsControl(rune)) continue;
            var wide = n is >= 0x1100 and <= 0x115f or >= 0x2329 and <= 0x232a or >= 0x2e80 and <= 0xa4cf or >= 0xac00 and <= 0xd7a3 or >= 0xf900 and <= 0xfaff or >= 0xfe10 and <= 0xfe6f or >= 0xff01 and <= 0xff60 or >= 0xffe0 and <= 0xffe6 or >= 0x1f000 and <= 0x1faff or >= 0x20000 and <= 0x3fffd;
            width = Math.Max(width, wide ? 2 : 1);
        }
        return width;
    }
    public static int Width(string text) => Elements(text).Sum(ElementWidth);
    public static string Pad(string text, int width) => text + new string(' ', Math.Max(0, width - Width(text)));
    public static string Fit(string text, int width)
    {
        var result = new StringBuilder(); var used = 0;
        foreach (var e in Elements(text)) { var n = ElementWidth(e); if (used + n > width) break; result.Append(e); used += n; }
        return result.ToString();
    }
    public static IEnumerable<string> Wrap(string text, int width)
    {
        width = Math.Max(2, width);
        if (text.Length == 0) { yield return ""; yield break; }
        var line = new StringBuilder(); var used = 0;
        foreach(var element in Elements(text))
        {
            var n = ElementWidth(element);
            if(used + n > width && line.Length > 0) { yield return line.ToString(); line.Clear(); used = 0; }
            line.Append(element); used += n;
        }
        if(line.Length > 0) yield return line.ToString();
    }
    public static IEnumerable<string> Paragraph(string text, int width)
    {
        var line = "";
        foreach (var word in Plain(text).Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if(line.Length > 0 && Width(line + " " + word) > width) { yield return line; line = ""; }
            if(Width(word) > width)
            {
                var parts = Wrap(word, width).ToArray();
                for(var i=0;i<parts.Length-1;i++) yield return parts[i];
                line = parts[^1];
            }
            else line += (line.Length == 0 ? "" : " ") + word;
        }
        if(line.Length > 0) yield return line;
    }
}
