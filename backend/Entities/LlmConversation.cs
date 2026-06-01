namespace backend.Entities;

public sealed class LlmConversation
{
    public long ConversationId { get; set; }

    public string ConversationCode { get; set; } = string.Empty;

    public int UserId { get; set; }

    public string BizType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Summary { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? LastMessageAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
