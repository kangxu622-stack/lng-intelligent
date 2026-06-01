namespace backend.Llm.Services;

public interface ILlmProvider
{
    Task<OllamaChatResult> ChatAsync(
        string model,
        IReadOnlyList<OllamaChatMessage> messages,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<OllamaStreamChunk> StreamChatAsync(
        string model,
        IReadOnlyList<OllamaChatMessage> messages,
        CancellationToken cancellationToken = default);

    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
