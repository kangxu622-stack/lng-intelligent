namespace backend.Llm.Training.Dtos;

public sealed class KnowledgePointDto
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
    public int QuestionCount { get; set; }
    public int WrongCount { get; set; }
}
