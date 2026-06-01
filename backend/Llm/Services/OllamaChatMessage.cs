namespace backend.Llm.Services;

public sealed class OllamaChatMessage
{
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}
