namespace backend.Llm.Services;

public sealed class LlmStreamEvent
{
    public string Type { get; set; } = string.Empty;

    public string? ConversationCode { get; set; }

    public long? MessageId { get; set; }

    public string? Content { get; set; }

    public string? ModelName { get; set; }

    public int? PromptTokens { get; set; }

    public int? CompletionTokens { get; set; }

    public int? TotalTokens { get; set; }
}
