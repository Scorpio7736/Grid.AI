using System.Text;

namespace Grid.Terminal;

public sealed class CommandDispatcher
{
    private readonly Dictionary<string, TerminalCommand> commands = new(StringComparer.OrdinalIgnoreCase);
    private TerminalIdentitySession? identity;
    public Func<IReadOnlyList<string>, CommandResult>? OpenWindow { get; set; }
    public Func<IReadOnlyList<string>, CommandResult>? OpenConversation { get; set; }
    public Func<bool>? ConversationConnected { get; set; }
    public Func<string, bool>? ConversationParticipant { get; set; }
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
            if (IsFence(line)) return new("Code-block marker ignored. Paste the command lines inside it.");
            return ExecuteTokens(ParseInput(line));
        }
        catch (ArgumentException e) { return new("Error: " + e.Message); }
    }
    private static bool IsFence(string line) => System.Text.RegularExpressions.Regex.IsMatch(line.Trim(), @"^```(?:text|plaintext|powershell|pwsh|sh|bash)?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    // Chat is raw text, not shell-like command tokens. Explicit command names remain reserved.
    public IReadOnlyList<string> ParseInput(string line)
    {
        var text = line.Trim();
        if (IsFence(text)) return [];
        var boundary = text.IndexOfAny([' ', '\t', '\r', '\n']);
        var name = boundary < 0 ? text : text[..boundary];
        if (name.Equals("talk", StringComparison.OrdinalIgnoreCase))
            return boundary < 0 ? ["talk"] : ["talk", text[(boundary + 1)..].Trim()];
        if (ConversationConnected?.Invoke() == true && ConversationParticipant?.Invoke(name) == true)
        {
            var second = boundary < 0 ? "" : text[(boundary + 1)..].Trim().Split(' ', '\t')[0];
            var commandForm = commands.TryGetValue(name, out var registered) && CommandHelp.Options(registered).Any(option =>
                option.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault()?.Equals(second, StringComparison.OrdinalIgnoreCase) == true);
            if (!commandForm) return ["talk", text];
        }
        if (text.Length > 0 && ConversationConnected?.Invoke() == true &&
            (!commands.ContainsKey(name) || text.StartsWith("help me ", StringComparison.OrdinalIgnoreCase)))
            return ["talk", text];
        return Parse(line);
    }

    public CommandResult ExecuteTokens(IReadOnlyList<string> tokens)
    {
        try
        {
            if (tokens.Count == 0) return new("");
            // Route meeting shorthand before the generic connect window launcher.
            if (tokens.Count > 1 && tokens[0].Equals("connect", StringComparison.OrdinalIgnoreCase) && tokens[1].Equals("meetings", StringComparison.OrdinalIgnoreCase))
                tokens = new[] { "agent" }.Concat(tokens.Skip(1)).ToArray();
            if (!commands.TryGetValue(tokens[0], out var command))
                return new($"Unknown command '{tokens[0]}'. " +
                    (ConversationConnected?.Invoke() == false && commands.ContainsKey("talk") && tokens[0].ToLowerInvariant() is "create" or "read" or "update" or "delete" or "reate"
                        ? "No agent conversation is connected. Use connect MCP -inline, then repeat your file request."
                        : "Type help for available commands."));
            CommandResult Run()
            {
                if (command.Name != "talk" && tokens.Skip(1).Contains("-opninwin", StringComparer.OrdinalIgnoreCase))
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
