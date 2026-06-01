using backend.Llm.Dtos;

namespace backend.Llm.Services;

public interface ILlmAppService
{
    Task<CreateConversationResult> CreateConversationAsync(string bizType, CreateConversationInput input, CancellationToken cancellationToken);

    Task<IReadOnlyList<LlmConversationDto>> GetConversationsAsync(int userId, string bizType, CancellationToken cancellationToken);

    Task<IReadOnlyList<LlmMessageDto>> GetMessagesAsync(string bizType, string conversationCode, CancellationToken cancellationToken);

    Task<SendLlmMessageResult> SendMessageAsync(string bizType, SendLlmMessageInput input, CancellationToken cancellationToken);

    Task StreamMessageAsync(
        string bizType,
        SendLlmMessageInput input,
        Func<LlmStreamEvent, Task> onEvent,
        CancellationToken cancellationToken);

    Task<UploadFaultImageResult> UploadFaultImageAsync(string? conversationCode, IFormFile file, int userId, CancellationToken cancellationToken);

    Task DeleteConversationAsync(int userId, string bizType, string conversationCode, CancellationToken cancellationToken);
}
