using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Grid.Terminal;

public sealed record LocalTaskItem(Guid Id, string Title, bool Completed, DateTimeOffset UpdatedUtc);
public sealed record LocalTaskBoard(int Version, LocalTaskItem[] Items);
public sealed record LocalConnection(string Id, string Kind, string Source, string[] Scopes, bool Enabled,
    string SourceHash, DateTimeOffset ConnectedUtc)
{
    public string[] ReadGrants { get; init; } = [];
}
public sealed record LocalConnectionReceipt(DateTimeOffset AtUtc, string Connection, string Action, string Outcome);
public sealed record LocalConnectionState(int Version, string Binding, LocalConnection[] Connections, LocalConnectionReceipt[] Receipts);
public sealed record LocalConnectionDraft(Guid Id, string Connection, string SourceHash, LocalTaskBoard Before,
    LocalTaskBoard After, DateTimeOffset ExpiresUtc, bool Approved = false);

/// <summary>Offline, file-scoped connection catalog. It has no credential or network provider.</summary>
public sealed class LocalConnections
{
    private readonly string path, binding, artifacts;
    private readonly object gate = new();
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, MaxDepth = 10,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    public LocalConnections(string path, string binding)
    { this.path = Path.GetFullPath(path); this.binding = binding; artifacts = path + ".artifacts"; GridTransfer.CheckPath(this.path); }

