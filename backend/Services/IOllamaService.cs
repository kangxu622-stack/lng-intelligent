namespace backend.Services;

public interface IOllamaService
{
    Task<OllamaChatResult> ChatAsync(
        string model,
        IReadOnlyList<OllamaChatMessage> messages,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<OllamaStreamChunk> StreamChatAsync(
        string model,
        IReadOnlyList<OllamaChatMessage> messages,
        CancellationToken cancellationToken = default);
}
