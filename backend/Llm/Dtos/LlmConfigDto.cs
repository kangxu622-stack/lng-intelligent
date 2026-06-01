namespace backend.Llm.Dtos;

public sealed class LlmConfigDto
{
    public string ActiveProvider { get; set; } = string.Empty;
    public string ActiveModel { get; set; } = string.Empty;
    public bool HasApiKey { get; set; }
    public string OpenAiBaseUrl { get; set; } = string.Empty;
    public string OpenAiModel { get; set; } = string.Empty;
}

public sealed class UpdateLlmConfigInput
{
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
    public string? Model { get; set; }
}
