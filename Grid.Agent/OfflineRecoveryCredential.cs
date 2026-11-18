using System.Security.Cryptography;

namespace Grid.Communication;

/// <summary>Salted verifier for a user-held offline recovery file. Plaintext is never stored here.</summary>
public sealed record OfflineRecoveryCredential(int Version, Guid KeyId, string Salt, string Hash, int Iterations)
{
    public const int CurrentVersion = 1;
    public const int WorkFactor = PasswordCredential.WorkFactor;

    public static OfflineRecoveryCredential Create(Guid keyId, string secret)
    {
        if (keyId == Guid.Empty) throw new ArgumentException("Recovery key ID is required.");
        var secretBytes = DecodeSecret(secret);
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(secretBytes, salt, WorkFactor, HashAlgorithmName.SHA256, 32);
        try { return new(CurrentVersion, keyId, Convert.ToBase64String(salt), Convert.ToBase64String(hash), WorkFactor); }
        finally { CryptographicOperations.ZeroMemory(secretBytes); CryptographicOperations.ZeroMemory(hash); }
    }

    public bool Verify(string secret)
    {
        Validate(this);
        byte[] supplied;
        try { supplied = DecodeSecret(secret); }
        catch (ArgumentException) { return false; }
        var derived = Rfc2898DeriveBytes.Pbkdf2(supplied, Convert.FromBase64String(Salt), Iterations, HashAlgorithmName.SHA256, 32);
        try { return CryptographicOperations.FixedTimeEquals(derived, Convert.FromBase64String(Hash)); }
        finally { CryptographicOperations.ZeroMemory(supplied); CryptographicOperations.ZeroMemory(derived); }
    }

    public static void Validate(OfflineRecoveryCredential credential)
    {
        if (credential is null || credential.Version != CurrentVersion || credential.KeyId == Guid.Empty ||
            credential.Iterations != WorkFactor || credential.Salt is null || credential.Hash is null)
            throw new InvalidDataException("Invalid offline recovery credential.");
        try
        {
            if (Convert.FromBase64String(credential.Salt).Length != 16 || Convert.FromBase64String(credential.Hash).Length != 32)
                throw new FormatException();
        }
        catch (FormatException) { throw new InvalidDataException("Invalid offline recovery credential encoding."); }
    }

    private static byte[] DecodeSecret(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        try
        {
            var bytes = Convert.FromBase64String(secret);
            if (bytes.Length != 32) throw new FormatException();
            return bytes;
        }
        catch (FormatException) { throw new ArgumentException("Recovery secret must be a 256-bit encoded key."); }
    }
}
