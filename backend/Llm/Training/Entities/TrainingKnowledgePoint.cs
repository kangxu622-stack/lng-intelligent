namespace backend.Llm.Training.Entities;

public sealed class TrainingKnowledgePoint
{
    public string KnowledgeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? SystemName { get; set; }
    public string? ChapterTitle { get; set; }
    public string? Position { get; set; }
    public string? Difficulty { get; set; }
    public string? RiskLevel { get; set; }
    public string? SourceChunkId { get; set; }
    public string? ManualBasis { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime? CreatedTime { get; set; }
}
