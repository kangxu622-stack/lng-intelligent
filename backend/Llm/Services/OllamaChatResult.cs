namespace backend.Llm.Services;

public sealed class OllamaChatResult
{
    public string Model { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public int? PromptTokens { get; set; }

    public int? CompletionTokens { get; set; }

    public int? TotalTokens { get; set; }

    public int ResponseMs { get; set; }
}
