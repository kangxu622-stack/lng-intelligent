namespace backend.Services;

public sealed class OllamaStreamChunk
{
    public string Model { get; set; } = string.Empty;

    public string ContentDelta { get; set; } = string.Empty;

    public bool Done { get; set; }

    public int? PromptTokens { get; set; }

    public int? CompletionTokens { get; set; }

    public int? TotalTokens { get; set; }
}
