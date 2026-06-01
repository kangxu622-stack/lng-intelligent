namespace backend.Llm.Training.Dtos;

public sealed class UserStatsDto
{
    public int Total { get; set; }
    public int Correct { get; set; }
    public int Wrong { get; set; }
    public double Accuracy { get; set; }
}

public sealed class KnowledgeAccuracyDto
{
    public string? KnowledgeId { get; set; }
    public string KnowledgeName { get; set; } = "未分类";
    public int Total { get; set; }
    public int Correct { get; set; }
    public double Accuracy { get; set; }
    public string Level { get; set; } = string.Empty;
}

public sealed class AdminStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalAnswers { get; set; }
    public double AvgAccuracy { get; set; }
    public List<PerUserStatDto> PerUser { get; set; } = new();
    public List<KnowledgeStatDto> KnowledgeStats { get; set; } = new();
}

public sealed class PerUserStatDto
{
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Correct { get; set; }
}

public sealed class KnowledgeStatDto
{
    public string KnowledgeName { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Correct { get; set; }
}

public sealed class UserAnalyticsDto
{
    public UserStatsDto Stats { get; set; } = new();
    public List<KnowledgeAccuracyDto> KnowledgeAccuracy { get; set; } = new();
    public List<KnowledgeAccuracyDto> WeakKnowledge { get; set; } = new();
    public List<AnswerRecordDto> RecentRecords { get; set; } = new();
}
