namespace Grid.Terminal;

public static class AiReadinessNotice
{
    public static string Format(bool ready, string reason) => ready ? "" :
        "WARNING: AI unavailable — setup or repair required.\n" +
        string.Join("\n", reason.Split('\n').Select(line => TerminalTable.Safe(line.TrimEnd('\r')))) +
        "\nAgent connections can open, but AI replies are unavailable; only basic fallback replies are possible. " +
        "Simulation startup does not enable AI. Return to the main terminal and run ai status for details.\n";
}
