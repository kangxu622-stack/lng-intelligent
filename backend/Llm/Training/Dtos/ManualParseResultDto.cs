namespace backend.Llm.Training.Dtos;

public sealed class ManualParseResultDto
{
    public string ManualId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int Length { get; set; }
}
