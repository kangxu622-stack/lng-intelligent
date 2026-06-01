namespace backend.Dtos.Llm;

public sealed class SendLlmMessageResult
{
    public string ConversationCode { get; set; } = string.Empty;

    public string BizType { get; set; } = string.Empty;

    public LlmMessageDto UserMessage { get; set; } = new();

    public LlmMessageDto AssistantMessage { get; set; } = new();
}
