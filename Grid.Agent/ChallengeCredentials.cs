using System.Security.Cryptography;
using System.Text;

namespace Grid.Communication;

public sealed record ChallengeCredential(Guid Id, string Prompt, string Salt, string Hash, int Iterations)
{
    public bool Verify(string response)
    {
        ChallengeCredentialSet.ValidateCredential(this);
        byte[] derived;
        try
        {
            derived = Rfc2898DeriveBytes.Pbkdf2(
                ChallengeCredentialSet.NormalizeResponse(response),
                Convert.FromBase64String(Salt), Iterations, HashAlgorithmName.SHA256, 32);
        }
        catch (ArgumentException) { return false; }
        try { return CryptographicOperations.FixedTimeEquals(derived, Convert.FromBase64String(Hash)); }
        finally { CryptographicOperations.ZeroMemory(derived); }
    }
}

public sealed record ChallengeCredentialSet(int Version, IReadOnlyList<ChallengeCredential> Challenges)
{
    public const int CurrentVersion = 1;
    public const int RequiredCount = 3;
    public const int MinimumPromptLength = 1;
    public const int MaximumPromptLength = 64;
    public const int MinimumResponseLength = 1;
    public const int MaximumResponseLength = 64;
    public const int WorkFactor = PasswordCredential.WorkFactor;

    public static ChallengeCredentialSet Create(
        IReadOnlyList<(string Prompt, string Response)> entries,
        string username,
        string password)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Count != RequiredCount)
            throw new ArgumentException($"Exactly {RequiredCount} challenge prompts and responses are required.");

        var normalizedUsername = NormalizeComparable(username);
        var normalizedPassword = NormalizeComparable(password);
        var prompts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var responses = new HashSet<string>(StringComparer.Ordinal);
        var challenges = new List<ChallengeCredential>(RequiredCount);
        foreach (var (rawPrompt, rawResponse) in entries)
        {
            var prompt = NormalizePrompt(rawPrompt);
            var response = NormalizeResponse(rawResponse);
            if (!prompts.Add(prompt)) throw new ArgumentException("Challenge prompts must be distinct.");
            if (response == normalizedUsername || response == normalizedPassword)
                throw new ArgumentException("Challenge responses must not match the username or password.");
            if (!responses.Add(response)) throw new ArgumentException("Challenge responses must be distinct.");
            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = Rfc2898DeriveBytes.Pbkdf2(response, salt, WorkFactor, HashAlgorithmName.SHA256, 32);
            try
            {
                challenges.Add(new(Guid.NewGuid(), prompt, Convert.ToBase64String(salt),
                    Convert.ToBase64String(hash), WorkFactor));
            }
            finally { CryptographicOperations.ZeroMemory(hash); }
        }
        var result = new ChallengeCredentialSet(CurrentVersion, challenges.AsReadOnly());
        Validate(result);
        return result;
    }

    public static void Validate(ChallengeCredentialSet set)
    {
        if (set is null || set.Version != CurrentVersion || set.Challenges is null ||
            set.Challenges.Count != RequiredCount || set.Challenges.Select(c => c.Id).Distinct().Count() != RequiredCount)
            throw new InvalidDataException("Invalid challenge credential set.");
        var prompts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var challenge in set.Challenges)
        {
            ValidateCredential(challenge);
            if (!prompts.Add(challenge.Prompt)) throw new InvalidDataException("Challenge prompts must be distinct.");
        }
    }

    internal static void ValidateCredential(ChallengeCredential credential)
    {
        if (credential is null || credential.Id == Guid.Empty || credential.Iterations != WorkFactor ||
            NormalizePrompt(credential.Prompt) != credential.Prompt || credential.Salt is null || credential.Hash is null)
            throw new InvalidDataException("Invalid challenge credential.");
        try
        {
            if (Convert.FromBase64String(credential.Salt).Length != 16 ||
                Convert.FromBase64String(credential.Hash).Length != 32) throw new FormatException();
        }
        catch (FormatException) { throw new InvalidDataException("Invalid challenge credential encoding."); }
    }

    public static string NormalizePrompt(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var normalized = CollapseWhitespace(value.Normalize(NormalizationForm.FormC));
        if (normalized.Length is < MinimumPromptLength or > MaximumPromptLength || normalized.Any(char.IsControl))
            throw new ArgumentException($"Challenge prompts must contain {MinimumPromptLength}..{MaximumPromptLength} printable characters.");
        return normalized;
    }

    public static string NormalizeResponse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var normalized = NormalizeComparable(value);
        if (normalized.Length is < MinimumResponseLength or > MaximumResponseLength || normalized.Any(char.IsControl))
            throw new ArgumentException($"Challenge responses must contain {MinimumResponseLength}..{MaximumResponseLength} printable characters.");
        return normalized;
    }

    private static string NormalizeComparable(string value) =>
        CollapseWhitespace(value.Normalize(NormalizationForm.FormKC)).ToUpperInvariant();

    private static string CollapseWhitespace(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