    public LocalConnectionState Read()
    {
        lock (gate)
        {
            if (!File.Exists(path)) return new(1, binding, [], []);
            var state = JsonSerializer.Deserialize<LocalConnectionState>(Bounded(path, 1024 * 1024), Json) ?? throw new InvalidDataException("Invalid connection catalog.");
            if (state.Version != 1 || state.Connections is null || state.Connections.Length > 32 || state.Receipts is null || state.Receipts.Length > 100)
                throw new InvalidDataException("Unsupported or oversized connection catalog.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in state.Connections)
            {
                Validate(item);
                if (!ids.Add(item.Id)) throw new InvalidDataException("Duplicate connection alias.");
            }
            return state.Binding == binding ? state : state with { Binding = binding,
                Connections = state.Connections.Select(item => item with { Enabled = false, ReadGrants = [] }).ToArray() };
        }
    }
    public static (LocalTaskBoard Board, string Hash) InspectBoard(string source)
    {
        source = Source(source); var bytes = Bounded(source, 1024 * 1024);
        var board = JsonSerializer.Deserialize<LocalTaskBoard>(bytes, Json) ?? throw new InvalidDataException("Invalid task board.");
        Validate(board); return (board, Convert.ToHexString(SHA256.HashData(bytes)));
    }
    public void Add(string id, string source, string reviewedHash)
    {
        lock (gate)
        {
            if (!Alias(id)) throw new ArgumentException("Connection alias must use lowercase letters, numbers and hyphens.");
            source = Source(source); var inspected = InspectBoard(source);
            if (inspected.Hash != reviewedHash) throw new InvalidDataException("Task board changed since review.");
            var state = Read(); if (state.Connections.Any(item => item.Id == id)) throw new ArgumentException("Connection alias already exists.");
            if (state.Connections.Length >= 32) throw new InvalidOperationException("Connection catalog limit reached.");
            var item = new LocalConnection(id, "local-task-board", source, ["tasks.read", "tasks.draft", "tasks.commit"], false,
                reviewedHash, DateTimeOffset.UtcNow);
            Save(Receipt(state with { Connections = state.Connections.Append(item).ToArray() }, id, "add", "disabled"));
        }
    }
    public void Control(string id, string action, string? hash = null, string? role = null)
    {
        lock (gate)
        {
            var state = Read(); var item = state.Connections.SingleOrDefault(value => value.Id == id) ?? throw new ArgumentException("Unknown connection.");
            LocalConnection? next = action switch
            {
                "enable" when hash == item.SourceHash => item with { Enabled = true },
                "enable" => throw new ArgumentException("Review the exact source hash before enabling."),
                "disable" => item with { Enabled = false, ReadGrants = [] },
                "grant" when item.Enabled && Agent(role) => item with { ReadGrants = item.ReadGrants.Append(role!.ToUpperInvariant()).Distinct().ToArray() },
                "revoke" when Agent(role) => item with { ReadGrants = item.ReadGrants.Where(value => value != role!.ToUpperInvariant()).ToArray() },
                "remove" => null,
                _ => throw new ArgumentException("Invalid connection control request.")
            };
            Save(Receipt(state with { Connections = state.Connections.Where(value => value.Id != id).Concat(next is null ? [] : [next]).ToArray() },
                id, action, "completed"));
        }
    }
    public string ReadTasks(string id, string actor)
    {
        lock (gate)
        {
            var state = Read(); var item = state.Connections.SingleOrDefault(value => value.Id == id && value.Enabled)
                ?? throw new UnauthorizedAccessException("Connection missing or disabled.");
            actor = actor.ToUpperInvariant();
            if (actor != "MASTER" && !item.ReadGrants.Contains(actor)) throw new UnauthorizedAccessException("Connection is not granted to the active agent.");
            var observed = DateTimeOffset.UtcNow; var (board, hash) = InspectBoard(item.Source);
            var lines = board.Items.Take(100).Select(task => $"{task.Id} | {(task.Completed ? "completed" : "open")} | {Safe(task.Title)} | updated {task.UpdatedUtc:O} | citation {id}:{task.Id}");
            Save(Receipt(state, id, "read", "fresh"));
            return $"Connection: {id} | local/offline | observed {observed:O}\nSource: {item.Source}\nSource SHA256: {hash}\n" + string.Join("\n", lines);
        }
    }
    public LocalConnectionDraft DraftAdd(string id, string title)
    {
        lock (gate)
        {
            if (!Title(title)) throw new ArgumentException("Task title must contain 1..200 printable characters.");
            var state = Read(); var item = state.Connections.SingleOrDefault(value => value.Id == id && value.Enabled)
                ?? throw new UnauthorizedAccessException("Connection missing or disabled.");
            var (before, hash) = InspectBoard(item.Source); if (before.Items.Length >= 1000) throw new InvalidOperationException("Task board limit reached.");
            var task = new LocalTaskItem(Guid.NewGuid(), title.Trim(), false, DateTimeOffset.UtcNow);
            var draft = new LocalConnectionDraft(Guid.NewGuid(), id, hash, before, before with { Items = before.Items.Append(task).ToArray() }, DateTimeOffset.UtcNow.AddMinutes(2));
            Save(Receipt(state, id, "draft", "pending")); return draft;
        }
    }
    public void Commit(LocalConnectionDraft draft)
    {
        lock (gate)
        {
            if (!draft.Approved || DateTimeOffset.UtcNow >= draft.ExpiresUtc) throw new UnauthorizedAccessException("Draft approval is missing or expired.");
            var state = Read(); var item = state.Connections.SingleOrDefault(value => value.Id == draft.Connection && value.Enabled)
                ?? throw new UnauthorizedAccessException("Connection missing or disabled.");
            var current = InspectBoard(item.Source); if (current.Hash != draft.SourceHash) throw new IOException("Source changed after preview; draft refused.");
            Directory.CreateDirectory(artifacts); var backup = Path.Combine(artifacts, draft.Id + ".before.json");
            File.Copy(item.Source, backup, false); WriteAtomic(item.Source, draft.After);
            var nextHash = InspectBoard(item.Source).Hash;
            Save(Receipt(state with { Connections = state.Connections.Where(value => value.Id != item.Id)
                .Append(item with { SourceHash = nextHash }).ToArray() }, item.Id, "commit", "completed"));
        }
    }
    private static void Validate(LocalConnection item)
    {
        if (item is null || !Alias(item.Id) || item.Kind != "local-task-board" || item.Scopes is null ||
            !item.Scopes.SequenceEqual(["tasks.read", "tasks.draft", "tasks.commit"]) || item.ReadGrants is null ||
            item.ReadGrants.Any(role => !Agent(role)) || item.ReadGrants.Distinct().Count() != item.ReadGrants.Length ||
            item.SourceHash.Length != 64) throw new InvalidDataException("Invalid local connection.");
        _ = StoredSource(item.Source);
    }
    private static void Validate(LocalTaskBoard board)
    {
        if (board.Version != 1 || board.Items is null || board.Items.Length > 1000 || board.Items.Select(item => item.Id).Distinct().Count() != board.Items.Length ||
            board.Items.Any(item => item.Id == Guid.Empty || !Title(item.Title) || item.UpdatedUtc.Offset != TimeSpan.Zero))
            throw new InvalidDataException("Invalid local task board.");
    }
    private static string Source(string value)
    {
        var path = StoredSource(value);
        if (!File.Exists(path) || Directory.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("Task-board source is missing or linked.");
        return path;
    }
    private static string StoredSource(string value)
    {
        if (!Path.IsPathFullyQualified(value) || !value.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) throw new IOException("Use an absolute local JSON task-board path.");
        var path = Path.GetFullPath(value); GridTransfer.CheckPath(path);
        for (var parent = Path.GetDirectoryName(path); parent is not null; parent = Path.GetDirectoryName(parent))
            if ((File.GetAttributes(parent) & FileAttributes.ReparsePoint) != 0) throw new IOException("Task-board path cannot traverse a link or junction.");
        return path;
    }
    private static byte[] Bounded(string path, int max) { using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read); if (file.Length > max) throw new InvalidDataException("Connection data exceeds its limit."); var bytes = new byte[(int)file.Length]; file.ReadExactly(bytes); return bytes; }
    private static bool Alias(string? value) => value is not null && Regex.IsMatch(value, "^[a-z][a-z0-9-]{0,47}$", RegexOptions.CultureInvariant);
    private static bool Agent(string? role) => role is "MCP" or "MAXIS" or "SENTINEL" or "JN" or "VOLT" or "RELAY";
    private static bool Title(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 200 && !value.Any(char.IsControl);
    private static string Safe(string value) => new(value.Select(character => char.IsControl(character) ? ' ' : character).ToArray());
    private static LocalConnectionState Receipt(LocalConnectionState state, string id, string action, string outcome) => state with
    { Receipts = state.Receipts.Append(new(DateTimeOffset.UtcNow, id, action, outcome)).TakeLast(100).ToArray() };
    private void Save(LocalConnectionState state) => WriteAtomic(path, state);
    private static void WriteAtomic<T>(string target, T value)
    {
        var temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough)) { JsonSerializer.Serialize(file, value, Json); file.Flush(true); } File.Move(temporary, target, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
