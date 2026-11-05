namespace Grid.AI;

public enum ModelOutputKind { Text, Image }
public sealed record ModelCapabilities(bool Text, bool Images, bool Streaming, bool StructuredOutput);
public sealed record LocalModel(string Id, string? FilePath = null, ModelOutputKind Kind = ModelOutputKind.Text);
public sealed record ModelRequest(string Prompt, int ContextTokens = 4096, int MaxOutputTokens = 512,
    bool Json = false, string? JsonSchema = null, bool CpuOnly = false,
    ModelOutputKind Kind = ModelOutputKind.Text, int Width = 512, int Height = 512)
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(120);
    public string? SystemInstruction { get; init; }
    public string? PromptVersion { get; init; }
    public string PromptTemplate { get; init; } = "raw";
    public void Validate(ModelCapabilities capabilities)
    {
        if (string.IsNullOrWhiteSpace(Prompt) || Prompt.Length > 65536 || ContextTokens is < 256 or > 131072 ||
            MaxOutputTokens is < 1 or > 8192 || Timeout <= TimeSpan.Zero || Timeout > TimeSpan.FromMinutes(5))
            throw new ArgumentException("Invalid model request or resource budget.");
        _ = ModelPrompt.Render(this);
        if (Kind == ModelOutputKind.Image && (!capabilities.Images || Width is < 64 or > 4096 || Height is < 64 or > 4096))
            throw new NotSupportedException("This provider cannot satisfy the requested image capability/dimensions.");
        if (Kind == ModelOutputKind.Text && !capabilities.Text) throw new NotSupportedException("Text generation is unavailable.");
        if ((Json || JsonSchema is not null) && !capabilities.StructuredOutput) throw new NotSupportedException("Structured output is unavailable.");
        if (!Enum.IsDefined(Kind)) throw new ArgumentException("Unknown output kind.");
        if (JsonSchema is not null)
        {
            if (JsonSchema.Length > 65536) throw new ArgumentException("Schema exceeds the request budget.");
            using var schema = System.Text.Json.JsonDocument.Parse(JsonSchema);
            if (schema.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object) throw new ArgumentException("Schema must be an object.");
        }
    }
}
public sealed record ModelResult(string Text, bool Complete, string FinishReason, byte[]? Image = null, string? ImageMediaType = null)
{
    public long? OutputTokens { get; init; }
    public long? GenerationNanoseconds { get; init; }
}
public sealed record ModelChunk(string Text, bool Finished, string FinishReason = "");
public sealed record ModelResources(bool Loaded, long? AllocatedBytes, long? GpuBytes, string Evidence);

/// <summary>Local intelligence only. Caller authorization and agent prompts belong to the host.</summary>
public interface ILocalModelProvider : IAsyncDisposable
{
    string Name { get; }
    ModelCapabilities Capabilities { get; }
    Task LoadAsync(LocalModel model, CancellationToken token);
    Task UnloadAsync(LocalModel model, CancellationToken token);
    Task<ModelResources> ResourcesAsync(LocalModel model, CancellationToken token);
    Task<ModelResult> GenerateAsync(LocalModel model, ModelRequest request, CancellationToken token);
    IAsyncEnumerable<ModelChunk> StreamAsync(LocalModel model, ModelRequest request, CancellationToken token);
}
