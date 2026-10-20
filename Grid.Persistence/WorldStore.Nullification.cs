namespace Grid.Persistence;

public sealed record NullificationPlan(Guid WorldId, string WorldName, IReadOnlyList<string> Paths);
public interface IWorldNullificationSession
{
    NullificationPlan PrepareNullification();
    Task NullifyAsync(NullificationPlan plan);
}

public sealed partial class WorldStore
{
    private NullificationPlan? issuedNullification;
    private bool nullificationStarted;
    private string NullificationMarker => DatabasePath + ".nullifying";
    private string[] NullificationPaths => [BackupPath, DatabasePath + "-journal", DatabasePath + "-wal", DatabasePath + "-shm", DatabasePath];

    public NullificationPlan PrepareNullification(Guid expectedWorld)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            EnsureNotRecovering();
            ValidateNullificationTargets(expectedWorld);
            var world = Read(DatabasePath);
            return issuedNullification = new(world.Info.Id, world.Info.Name, Array.AsReadOnly(NullificationPaths));
        }
    }

    private void ValidateNullificationTargets(Guid expectedWorld)
    {
        // Refuse redirected paths: lexical adjacency alone is insufficient for destructive work.
        foreach (var path in NullificationPaths.Append(NullificationMarker))
        {
            for (var current = path; current is not null; current = Path.GetDirectoryName(current))
                if ((File.Exists(current) || Directory.Exists(current)) &&
                    (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("NGE refuses symbolic-link or junction targets.");
            if (Directory.Exists(path)) throw new IOException($"NGE target is a directory: {path}");
        }
        if (Read(DatabasePath).Info.Id != expectedWorld ||
            File.Exists(BackupPath) && Read(BackupPath).Info.Id != expectedWorld)
            throw new InvalidDataException("NGE world identity changed or backup belongs to another world.");
    }

    public void Nullify(NullificationPlan plan)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            EnsureNotRecovering();
            if (!ReferenceEquals(plan, issuedNullification))
                throw new InvalidOperationException("NGE requires this session's fresh confirmation plan.");
            ValidateNullificationTargets(plan.WorldId);
            nullificationStarted = true;
            // Retain this marker on interruption/failure so neither startup nor recovery resurrects the world.
            using (var marker = new FileStream(NullificationMarker, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough))
            {
                marker.Write(System.Text.Encoding.UTF8.GetBytes($"NGE interrupted or in progress. World: {plan.WorldId:D}"));
                marker.Flush(flushToDisk: true);
            }
            try
            {
                foreach (var path in NullificationPaths) File.Delete(path);
                File.Delete(NullificationMarker);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                var remaining = NullificationPaths.Where(File.Exists);
                throw new IOException("NGE incomplete. No final save will occur. Remaining files: " +
                    string.Join(", ", remaining) + ". Nullification marker retained; manual review required.", ex);
            }
        }
    }
}
