namespace backend.Llm.Entities;

public sealed class LlmMessage
{
    public long MessageId { get; set; }

    public long ConversationId { get; set; }

    public string Role { get; set; } = string.Empty;

    public string ContentType { get; set; } = "text";

    public string Content { get; set; } = string.Empty;

    public string? ModelName { get; set; }

    public int? PromptTokens { get; set; }

    public int? CompletionTokens { get; set; }

    public int? TotalTokens { get; set; }

    public int? ResponseMs { get; set; }

    public int SequenceNo { get; set; }

    public long? ParentMessageId { get; set; }

    public DateTime CreatedAt { get; set; }
}
