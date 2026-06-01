namespace backend.Llm.Training.Entities;

public sealed class TrainingManual
{
    public string ManualId { get; set; } = string.Empty;
    public string ManualName { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public string? FilePath { get; set; }
    public string? UploadUser { get; set; }
    public DateTime? UploadTime { get; set; }
    public string Status { get; set; } = "uploaded";
}
