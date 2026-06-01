namespace backend.Llm.Dtos;

public sealed class UploadFaultImageResult
{
    public long AttachmentId { get; set; }

    public string ConversationCode { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string StoragePath { get; set; } = string.Empty;

    public string? PreviewUrl { get; set; }
}
