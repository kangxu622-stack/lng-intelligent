namespace backend.Llm.Training.Entities;

public sealed class TrainingAnswerRecord
{
    public string RecordId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? QuestionId { get; set; }
    public string? UserAnswer { get; set; }
    public string? CorrectAnswer { get; set; }
    public int IsCorrect { get; set; }
    public decimal? Score { get; set; }
    public DateTime? AnswerTime { get; set; }
    public int? Duration { get; set; }
    public string? Stem { get; set; }
    public string? QuestionType { get; set; }
    public string? KnowledgeName { get; set; }
}
