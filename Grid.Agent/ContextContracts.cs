namespace Grid.Communication;

/// <summary>Immutable captured content, not permission to reopen a host path.</summary>
public sealed record ContextDocument(Guid Id, string Project, string SourcePath, string SourceHash, long SourceBytes,
    DateTimeOffset SourceModifiedUtc, DateTimeOffset CapturedUtc, string Format, string Text, Guid? GrantedAgentId);
public sealed record ContextState(IReadOnlyList<ContextDocument> Documents)
{
    public const int MaxFiles = 64;
    public const int MaxSourceBytes = 1024 * 1024;
    public const int MaxTextCharacters = 256 * 1024;
    public const int MaxTotalCharacters = 1024 * 1024;
    public static ContextState Empty => new(Array.Empty<ContextDocument>());
    public static ContextState Copy(ContextState state) { Validate(state); return new(Array.AsReadOnly(state.Documents.ToArray())); }
    public static void Validate(ContextState state)
    {
        if (state?.Documents is null || state.Documents.Count > MaxFiles || state.Documents.Any(d => d is null) || state.Documents.Select(d => d.Id).Distinct().Count() != state.Documents.Count)
            throw new InvalidDataException("Invalid context collection.");
        long total = 0;
        foreach (var d in state.Documents)
        {
            if (d.Id == Guid.Empty || string.IsNullOrWhiteSpace(d.Project) || d.Project.Length > 80 || d.Project.Any(char.IsControl) ||
                string.IsNullOrWhiteSpace(d.SourcePath) || d.SourcePath.Length > 4096 || d.SourcePath.Any(char.IsControl) ||
                d.SourceHash is null || d.SourceHash.Length != 64 || d.SourceHash.Any(c => !Uri.IsHexDigit(c)) ||
                d.SourceBytes is < 0 or > MaxSourceBytes || d.SourceModifiedUtc.Offset != TimeSpan.Zero || d.CapturedUtc.Offset != TimeSpan.Zero ||
                d.Format is not ("utf8" or "docx-body" or "pdf-pages") || d.Text is null || d.Text.Length > MaxTextCharacters ||
                d.Text.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t')) || d.GrantedAgentId == Guid.Empty)
                throw new InvalidDataException("Invalid captured context document.");
            total += d.Text.Length;
        }
        if (total > MaxTotalCharacters) throw new InvalidDataException("Context text capacity exceeded; remove an import before adding more.");
    }
}
