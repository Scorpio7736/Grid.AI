using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace Grid.Simulation.Tasks;

/// <summary>Bounded, single-file review/apply/undo. Not a security boundary against concurrent OS processes.</summary>
public enum WorkspaceFileOperation { Create, Update, Delete }

public sealed class WorkspaceFileChange
{
    public const int MaximumBytes = 65536;
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private readonly byte[]? original;
    private readonly byte[] candidate;
    public Guid Id { get; private set; } = Guid.NewGuid();
    public WorkspaceFileOperation Operation { get; private set; }
    public bool DeletesFile => Operation == WorkspaceFileOperation.Delete;
    public string Root { get; }
    public string Relative { get; }
    public string Before => original is null ? "" : Decode(original);
    public string After => Decode(candidate);
    public string BeforeHash => original is null ? "ABSENT" : Hash(original);
    public string AfterHash => DeletesFile ? "ABSENT" : Hash(candidate);
    public bool CreatesFile => original is null;
    public bool HighRisk => DeletesFile || !new[] { ".md", ".txt" }.Contains(Path.GetExtension(Relative).ToLowerInvariant());
    public bool Applied { get; private set; }
    public bool RolledBack { get; private set; }
    public string? Evidence { get; private set; }
    public string? ApprovalEvidence { get; private set; }
    public string? ReportWarning { get; private set; }
    private WorkspaceFileChange(string root, string relative, byte[]? before, byte[] after)
    { Root = root; Relative = relative; original = before; candidate = after; Operation = before is null ? WorkspaceFileOperation.Create : WorkspaceFileOperation.Update; }

