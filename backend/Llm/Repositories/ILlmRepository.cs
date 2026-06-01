using backend.Llm.Entities;

namespace backend.Llm.Repositories;

public interface ILlmRepository
{
    Task<long> InsertConversationAsync(LlmConversation conversation, CancellationToken cancellationToken);

    Task<LlmConversation?> GetConversationByCodeAsync(string conversationCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<LlmConversation>> GetConversationsAsync(int userId, string bizType, CancellationToken cancellationToken);

    Task<IReadOnlyList<LlmMessage>> GetMessagesAsync(long conversationId, CancellationToken cancellationToken);

    Task<int> GetNextMessageSequenceNoAsync(long conversationId, CancellationToken cancellationToken);

    Task<long> InsertMessageAsync(LlmMessage message, CancellationToken cancellationToken);

    Task<long> InsertAttachmentAsync(LlmAttachment attachment, CancellationToken cancellationToken);

    Task TouchConversationAsync(long conversationId, DateTime lastMessageAt, CancellationToken cancellationToken);

    Task DeleteConversationAsync(long conversationId, CancellationToken cancellationToken);
}
