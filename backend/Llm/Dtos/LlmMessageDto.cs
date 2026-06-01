namespace backend.Llm.Dtos;

public sealed class LlmMessageDto
{
    public long MessageId { get; set; }

    public string Role { get; set; } = string.Empty;

    public string ContentType { get; set; } = "text";

    public string Content { get; set; } = string.Empty;

    public string? ModelName { get; set; }

    public int SequenceNo { get; set; }

    public DateTime CreatedAt { get; set; }
}
