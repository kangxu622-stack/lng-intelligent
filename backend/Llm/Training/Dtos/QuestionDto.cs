namespace backend.Llm.Training.Dtos;

public sealed class QuestionDto
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

public sealed class QuestionGenerateInput
{
    public string Position { get; set; } = "操作员";
    public string QuestionType { get; set; } = "单选题";
    public string Difficulty { get; set; } = "中级";
    public int Count { get; set; } = 5;
    public List<string>? ChunkIds { get; set; }
    public string? CustomText { get; set; }
    public string? KnowledgeId { get; set; }
    public string? KnowledgeName { get; set; }
    public string UserId { get; set; } = "admin";
}

public sealed class QuestionGenerateResult
{
    public List<string> SavedIds { get; set; } = new();
    public int Count { get; set; }
}

public sealed class QuestionUpdateInput
{
    public string? Stem { get; set; }
    public string? Answer { get; set; }
    public string? Explanation { get; set; }
    public string? OptionsJson { get; set; }
    public string? QuestionType { get; set; }
    public string? KnowledgeName { get; set; }
    public string? Position { get; set; }
    public string? Difficulty { get; set; }
    public string? ManualBasis { get; set; }
    public string? KnowledgeId { get; set; }
}
