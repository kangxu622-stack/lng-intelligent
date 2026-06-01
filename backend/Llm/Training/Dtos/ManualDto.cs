namespace backend.Llm.Training.Dtos;

public sealed class ManualDto
{
    public string ManualId { get; set; } = string.Empty;
    public string ManualName { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public string? UploadUser { get; set; }
    public DateTime? UploadTime { get; set; }
    public string Status { get; set; } = "uploaded";
    public int ChunkCount { get; set; }
}
