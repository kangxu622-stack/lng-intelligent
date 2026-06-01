namespace backend.Llm.Dtos;

public sealed class CreateConversationInput
{
    public int UserId { get; set; }

    public string? Title { get; set; }
}
