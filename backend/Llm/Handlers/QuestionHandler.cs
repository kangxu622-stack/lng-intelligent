namespace backend.Llm.Handlers;

public sealed class QuestionHandler : ILlmBizTypeHandler
{
    public string BizType => "question";
    public string ConversationCodePrefix => "Q";
    public string DefaultTitle => "智能问答";
    public string SystemPrompt => "你是 LNG 接收站业务助手。请结合 LNG 接收站、BOG、储罐、泵、能耗与调度等场景，给出准确、简洁、可执行的回答。";

    public string BuildUserContent(string content, IReadOnlyCollection<long>? attachmentIds) => content;
}
