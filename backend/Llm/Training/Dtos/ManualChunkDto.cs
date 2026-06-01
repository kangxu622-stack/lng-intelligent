namespace backend.Llm.Training.Dtos;

public sealed class ManualChunkDto
{
    public string ChunkId { get; set; } = string.Empty;
    public string ManualId { get; set; } = string.Empty;
    public string? ChapterTitle { get; set; }
    public string? SectionNo { get; set; }
    public string? Content { get; set; }
    public string? PageNo { get; set; }
    public string? SystemName { get; set; }
    public DateTime? CreatedTime { get; set; }
}