    public static void ValidateDestinationRoot(string root)
    {
        if (!Path.IsPathFullyQualified(root)) throw new ArgumentException("Specify an absolute destination folder.");
        ValidateParts(root[Path.GetPathRoot(root)!.Length..].TrimEnd('\\', '/'));
    }
    private static void ValidateParts(string relative)
    {
        var parts = relative.Split(['/', '\\']);
        var reserved = new[] { ".git", ".svn", ".hg", ".codex", ".agents", ".grid-runtime", "workspace-edits", "bin", "obj" };
        if (parts.Any(p => p.EndsWith('.') || p.EndsWith(' ') || p.Any(c => char.IsControl(c) || char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.Format || "<>\"*?".Contains(c)) ||
            reserved.Contains(p, StringComparer.OrdinalIgnoreCase) || p.EndsWith(".maxis-artifacts", StringComparison.OrdinalIgnoreCase) ||
            System.Text.RegularExpressions.Regex.IsMatch(p, @"^(CON|PRN|AUX|NUL|COM[0-9¹²³]|LPT[0-9¹²³])(?:\.|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase)))
            throw new ArgumentException("Reserved, generated or ambiguous paths cannot be edited.");
    }
    public static string Target(string root, string relative)
    {
        if (!DevelopmentPolicy.Relative(relative)) throw new ArgumentException("Use a workspace-relative file path.");
        ValidateParts(relative);
        var allowed = new[] { ".md", ".txt", ".cs", ".csproj", ".sln", ".slnx", ".props", ".targets", ".json", ".xml", ".yml", ".yaml", ".html", ".css", ".js", ".ts", ".tsx", ".jsx", ".py", ".sql" };
        if (!allowed.Contains(Path.GetExtension(relative).ToLowerInvariant())) throw new ArgumentException("Unsupported editable text extension.");
        var path = DevelopmentPolicy.CheckedPath(root, relative, fileDestination: true);
        if (!Directory.Exists(Path.GetDirectoryName(path))) throw new IOException("Parent directory must already exist.");
        if (Directory.Exists(path)) throw new IOException("Target is a directory.");
        return path;
    }
    public static byte[]? Read(string path)
    {
        if (!File.Exists(path)) return null;
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (file.Length > MaximumBytes) throw new IOException("Editable files are limited to 64 KiB.");
        var bytes = new byte[(int)file.Length]; file.ReadExactly(bytes); Decode(bytes); return bytes;
    }
    public static string Decode(byte[] bytes)
    {
        var offset = bytes.AsSpan().StartsWith(new byte[] { 239, 187, 191 }) ? 3 : 0;
        var value = Utf8.GetString(bytes, offset, bytes.Length - offset);
        if (value.Contains('\0')) throw new IOException("Binary text is unsupported.");
        return value;
    }
    public static WorkspaceFileChange Stage(string root, string relative, string content, bool allowEmpty = false)
    {
        if (!allowEmpty && string.IsNullOrWhiteSpace(content) || content.Contains('\0')) throw new ArgumentException("Empty/binary replacements are refused; use an explicit delete request.");
        var path = Target(root, relative); var before = Read(path);
        var encoded = Utf8.GetBytes(content);
        if (before is not null && before.AsSpan().StartsWith(new byte[] { 239, 187, 191 })) encoded = new byte[] { 239, 187, 191 }.Concat(encoded).ToArray();
        if (encoded.Length > MaximumBytes) throw new ArgumentException("Candidate exceeds 64 KiB.");
        if (before is not null && before.SequenceEqual(encoded)) throw new ArgumentException("No change to stage.");
        return new(root, relative, before, encoded);
    }
    public static WorkspaceFileChange StageOperation(string root, string relative, WorkspaceFileOperation operation, string? content = null)
    {
        if (!Enum.IsDefined(operation)) throw new ArgumentException("Unknown file operation.");
        if (operation == WorkspaceFileOperation.Delete)
        {
            var before = Read(Target(root, relative)) ?? throw new IOException("Delete target does not exist.");
            return new(root, relative, before, []) { Operation = operation };
        }
        if (operation == WorkspaceFileOperation.Create && File.Exists(Target(root, relative)))
            throw new IOException("Create target already exists: " + Display(Target(root, relative)) + ". Nothing changed. Read it, choose a new filename, or request an update.");
        var change = Stage(root, relative, content ?? throw new ArgumentException("Provide complete content."), allowEmpty: true);
        if (operation == WorkspaceFileOperation.Create && !change.CreatesFile)
            throw new IOException("Create refuses an existing destination. Request an update and review it instead.");
        if (operation == WorkspaceFileOperation.Update && change.CreatesFile)
            throw new IOException("Update target does not exist. Request create instead.");
        return change;
    }

    // Recovery is explicit and limited to a known change ID inside this host's artifact store.
    // Unapplied approvals are never restored after restart.
    public static WorkspaceFileChange Recover(string artifactsRoot, string root, Guid id)
    {
        var folder = DevelopmentPolicy.CheckedPath(artifactsRoot, id.ToString("N"));
        var report = Read(DevelopmentPolicy.CheckedPath(folder, "report.json")) ?? throw new IOException("Recovery report missing.");
        using var json = JsonDocument.Parse(report);
        var record = json.RootElement;
        if (record.GetProperty("Id").GetGuid() != id || !string.Equals(Path.GetFullPath(record.GetProperty("Root").GetString()!), Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase))
            throw new IOException("Recovery record does not belong to this workspace/change.");
        var relative = record.GetProperty("Relative").GetString()!;
        _ = Target(root, relative);
        var beforeHash = record.GetProperty("BeforeHash").GetString();
        var afterHash = record.GetProperty("AfterHash").GetString();
        var before = beforeHash == "ABSENT" ? null : Read(DevelopmentPolicy.CheckedPath(folder, "original.bak")) ?? throw new IOException("Recovery original missing.");
        var candidate = Read(DevelopmentPolicy.CheckedPath(folder, "candidate.bak")) ?? throw new IOException("Recovery candidate missing.");
        var operation = afterHash == "ABSENT" ? WorkspaceFileOperation.Delete : before is null ? WorkspaceFileOperation.Create : WorkspaceFileOperation.Update;
        var change = new WorkspaceFileChange(root, relative, before, candidate) { Id = id, Operation = operation, Evidence = folder,
            ApprovalEvidence = record.TryGetProperty("ApprovalEvidence", out var approval) ? approval.GetString() : null };
        if (change.BeforeHash != beforeHash || change.AfterHash != afterHash || operation == WorkspaceFileOperation.Delete && (before is null || candidate.Length != 0))
            throw new IOException("Recovery backup hash mismatch. Nothing restored.");
        var phase = record.GetProperty("Phase").GetString();
        if (phase == "RolledBack") { change.Applied = true; change.RolledBack = true; }
        else if (phase == "Applied") change.Applied = true;
        else if (phase == "Prepared")
        {
            var current = Read(Target(root, relative));
            if ((current is null ? "ABSENT" : Hash(current)) != afterHash)
                throw new IOException("Prepared change is not in its expected applied state; inspect retained evidence manually. No automatic retry.");
            change.Applied = true;
            change.ReportWarning = "Recovered an applied state from an incomplete final report; backup hashes verified.";
        }
        else throw new IOException("Unknown recovery phase.");
        return change;
    }
    public void VerifyUnchanged()
    {
        var current = Read(Target(Root, Relative));
        if (BeforeHash != (current is null ? "ABSENT" : Hash(current))) throw new IOException("Target changed since preview. Stage and approve a fresh draft.");
    }
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static void Durable(string path, byte[] bytes)
    {
        using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough);
        file.Write(bytes); file.Flush(true);
    }
    private void Journal(string phase)
    {
        var report = DevelopmentPolicy.CheckedPath(Evidence!, "report.json");
        var temporary = DevelopmentPolicy.CheckedPath(Evidence!, Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            Durable(temporary, Utf8.GetBytes(JsonSerializer.Serialize(new
            {
                Id,
                Root,
                Relative,
                BeforeHash,
                AfterHash,
                CreatesFile,
                Operation = Operation.ToString(),
                ApprovalEvidence,
                Phase = phase,
                Utc = DateTimeOffset.UtcNow
            }, new JsonSerializerOptions { WriteIndented = true })));
            File.Move(temporary, report, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public void Apply(string artifactsRoot, string approvalEvidence)
    {
        if (Applied || RolledBack || Evidence is not null) throw new InvalidOperationException("This draft was already attempted. Stage again instead of replaying.");
        VerifyUnchanged();
        ApprovalEvidence = approvalEvidence;
        var target = Target(Root, Relative);
        var folder = DevelopmentPolicy.CheckedPath(artifactsRoot, Id.ToString("N"));
        if (Directory.Exists(folder) || File.Exists(folder)) throw new IOException("Recovery directory already exists; refusing to overwrite evidence.");
        Directory.CreateDirectory(folder);
        Evidence = folder;
        // Write durable recovery material before changing the source.
        if (original is not null) Durable(DevelopmentPolicy.CheckedPath(folder, "original.bak"), original);
        Durable(DevelopmentPolicy.CheckedPath(folder, "candidate.bak"), candidate);
        Journal("Prepared");
        var temporary = DevelopmentPolicy.CheckedPath(Root, Relative + "." + Id.ToString("N") + ".grid-tmp", fileDestination: true);
        try
        {
            if (!DeletesFile) Durable(temporary, candidate);
            VerifyUnchanged();
            if (DeletesFile) File.Delete(target);
            else if (CreatesFile) File.Move(temporary, target, false);
            else File.Replace(temporary, target, null);
            Applied = true;
            try { Journal("Applied"); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { ReportWarning = "File changed but final report update failed: " + e.Message; }
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public void Rollback()
    {
        if (!Applied || RolledBack) throw new InvalidOperationException("Only an applied, not-yet-undone change can be rolled back.");
        var target = Target(Root, Relative);
        var current = Read(target);
        if ((current is null ? "ABSENT" : Hash(current)) != AfterHash) throw new IOException("Target changed after apply. Rollback refused to preserve newer work.");
        var temporary = DevelopmentPolicy.CheckedPath(Root, Relative + "." + Id.ToString("N") + ".grid-tmp", fileDestination: true);
        try
        {
            if (DeletesFile)
            {
                Durable(temporary, original!);
                if (Read(Target(Root, Relative)) is not null) throw new IOException("Target reappeared; rollback refused.");
                File.Move(temporary, target, false);
            }
            else if (original is null)
            {
                // Explicit rollback of a newly created file; candidate.bak remains as recovery evidence.
                File.Delete(target);
            }
            else
            {
                Durable(temporary, original);
                var checkedCurrent = Read(Target(Root, Relative));
                if (checkedCurrent is null || Hash(checkedCurrent) != AfterHash) throw new IOException("Target changed during rollback.");
                File.Replace(temporary, target, null);
            }
            RolledBack = true;
            try { Journal("RolledBack"); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { ReportWarning = "Rollback completed but report update failed: " + e.Message; }
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public static string Display(string text) => string.Concat(text.Select(c => (char.IsControl(c) && c is not ('\r' or '\n' or '\t')) || char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.Format ? "\\u" + ((int)c).ToString("X4") : c.ToString()));
    public string Preview() => $"Change {Id}\n{(DeletesFile ? "DELETE (1 file; recoverable from durable original.bak)" : CreatesFile ? "CREATE" : "REPLACE")} {Root} / {Relative}\nAbsolute path: {Display(Path.GetFullPath(Path.Combine(Root, Relative)))}\nRisk: {(HighRisk ? "HIGH - code/configuration can affect execution" : "text/document")}\nBefore SHA256: {BeforeHash}\nAfter SHA256: {AfterHash}\n--- BEFORE (complete)\n" +
        string.Join("\n", Display(Before).Replace("\r\n", "\n").Split('\n').Select(l => "- " + l)) +
        "\n+++ AFTER (complete)\n" + string.Join("\n", Display(After).Replace("\r\n", "\n").Split('\n').Select(l => "+ " + l)) +
        "\nNo change applied. Review all content before approval.";
}

