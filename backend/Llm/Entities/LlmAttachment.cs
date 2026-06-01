namespace backend.Llm.Entities;

public sealed class LlmAttachment
{
    public long AttachmentId { get; set; }

    public long ConversationId { get; set; }

    public long? MessageId { get; set; }

    public string AttachmentType { get; set; } = "image";

    public string FileName { get; set; } = string.Empty;

    public string? FileExt { get; set; }

    public string? MimeType { get; set; }

    public long? FileSize { get; set; }

    public string StoragePath { get; set; } = string.Empty;

    public string? PreviewUrl { get; set; }

    public string? OcrText { get; set; }

    public DateTime CreatedAt { get; set; }
}
