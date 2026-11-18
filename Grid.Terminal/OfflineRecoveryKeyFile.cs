using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Grid.Communication;

namespace Grid.Terminal;

public sealed record OfflineRecoveryKeyDocument(string Contract, Guid KeyId, Guid WorldId,
    string Secret, DateTimeOffset CreatedUtc);

public static class OfflineRecoveryKeyFile
{
    public const string Contract = "Grid.AI.OfflineOwnerRecoveryKey.v1";
    public const string Extension = ".grid-recovery.json";

    public static (string Path, OfflineRecoveryKeyDocument Document, OfflineRecoveryCredential Verifier) Create(string target, Guid worldId)
    {
        var path = ValidateNewPath(target);
        var secretBytes = RandomNumberGenerator.GetBytes(32);
        var secret = Convert.ToBase64String(secretBytes);
        CryptographicOperations.ZeroMemory(secretBytes);
        var document = new OfflineRecoveryKeyDocument(Contract, Guid.NewGuid(), worldId, secret, DateTimeOffset.UtcNow);
        var verifier = OfflineRecoveryCredential.Create(document.KeyId, secret);
        var json = JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
        using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough))
        { var bytes = Encoding.UTF8.GetBytes(json); try { file.Write(bytes); file.Flush(true); } finally { CryptographicOperations.ZeroMemory(bytes); } }
        return (path, document, verifier);
    }

    public static OfflineRecoveryKeyDocument Read(string path)
    {
        path = ValidateExistingPath(path);
        var info = new FileInfo(path);
        if (info.Length is < 64 or > 4096) throw new InvalidDataException("Recovery key file size is invalid.");
        OfflineRecoveryKeyDocument? document;
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            document = JsonSerializer.Deserialize<OfflineRecoveryKeyDocument>(stream);
        if (document is null || document.Contract != Contract || document.KeyId == Guid.Empty || document.WorldId == Guid.Empty ||
            document.CreatedUtc.Offset != TimeSpan.Zero)
            throw new InvalidDataException("Recovery key file contract is invalid.");
        try { if (Convert.FromBase64String(document.Secret).Length != 32) throw new FormatException(); }
        catch (FormatException) { throw new InvalidDataException("Recovery key file secret encoding is invalid."); }
        return document;
    }

    public static string ValidateNewPath(string value)
    {
        var path = Normalize(value);
        if (!path.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Recovery file must end with {Extension}.");
        if (File.Exists(path) || Directory.Exists(path)) throw new IOException("Recovery file target already exists; overwrite is refused.");
        return path;
    }

    public static string ValidateExistingPath(string value)
    {
        var path = Normalize(value);
        if (!path.EndsWith(Extension, StringComparison.OrdinalIgnoreCase) || !File.Exists(path) || Directory.Exists(path))
            throw new IOException("Recovery key file does not exist or has the wrong extension.");
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("Recovery key file cannot be a link.");
        return path;
    }

    private static string Normalize(string value)
    {
        if (!Path.IsPathFullyQualified(value)) throw new ArgumentException("Use an absolute recovery-file path.");
        var path = Path.GetFullPath(value);
        var parent = Path.GetDirectoryName(path) ?? throw new ArgumentException("Recovery file requires a parent folder.");
        if (!Directory.Exists(parent)) throw new IOException("Recovery-file parent folder does not exist.");
        for (var current = parent; current is not null; current = Path.GetDirectoryName(current))
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Recovery file path cannot traverse a link or junction.");
        return path;
    }
}
