namespace backend.Llm.Handlers;

public sealed class FaultDiagnosisHandler : ILlmBizTypeHandler
{
    public string BizType => "fault_diagnosis";
    public string ConversationCodePrefix => "F";
    public string DefaultTitle => "故障诊断";
    public string SystemPrompt => "你是 LNG 接收站设备故障诊断助手。请基于设备现象、图片线索和上下文，给出可能原因、排查步骤和处理建议。";

    public string BuildUserContent(string content, IReadOnlyCollection<long>? attachmentIds)
    {
        if (attachmentIds is { Count: > 0 })
        {
            return content
                + Environment.NewLine
                + Environment.NewLine
                + $"本次消息附带 {attachmentIds.Count} 张故障图片，请结合图片场景进行分析。";
        }

        return content;
    }
}
