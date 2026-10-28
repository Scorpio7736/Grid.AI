using System.Globalization;
namespace Grid.Terminal;

/// <summary>Presentation only: never generates, saves or dispatches messages.</summary>
public static class TypewriterRenderer
{
    public static bool Enabled(bool inputRedirected, bool outputRedirected, string? term, string? preference) =>
        !inputRedirected && !outputRedirected && !string.Equals(term, "dumb", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(preference, "off", StringComparison.OrdinalIgnoreCase);

    public static bool IsSkipKey(ConsoleKey key) => key != ConsoleKey.Enter;
    public static bool SkipRequested()
    {
        if (!Console.KeyAvailable) return false;
        return HandleSkipKey(Console.ReadKey(true));
    }
    internal static bool HandleSkipKey(ConsoleKeyInfo key)
    {
        // A printable key is the start of the operator's next input, not disposable animation control.
        if (!char.IsControl(key.KeyChar)) ConsoleInputBuffer.Preserve(key);
        return IsSkipKey(key.Key);
    }
    public static async Task WriteAsync(TextWriter output, string text, int? animateFrom, bool enabled,
        Func<bool> skip, Func<Task> validateSession, CancellationToken token,
        Func<CancellationToken, Task>? delay = null, IReadOnlyList<ConversationSpan>? presentation = null)
    {
        var spans = (presentation ?? []).OrderBy(s => s.Start).ToArray();
        var previousEnd = 0;
        foreach (var span in spans)
        {
            if (span.Start < previousEnd || span.Length < 0 || span.Start > text.Length - span.Length || !Enum.IsDefined(span.Tone)) throw new ArgumentException("Invalid conversation presentation.");
            previousEnd = span.Start + span.Length;
        }
        ConversationSpan? At(int index) => spans.FirstOrDefault(s => index >= s.Start && index < s.Start + s.Length);
        async Task WriteRange(int start, int length)
        {
            var end = start + length;
            for (var index = start; index < end;)
            {
                var span = At(index);
                var next = span is null ? Math.Min(end, spans.Where(s => s.Start > index).Select(s => s.Start).DefaultIfEmpty(end).Min()) : Math.Min(end, span.Start + span.Length);
                var value = text[index..next];
                if (spans.Length > 0 && output is IConversationOutput styled) await styled.WriteConversationAsync(value, ConversationPresentation.Color(span?.Tone ?? ConversationTone.Agent));
                else await output.WriteAsync(value);
                index = next;
            }
        }
        token.ThrowIfCancellationRequested();
        await validateSession();
        if (!enabled || animateFrom is not { } start || start < 0 || start >= text.Length)
        { await WriteRange(0, text.Length); await output.WriteLineAsync(""); await output.FlushAsync(); return; }
        await WriteRange(0, start);
        var elements = StringInfo.GetTextElementEnumerator(text[start..]);
        var count = 0;
        while (elements.MoveNext())
        {
            token.ThrowIfCancellationRequested();
            // Keep the attachment lease alive and stop revealing text after session revocation.
            if (count++ % 16 == 0) await validateSession();
            if (skip())
            {
                await validateSession();
                await WriteRange(start + elements.ElementIndex, text.Length - start - elements.ElementIndex);
                break;
            }
            await WriteRange(start + elements.ElementIndex, elements.GetTextElement().Length);
            await output.FlushAsync();
            if (spans.Length == 0 || At(start + elements.ElementIndex)?.Tone == ConversationTone.Agent)
                await (delay?.Invoke(token) ?? Task.Delay(20, token));
        }
        await output.WriteLineAsync("");
    }
}
