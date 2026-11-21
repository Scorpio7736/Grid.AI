using System.Text.Json;

namespace Grid.Agent;

public enum ConversationIntentKind { CreateFile, UpdateFile, DeleteFile, ReadFile, SearchFiles, ObserveHost, PlanTasks, Clarify, Unsupported }
public sealed record ConversationIntent(int Version, ConversationIntentKind Kind, string Scope, string Target, string Text)
{
    public const int CurrentVersion = 1;
    public static ConversationIntent Parse(string json)
    {
        if (json.Length > 12000) throw new ArgumentException("Intent exceeds its size limit.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new ArgumentException("Expected an intent object.");
        var fields = root.EnumerateObject().ToArray();
        var expected = new[] { "version", "kind", "scope", "target", "text" };
        if (fields.Length != expected.Length || fields.Select(f => f.Name).Distinct().Count() != fields.Length || fields.Any(f => !expected.Contains(f.Name)))
            throw new ArgumentException("Unexpected or duplicate intent fields.");
        if (root.GetProperty("version").ValueKind != JsonValueKind.Number || !root.GetProperty("version").TryGetInt32(out var version) || version != CurrentVersion)
            throw new ArgumentException("Unsupported conversation intent version.");
        foreach (var field in fields.Where(f => f.Name != "version"))
            if (field.Value.ValueKind != JsonValueKind.String) throw new ArgumentException("Intent arguments must be strings.");
        var kindText = root.GetProperty("kind").GetString();
        if (!Enum.TryParse<ConversationIntentKind>(kindText, false, out var kind) || !Enum.IsDefined(kind) || int.TryParse(kindText, out _))
            throw new ArgumentException("Unsupported intent kind.");
        var scope = root.GetProperty("scope").GetString()!; var target = root.GetProperty("target").GetString()!; var text = root.GetProperty("text").GetString()!;
        if (scope.Length > 1024 || target.Length > 1024 || text.Length > 4096 || scope.Any(char.IsControl) || target.Any(char.IsControl) || text.Contains('\0'))
            throw new ArgumentException("Invalid intent argument size or control characters.");
        if (kind is ConversationIntentKind.CreateFile or ConversationIntentKind.UpdateFile or ConversationIntentKind.DeleteFile or ConversationIntentKind.ReadFile or ConversationIntentKind.SearchFiles)
        { if (string.IsNullOrWhiteSpace(scope) || string.IsNullOrWhiteSpace(target)) throw new ArgumentException("File intents require a destination and target; clarify missing values."); }
        else if (kind == ConversationIntentKind.ObserveHost)
        {
            if (string.IsNullOrWhiteSpace(scope) || string.IsNullOrWhiteSpace(target))
                throw new ArgumentException("Host observations require a registered access path and argument.");
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Host observations require an AI-written permission question.");
        }
        else if (scope.Length != 0 || target.Length != 0) throw new ArgumentException("This intent cannot carry a file destination.");
        if (kind is ConversationIntentKind.Clarify or ConversationIntentKind.Unsupported && string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Clarification requires an explanation.");
        return new(version, kind, scope, target, text);
    }
}
