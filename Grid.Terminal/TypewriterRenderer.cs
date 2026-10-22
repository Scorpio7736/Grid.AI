using System.Globalization;
namespace Grid.Terminal;

/// <summary>Presentation only: never generates, saves or dispatches messages.</summary>
public static class TypewriterRenderer
{
    public static bool Enabled(bool inputRedirected, bool outputRedirected, string? term, string? preference) =>
        !inputRedirected && !outputRedirected && !string.Equals(term, "dumb", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(preference, "off", StringComparison.OrdinalIgnoreCase);

    public static async Task WriteAsync(TextWriter output, string text, int? animateFrom, bool enabled,
        Func<bool> skip, Func<Task> validateSession, CancellationToken token,
        Func<CancellationToken, Task>? delay = null)
    {
        token.ThrowIfCancellationRequested();
        await validateSession();
        if (!enabled || animateFrom is not { } start || start < 0 || start >= text.Length)
        { await output.WriteLineAsync(text); return; }
        await output.WriteAsync(text[..start]);
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
                await output.WriteAsync(text[(start + elements.ElementIndex)..]);
                break;
            }
            await output.WriteAsync(elements.GetTextElement());
            await output.FlushAsync();
            await (delay?.Invoke(token) ?? Task.Delay(12, token));
        }
        await output.WriteLineAsync("");
    }
}
