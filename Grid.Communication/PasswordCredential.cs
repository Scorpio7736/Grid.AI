using System.Security.Cryptography;
using System.Text;
namespace Grid.Communication;

public sealed record PasswordCredential(string Username, string Salt, string Hash, int Iterations)
{
    public const int WorkFactor = 600000;
    public const int MinimumPasswordLength = 6;
    public const int MaximumPasswordLength = 24;
    // Existing hashes may have been created under the original 256-character limit.
    public const int LegacyMaximumPasswordLength = 256;
    public static PasswordCredential Create(string username, string password)
    {
        username = username.Trim().Normalize(NormalizationForm.FormC);
        if (string.IsNullOrWhiteSpace(username) || username.Length > 64 || username.Any(char.IsControl)) throw new ArgumentException("Username must contain 1..64 printable characters.");
        if (password.Length is < MinimumPasswordLength or > MaximumPasswordLength || password.Any(char.IsControl)) throw new ArgumentException($"Password must contain {MinimumPasswordLength}..{MaximumPasswordLength} characters without control characters. Uppercase, lowercase, numbers and symbols are allowed; no mixture is required.");
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, WorkFactor, HashAlgorithmName.SHA256, 32);
        return new(username, Convert.ToBase64String(salt), Convert.ToBase64String(hash), WorkFactor);
    }
    public static void Validate(PasswordCredential c)
    {
        if (c is null || string.IsNullOrWhiteSpace(c.Username) || c.Username.Length > 64 || c.Username.Any(char.IsControl) || c.Iterations != WorkFactor || c.Salt is null || c.Hash is null)
            throw new InvalidDataException("Invalid local credential record.");
        try { if (Convert.FromBase64String(c.Salt).Length != 16 || Convert.FromBase64String(c.Hash).Length != 32) throw new FormatException(); }
        catch (FormatException) { throw new InvalidDataException("Invalid credential encoding."); }
    }
    public bool Verify(string username, string password)
    {
        Validate(this);
        if (password.Length > LegacyMaximumPasswordLength || username.Length > 64) return false;
        var derived = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(Salt), Iterations, HashAlgorithmName.SHA256, 32);
        try { return CryptographicOperations.FixedTimeEquals(derived, Convert.FromBase64String(Hash)) & string.Equals(Username, username.Trim().Normalize(NormalizationForm.FormC), StringComparison.OrdinalIgnoreCase); }
        finally { CryptographicOperations.ZeroMemory(derived); }
    }
}
