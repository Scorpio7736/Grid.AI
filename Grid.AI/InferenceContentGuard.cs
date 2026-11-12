using System.Text.RegularExpressions;

namespace Grid.AI;

/// <summary>Bounds content and rejects recognizable credential material at inference boundaries.</summary>
public static class InferenceContentGuard
{
    private static readonly Regex Credentials = new(
        "-----BEGIN (?:RSA |EC |OPENSSH |DSA |ENCRYPTED )?PRIVATE KEY-----|" +
        "\\bBearer\\s+[A-Za-z0-9._~+/-]{12,}=*|" +
        "\\b(?:api[_-]?key|access[_-]?token|client[_-]?secret|password)\\s*[\\\"']?\\s*[:=]\\s*[\\\"']?[^\\s\\\"',;}]{8,}",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    public static bool ContainsCredentials(string text) => Credentials.IsMatch(text.Replace("\\\"", "\""));

    public static void ValidateOutput(string text, int maximumCharacters)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > maximumCharacters ||
            text.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t')))
            throw new InvalidDataException("Invalid model output content or size.");
        if (ContainsCredentials(text))
        {
            var error = new InvalidDataException("Model output contains recognizable credential material.");
            error.Data["GRID_FAILURE"] = "credentials";
            throw error;
        }
    }
}
