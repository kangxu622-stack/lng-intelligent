namespace backend.Llm.Handlers;

public interface ILlmBizTypeHandler
{
    string BizType { get; }
    string ConversationCodePrefix { get; }
    string DefaultTitle { get; }
    string SystemPrompt { get; }
    string BuildUserContent(string content, IReadOnlyCollection<long>? attachmentIds);
}
