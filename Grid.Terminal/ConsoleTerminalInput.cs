using System.Text;
namespace Grid.Terminal;

public interface ITerminalInput
{
    bool Interactive { get; }
    Task<string?> ReadAsync(bool secret, CancellationToken token);
}

/// <summary>One cancellable keyboard owner, no input history, masked secret mode with completion disabled.</summary>
public sealed class ConsoleTerminalInput(CommandDispatcher commands, bool color) : ITerminalInput
{
    public bool Interactive => !Console.IsInputRedirected && !Console.IsOutputRedirected;
    public async Task<string?> ReadAsync(bool secret, CancellationToken token)
    {
        if (!Interactive)
        {
            if (secret) throw new InvalidOperationException("Secret entry requires an interactive terminal.");
            return await Task.Run(Console.ReadLine, token).WaitAsync(token);
        }
        if (secret) ConsoleInputBuffer.Clear();
        var text = new StringBuilder(); var cursor = 0; string[]? choices = null; var choice = 0;
        var origin = Console.CursorLeft; var row = Console.CursorTop; var priorColor = Console.ForegroundColor;
        try
        {
            if (color) Console.ForegroundColor = GridTerminalTheme.Palette[TerminalRole.Input].Fallback;
            while (true)
            {
                token.ThrowIfCancellationRequested();
                ConsoleKeyInfo key;
                if (!ConsoleInputBuffer.TryRead(out key))
                {
                    if (!Console.KeyAvailable) { await Task.Delay(20, token); continue; }
                    key = Console.ReadKey(true);
                }
                if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return text.ToString(); }
                if (key.Key == ConsoleKey.D && key.Modifiers.HasFlag(ConsoleModifiers.Control)) { Console.WriteLine(); return null; }
                if (key.Key == ConsoleKey.Tab && !secret && cursor == text.Length && commands.ConversationConnected?.Invoke() != true)
                {
                    choices ??= CommandHelp.Complete(text.ToString(), commands.Commands).ToArray();
                    choices = choices.Where(candidate => commands.Commands.Any(c => c.Name.Equals(candidate.Split(' ')[0], StringComparison.OrdinalIgnoreCase))).ToArray();
                    if (choices.Length > 0) { text.Clear().Append(choices[choice++ % choices.Length]); cursor = text.Length; }
                }
                else
                {
                    choices = null; choice = 0;
                    switch (key.Key)
                    {
                        case ConsoleKey.LeftArrow: cursor = TerminalLayout.Previous(text.ToString(), cursor); break;
                        case ConsoleKey.RightArrow: cursor = TerminalLayout.Next(text.ToString(), cursor); break;
                        case ConsoleKey.Home: cursor = 0; break;
                        case ConsoleKey.End: cursor = text.Length; break;
                        case ConsoleKey.Backspace:
                            if (cursor > 0) { var previous = TerminalLayout.Previous(text.ToString(), cursor); text.Remove(previous, cursor - previous); cursor = previous; } break;
                        case ConsoleKey.Delete:
                            if (cursor < text.Length) text.Remove(cursor, TerminalLayout.Next(text.ToString(), cursor) - cursor); break;
                        default:
                            if (!char.IsControl(key.KeyChar) && text.Length < (secret ? Grid.Communication.PasswordCredential.LegacyMaximumPasswordLength : 8192)) text.Insert(cursor++, key.KeyChar);
                            break;
                    }
                }
                origin = Math.Min(origin, Math.Max(0, Console.WindowWidth - 2));
                var width = Math.Max(1, Console.WindowWidth - origin - 1);
                var shown = TerminalLayout.InputWindow(text.ToString(), cursor, width, secret);
                row = Math.Min(row, Console.BufferHeight - 1);
                Console.SetCursorPosition(origin, row);
                GridTerminalRenderer.Segment(Console.Out, shown.Text, TerminalRole.Input, TerminalCapabilities.Detect(color));
                Console.SetCursorPosition(origin + shown.Cursor, row);
            }
        }
        finally { text.Clear(); if (color) Console.ForegroundColor = priorColor; }
    }
}
