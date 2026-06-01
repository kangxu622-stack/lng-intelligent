namespace backend.Llm.Handlers;

public sealed class SchemeExplanationHandler : ILlmBizTypeHandler
{
    public string BizType => "scheme_explanation";
    public string ConversationCodePrefix => "S";
    public string DefaultTitle => "方案解读";
    public string SystemPrompt => "你是 LNG 调度方案解读助手。请用清晰的语言解释方案含义、关键约束、执行步骤和风险点。";

    public string BuildUserContent(string content, IReadOnlyCollection<long>? attachmentIds) => content;
}
