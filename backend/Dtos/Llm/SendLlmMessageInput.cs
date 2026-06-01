namespace backend.Dtos.Llm;

public sealed class SendLlmMessageInput
{
    public int UserId { get; set; }

    public string? ConversationCode { get; set; }

    public string? Content { get; set; }

    public List<long>? AttachmentIds { get; set; }
}
