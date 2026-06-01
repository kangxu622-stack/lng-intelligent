namespace backend.Llm.Dtos;

public sealed class LlmConversationDto
{
    public long ConversationId { get; set; }

    public string ConversationCode { get; set; } = string.Empty;

    public string BizType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? LastMessageAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
