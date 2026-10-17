using System.Text;

namespace Grid.Terminal;

public sealed class CommandDispatcher
{
    private readonly Dictionary<string, TerminalCommand> commands = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<TerminalCommand> Commands => commands.Values.OrderBy(c => c.Name).ToArray();

    public void Register(TerminalCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Any(char.IsWhiteSpace))
            throw new ArgumentException("Command names must be a single word.");
        ArgumentNullException.ThrowIfNull(command.Execute);
        if (!commands.TryAdd(command.Name, command))
            throw new ArgumentException($"Command already registered: {command.Name}.");
    }

    public CommandResult Execute(string line)
    {
        try
        {
            var tokens = Parse(line);
            if (tokens.Count == 0) return new("");
            if (!commands.TryGetValue(tokens[0], out var command))
                return new($"Unknown command '{tokens[0]}'. Type help for available commands.");
            return command.Execute(tokens.Skip(1).ToArray());
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or OverflowException)
        {
            return new($"Error: {exception.Message}");
        }
    }

    private static List<string> Parse(string line)
    {
        var result = new List<string>();
        var token = new StringBuilder();
        char? quote = null;
        var started = false;
        foreach (var character in line)
        {
            if (quote is not null)
            {
                if (character == quote) quote = null;
                else token.Append(character);
            }
            else if (character is '"' or '\'')
            {
                quote = character;
                started = true;
            }
            else if (char.IsWhiteSpace(character))
            {
                if (started) result.Add(token.ToString());
                token.Clear();
                started = false;
            }
            else
            {
                token.Append(character);
                started = true;
            }
        }
        if (quote is not null) throw new ArgumentException("Unclosed quote.");
        if (started) result.Add(token.ToString());
        return result;
    }
}
