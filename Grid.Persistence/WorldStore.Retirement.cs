using System.Security.Cryptography;
using System.Text.Json;

namespace Grid.Persistence;

public sealed record RetirementFile(string Source, string Name, string Sha256, long Bytes);
public sealed record WorldRetirementPlan(Guid Id, Guid WorldId, string Destination, DateTimeOffset ExpiresUtc, IReadOnlyList<RetirementFile> Files);

public sealed partial class WorldStore
{
    private WorldRetirementPlan? retirementPlan;
    private string RetirementMarker => DatabasePath + ".retired";

    public WorldRetirementPlan PrepareRetirement(Guid worldId, string destination)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this); EnsureNotRecovering();
            if (new[] { "-wal", "-shm", "-journal" }.Any(s => File.Exists(DatabasePath + s)))
                throw new IOException("SQLite sidecars remain. Close/checkpoint the world before retirement; nothing moved.");
            ValidateNullificationTargets(worldId);
            destination = RetirementPath(destination);
            if (Directory.Exists(destination) || File.Exists(destination) || !Directory.Exists(Path.GetDirectoryName(destination)))
                throw new IOException("Choose a new quarantine directory in an existing local parent.");
            if (!string.Equals(Path.GetPathRoot(destination), Path.GetPathRoot(DatabasePath), StringComparison.OrdinalIgnoreCase))
                throw new IOException("Retirement requires same-volume quarantine; copy/transfer uses its separate verified workflow.");
            var parent = Path.GetDirectoryName(DatabasePath)!;
            if (destination.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                Directory.Exists(Path.Combine(parent, ".git")) || Directory.Exists(Path.Combine(Path.GetDirectoryName(destination)!, ".git")))
                throw new IOException("Quarantine must be outside the world directory and cannot be a repository root.");
            foreach (var path in NullificationPaths.Append(RetirementMarker)) RetirementPath(path);
            if (new[] { "-wal", "-shm", "-journal" }.Any(s => File.Exists(DatabasePath + s)))
                throw new IOException("SQLite sidecars remain. Close/checkpoint the world before retirement; nothing moved.");
            var files = new[] { DatabasePath, BackupPath }.Where(File.Exists).Select(path =>
                new RetirementFile(path, Path.GetFileName(path), RetirementHash(path), new FileInfo(path).Length)).ToArray();
            return retirementPlan = new(Guid.NewGuid(), worldId, destination, DateTimeOffset.UtcNow.AddMinutes(2), Array.AsReadOnly(files));
        }
    }

    public string Retire(WorldRetirementPlan plan)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this); EnsureNotRecovering();
            if (!ReferenceEquals(plan, retirementPlan) || DateTimeOffset.UtcNow >= plan.ExpiresUtc)
                throw new InvalidOperationException("Retirement requires this stopped world's fresh reviewed plan.");
            retirementPlan = null;
            if (new[] { "-wal", "-shm", "-journal" }.Any(s => File.Exists(DatabasePath + s)))
                throw new IOException("SQLite sidecars appeared after review.");
            ValidateNullificationTargets(plan.WorldId);
            RetirementPath(plan.Destination);
            if (Directory.Exists(plan.Destination) || File.Exists(plan.Destination)) throw new IOException("Quarantine destination changed.");
            foreach (var file in plan.Files)
                if (RetirementHash(RetirementPath(file.Source)) != file.Sha256) throw new IOException("World changed after retirement review.");
            if (new[] { "-wal", "-shm", "-journal" }.Any(s => File.Exists(DatabasePath + s))) throw new IOException("SQLite sidecars appeared after review.");
            Directory.CreateDirectory(plan.Destination);
            var receipt = Path.Combine(plan.Destination, "retirement.json");
            var document = JsonSerializer.Serialize(new { Version = 1, Plan = plan,
                Recovery = "Compare the listed hashes in source and quarantine. Never regenerate the source. To restore, close all hosts, restore the verified files to their exact source paths, then archive the .retired marker. No purge or automatic retry is provided." }, new JsonSerializerOptions { WriteIndented = true });
            WriteRetirementFile(receipt, document);
            // A durable source tombstone blocks regeneration after success or any interrupted move.
            WriteRetirementFile(RetirementMarker, document);
            foreach (var file in plan.Files)
            {
                RetirementPath(file.Source); RetirementPath(plan.Destination);
                File.Move(file.Source, Path.Combine(plan.Destination, file.Name), overwrite: false);
            }
            return receipt;
        }
    }

    private static string RetirementPath(string path)
    {
        if (!Path.IsPathFullyQualified(path) || path.StartsWith(@"\\", StringComparison.Ordinal) || path.Any(char.IsControl))
            throw new IOException("Retirement requires an absolute local path.");
        path = Path.GetFullPath(path);
        if (path == Path.GetPathRoot(path) || path.Equals(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), StringComparison.OrdinalIgnoreCase) ||
            path[Path.GetPathRoot(path)!.Length..].Contains(':')) throw new IOException("Unsafe retirement path.");
        for (var current = path; current is not null; current = Path.GetDirectoryName(current))
            if ((File.Exists(current) || Directory.Exists(current)) && File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
                throw new IOException("Retirement refuses linked files/directories.");
        return path;
    }
    private static string RetirementHash(string path)
    { using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read); return Convert.ToHexString(SHA256.HashData(file)); }
    private static void WriteRetirementFile(string path, string text)
    {
        RetirementPath(path);
        using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
        file.Write(System.Text.Encoding.UTF8.GetBytes(text)); file.Flush(true);
    }
}
