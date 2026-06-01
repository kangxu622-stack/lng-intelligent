namespace backend.Llm.Training.Entities;

public sealed class TrainingWrongQuestion
{
    public string WrongId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? QuestionId { get; set; }
    public string? KnowledgeId { get; set; }
    public string? WrongAnswer { get; set; }
    public string? CorrectAnswer { get; set; }
    public DateTime? CreatedTime { get; set; }
    public int ReviewCount { get; set; }
    public string? Stem { get; set; }
    public string? QuestionType { get; set; }
    public string? KnowledgeName { get; set; }
    public string? QAnswer { get; set; }
    public string? Explanation { get; set; }
    public string? ManualBasis { get; set; }
}
