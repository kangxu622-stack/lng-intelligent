namespace backend.Llm.Training.Dtos;

public sealed class AnswerSubmitInput
{
    public string UserId { get; set; } = string.Empty;
    public string QuestionId { get; set; } = string.Empty;
    public string UserAnswer { get; set; } = string.Empty;
    public int Duration { get; set; }
}

public sealed class AnswerSubmitResult
{
    public string RecordId { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string? CorrectAnswer { get; set; }
    public string? Explanation { get; set; }
    public string? KnowledgeName { get; set; }
}

public sealed class TrainingQueryInput
{
    public string UserId { get; set; } = string.Empty;
    public string? KnowledgeId { get; set; }
    public string? Position { get; set; }
    public string? QuestionType { get; set; }
    public string? Difficulty { get; set; }
    public int Limit { get; set; } = 10;
}

public sealed class AnswerRecordDto
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
