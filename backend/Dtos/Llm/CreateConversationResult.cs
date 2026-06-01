namespace backend.Dtos.Llm;

public sealed class CreateConversationResult
{
    public long ConversationId { get; set; }

    public string ConversationCode { get; set; } = string.Empty;

    public string BizType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
