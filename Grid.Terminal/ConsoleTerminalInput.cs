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
        var text = new StringBuilder(); var cursor = 0; string[]? choices = null; var choice = 0;
        var origin = Console.CursorLeft; var row = Console.CursorTop; var priorColor = Console.ForegroundColor;
        try
        {
            if (color) Console.ForegroundColor = ConsoleColor.White;
            while (true)
            {
                token.ThrowIfCancellationRequested();
                if (!Console.KeyAvailable) { await Task.Delay(20, token); continue; }
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return text.ToString(); }
                if (key.Key == ConsoleKey.D && key.Modifiers.HasFlag(ConsoleModifiers.Control)) { Console.WriteLine(); return null; }
                if (key.Key == ConsoleKey.Tab && !secret && cursor == text.Length)
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
                        case ConsoleKey.LeftArrow: cursor = Math.Max(0, cursor - 1); break;
                        case ConsoleKey.RightArrow: cursor = Math.Min(text.Length, cursor + 1); break;
                        case ConsoleKey.Home: cursor = 0; break;
                        case ConsoleKey.End: cursor = text.Length; break;
                        case ConsoleKey.Backspace: if (cursor > 0) text.Remove(--cursor, 1); break;
                        case ConsoleKey.Delete: if (cursor < text.Length) text.Remove(cursor, 1); break;
                        default:
                            if (!char.IsControl(key.KeyChar) && text.Length < (secret ? Grid.Communication.PasswordCredential.LegacyMaximumPasswordLength : 8192)) text.Insert(cursor++, key.KeyChar);
                            break;
                    }
                }
                origin = Math.Min(origin, Math.Max(0, Console.BufferWidth - 2));
                var width = Math.Max(1, Console.BufferWidth - origin - 1);
                var offset = Math.Max(0, cursor - width + 1);
                var shown = secret ? new string('*', text.Length) : text.ToString();
                shown = shown.Substring(offset, Math.Min(width, shown.Length - offset));
                row = Math.Min(row, Console.BufferHeight - 1);
                Console.SetCursorPosition(origin, row); Console.Write(shown.PadRight(width));
                Console.SetCursorPosition(origin + cursor - offset, row);
            }
        }
        finally { text.Clear(); if (color) Console.ForegroundColor = priorColor; }
    }
}
