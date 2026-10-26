namespace Grid.Terminal;

public enum ConversationTone { Heading, User, Agent, Error }
public sealed record ConversationSpan(int Start,int Length,ConversationTone Tone);
public interface IConversationOutput
{
    Task WriteConversationAsync(string text,ConsoleColor color);
}
public static class ConversationPresentation
{
    public static ConsoleColor Color(ConversationTone tone) => tone switch { ConversationTone.User=>ConsoleColor.White, ConversationTone.Agent=>ConsoleColor.Yellow, ConversationTone.Error=>ConsoleColor.Red, _=>ConsoleColor.Cyan };
    public static CommandResult AgentReply(string heading,string body,string footer="")
    {
        var text=heading+"\n\n"+body+(footer.Length==0 ? "" : "\n\n"+footer);
        return new(text,AnimateFrom:heading.Length+2,Presentation:[new(0,heading.Length,ConversationTone.Heading),new(heading.Length+2,body.Length,ConversationTone.Agent)]);
    }
    public static IReadOnlyList<ConversationSpan> Shift(IReadOnlyList<ConversationSpan>? spans,int offset) =>
        spans?.Select(s=>s with { Start=s.Start+offset }).ToArray() ?? [];
}
