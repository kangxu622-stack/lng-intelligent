namespace backend.Llm.Training.Entities;

public sealed class TrainingQuestion
{
    public string QuestionId { get; set; } = string.Empty;
    public string? QuestionType { get; set; }
    public string Stem { get; set; } = string.Empty;
    public string? OptionsJson { get; set; }
    public string? Answer { get; set; }
    public string? Explanation { get; set; }
    public string? KnowledgeId { get; set; }
    public string? KnowledgeName { get; set; }
    public string? Position { get; set; }
    public string? Difficulty { get; set; }
    public string? Source { get; set; }
    public string? ManualBasis { get; set; }
    public string ReviewStatus { get; set; } = "pending";
    public DateTime? CreatedTime { get; set; }
    public DateTime? UpdatedTime { get; set; }
}
