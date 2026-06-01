namespace backend.Llm.Training.Dtos;

public sealed class KnowledgeExtractInput
{
    public List<string> ChunkIds { get; set; } = new();
    public string UserId { get; set; } = "admin";
}

public sealed class KnowledgeExtractResult
{
    public List<string> SavedIds { get; set; } = new();
    public int Count { get; set; }
}

public sealed class KnowledgeUpdateInput
{
    public string? Name { get; set; }
    public string? SystemName { get; set; }
    public string? ChapterTitle { get; set; }
    public string? Position { get; set; }
    public string? Difficulty { get; set; }
    public string? RiskLevel { get; set; }
    public string? ManualBasis { get; set; }
}
