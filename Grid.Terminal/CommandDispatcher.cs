using System.Text;

namespace Grid.Terminal;

public sealed class CommandDispatcher
{
    private readonly Dictionary<string, TerminalCommand> commands = new(StringComparer.OrdinalIgnoreCase);
    private TerminalIdentitySession? identity;
    public Func<IReadOnlyList<string>, CommandResult>? OpenWindow { get; set; }
    public Func<IReadOnlyList<string>, CommandResult>? OpenConversation { get; set; }
    public Func<bool>? ConversationConnected { get; set; }
    public string Prompt => identity?.Prompt ?? "grid> ";
    public bool Typewriter { get; set; } = !string.Equals(Environment.GetEnvironmentVariable("GRID_TYPEWRITER"), "off", StringComparison.OrdinalIgnoreCase);
    public long? IdentityGeneration => identity?.Generation;
    public bool LoginRequired => identity is not null;
    public bool IsAllowed(string name) => commands.TryGetValue(name, out var command) && (identity?.Allows(command.Capability) ?? true);
    public IReadOnlyList<TerminalCommand> Commands => commands.Values.Where(c => identity?.Allows(c.Capability) ?? true).OrderBy(c => c.Name).ToArray();
    public void UseIdentity(TerminalIdentitySession session)
    {
        identity = session;
        foreach (var key in commands.Keys.ToArray())
        {
            var capability = key switch {
                "help" or "exit" or "clear" => "public",
                "connect" or "talk" or "conversations" or "transcript" or "resume" or "disconnect" => "chat",
                _ => commands[key].Capability
            };
            commands[key] = commands[key] with { Capability = capability };
        }
    }

    public void Remove(string name) => commands.Remove(name);

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
            return ExecuteTokens(Parse(line));
        }
        catch (ArgumentException e) { return new("Error: " + e.Message); }
    }
    public CommandResult ExecuteTokens(IReadOnlyList<string> tokens)
    {
        try
        {
            if (tokens.Count == 0) return new("");
            if (!commands.TryGetValue(tokens[0], out var command))
                return new($"Unknown command '{tokens[0]}'. Type help for available commands.");
            CommandResult Run()
            {
                if (tokens.Skip(1).Contains("-opninwin", StringComparer.OrdinalIgnoreCase))
                {
                    if (!WindowCommands.Supports(command.Name) || tokens.Count(t => t.Equals("-opninwin", StringComparison.OrdinalIgnoreCase)) != 1)
                        return new("Error: -opninwin is supported only once on read-only data commands or live.");
                    return OpenWindow?.Invoke(tokens.Where(t => !t.Equals("-opninwin", StringComparison.OrdinalIgnoreCase)).ToArray()) ?? new("Window opening is unavailable in this host.");
                }
                if (command.Name is "connect" or "resume")
                {
                    var inline = tokens.Skip(1).Count(t => t.Equals("-inline", StringComparison.OrdinalIgnoreCase));
                    if (inline > 1) return new("Error: use -inline only once.");
                    if (inline == 1) return command.Execute(tokens.Skip(1).Where(t => !t.Equals("-inline", StringComparison.OrdinalIgnoreCase)).ToArray());
                    if (OpenConversation is not null) return OpenConversation(tokens);
                }
                return command.Execute(tokens.Skip(1).ToArray());
            }
            return identity is null ? Run() : identity.Execute(command.Capability, Run, command.Name);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or OverflowException)
        {
            return new($"Error: {exception.Message}");
        }
    }

    public static List<string> Parse(string line)
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
