namespace Grid.Terminal;

public static class AiReadinessNotice
{
    public static string Summary(string reason)
    {
        var first = reason.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.TrimEnd('\r') ?? "Unknown local AI failure.";
        return TerminalTable.Safe(first.Length <= 300 ? first : first[..300] + "...");
    }
    public static string Format(bool ready, string reason) => ready ? "" :
        "WARNING: Grid.AI local AI unavailable.\n" + Summary(reason) +
        "\nAgent connections can open in limited fallback mode. Return to the main terminal and run ai status for full repair details.\n";
}
