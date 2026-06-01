using backend.Llm.Dtos;
using backend.Llm.Entities;
using backend.Llm.Handlers;
using backend.Llm.Repositories;
using System.Text;

namespace backend.Llm.Services;

public sealed class LlmConversationEngine
{
    private readonly ILlmRepository _llmRepository;
    private readonly LlmProviderSelector _selector;
    private readonly ILogger<LlmConversationEngine> _logger;

    public LlmConversationEngine(
        ILlmRepository llmRepository,
        LlmProviderSelector selector,
        ILogger<LlmConversationEngine> logger)
    {
        _llmRepository = llmRepository;
        _selector = selector;
        _logger = logger;
    }

    public async Task<CreateConversationResult> CreateConversationAsync(
        ILlmBizTypeHandler handler, CreateConversationInput input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTime.Now;
        var conversation = new LlmConversation
        {
            ConversationCode = GenerateConversationCode(handler),
            UserId = input.UserId,
            BizType = handler.BizType,
            Title = string.IsNullOrWhiteSpace(input.Title) ? handler.DefaultTitle : input.Title.Trim(),
            LastMessageAt = now
        };

        conversation.ConversationId = await _llmRepository.InsertConversationAsync(conversation, cancellationToken);

        return new CreateConversationResult
        {
            ConversationId = conversation.ConversationId,
            ConversationCode = conversation.ConversationCode,
            BizType = conversation.BizType,
            Title = conversation.Title,
            CreatedAt = now
        };
    }

    public async Task<IReadOnlyList<LlmConversationDto>> GetConversationsAsync(
        ILlmBizTypeHandler handler, int userId, CancellationToken cancellationToken)
    {
        var conversations = await _llmRepository.GetConversationsAsync(userId, handler.BizType, cancellationToken);
        return conversations
            .Select(c => new LlmConversationDto
            {
                ConversationId = c.ConversationId,
                ConversationCode = c.ConversationCode,
                BizType = c.BizType,
                Title = c.Title,
                Status = c.Status,
                LastMessageAt = c.LastMessageAt,
                CreatedAt = c.CreatedAt
            })
            .ToList();
    }

    public async Task<IReadOnlyList<LlmMessageDto>> GetMessagesAsync(
        ILlmBizTypeHandler handler, string conversationCode, CancellationToken cancellationToken)
    {
        var conversation = await GetConversationOrThrowAsync(conversationCode, handler.BizType, cancellationToken);
        var messages = await _llmRepository.GetMessagesAsync(conversation.ConversationId, cancellationToken);

        return messages
            .Select(m => new LlmMessageDto
            {
                MessageId = m.MessageId,
                Role = m.Role,
                ContentType = m.ContentType,
                Content = m.Content,
                ModelName = m.ModelName,
                SequenceNo = m.SequenceNo,
                CreatedAt = m.CreatedAt
            })
            .ToList();
    }

    public async Task<SendLlmMessageResult> SendMessageAsync(
        ILlmBizTypeHandler handler, SendLlmMessageInput input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTime.Now;
        var content = input.Content?.Trim() ?? string.Empty;
        var conversation = await ResolveConversationAsync(handler, input, now, cancellationToken);
        var userSequence = await GetNextSequenceNoAsync(conversation, cancellationToken);
        var reply = await GenerateAssistantReplyAsync(handler, conversation, content, input.AttachmentIds, cancellationToken);

        await PersistConversationIfNeededAsync(conversation, cancellationToken);

        var userMessage = new LlmMessage
        {
            ConversationId = conversation.ConversationId,
            Role = "user",
            ContentType = (input.AttachmentIds?.Count ?? 0) > 0 ? "mixed" : "text",
            Content = content,
            SequenceNo = userSequence,
            CreatedAt = now
        };
        userMessage.MessageId = await _llmRepository.InsertMessageAsync(userMessage, cancellationToken);

        var assistantMessage = new LlmMessage
        {
            ConversationId = conversation.ConversationId,
            Role = "assistant",
            ContentType = "text",
            Content = reply.Content,
            ModelName = reply.Model,
            PromptTokens = reply.PromptTokens,
            CompletionTokens = reply.CompletionTokens,
            TotalTokens = reply.TotalTokens,
            ResponseMs = reply.ResponseMs,
            SequenceNo = userSequence + 1,
            ParentMessageId = userMessage.MessageId,
            CreatedAt = now
        };
        assistantMessage.MessageId = await _llmRepository.InsertMessageAsync(assistantMessage, cancellationToken);
        await _llmRepository.TouchConversationAsync(conversation.ConversationId, now, cancellationToken);

        return new SendLlmMessageResult
        {
            ConversationCode = conversation.ConversationCode,
            BizType = handler.BizType,
            UserMessage = ToDto(userMessage),
            AssistantMessage = ToDto(assistantMessage)
        };
    }

    public async Task StreamMessageAsync(
        ILlmBizTypeHandler handler,
        SendLlmMessageInput input,
        Func<LlmStreamEvent, Task> onEvent,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTime.Now;
        var content = input.Content?.Trim() ?? string.Empty;
        var attachmentCount = input.AttachmentIds?.Count ?? 0;
        var conversation = await ResolveConversationAsync(handler, input, now, cancellationToken);
        var userSequence = await GetNextSequenceNoAsync(conversation, cancellationToken);

        await onEvent(new LlmStreamEvent
        {
            Type = "start",
            ConversationCode = conversation.ConversationCode
        });

        var assistantContent = new StringBuilder();
        string modelName = _selector.GetActiveModel();
        int? promptTokens = null;
        int? completionTokens = null;
        int? totalTokens = null;
        var chunkCount = 0;

        try
        {
            await foreach (var chunk in GenerateAssistantReplyStreamAsync(
                               handler,
                               conversation,
                               content,
                               input.AttachmentIds,
                               cancellationToken))
            {
                modelName = chunk.Model;
                promptTokens = chunk.PromptTokens ?? promptTokens;
                completionTokens = chunk.CompletionTokens ?? completionTokens;
                totalTokens = chunk.TotalTokens ?? totalTokens;

                if (string.IsNullOrEmpty(chunk.ContentDelta))
                {
                    continue;
                }

                chunkCount++;
                assistantContent.Append(chunk.ContentDelta);
                await onEvent(new LlmStreamEvent
                {
                    Type = "delta",
                    ConversationCode = conversation.ConversationCode,
                    Content = chunk.ContentDelta,
                    ModelName = chunk.Model
                });
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "LLM stream cancelled by client. BizType={BizType}, UserId={UserId}, ConversationCode={ConversationCode}, ConversationId={ConversationId}, ChunkCount={ChunkCount}, ResponseLength={ResponseLength}",
                handler.BizType,
                input.UserId,
                conversation.ConversationCode,
                conversation.ConversationId,
                chunkCount,
                assistantContent.Length);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to stream Ollama response. BizType={BizType}, UserId={UserId}, ConversationCode={ConversationCode}, ConversationId={ConversationId}, AttachmentCount={AttachmentCount}, ChunkCount={ChunkCount}",
                handler.BizType,
                input.UserId,
                conversation.ConversationCode,
                conversation.ConversationId,
                attachmentCount,
                chunkCount);

            assistantContent.Clear();
            assistantContent.Append($"当前大模型服务暂不可用，请稍后重试。你的问题是：{content}");
            modelName = "fallback";

            await onEvent(new LlmStreamEvent
            {
                Type = "delta",
                ConversationCode = conversation.ConversationCode,
                Content = assistantContent.ToString(),
                ModelName = modelName
            });
        }

        await PersistConversationIfNeededAsync(conversation, cancellationToken);

        var userMessage = new LlmMessage
        {
            ConversationId = conversation.ConversationId,
            Role = "user",
            ContentType = attachmentCount > 0 ? "mixed" : "text",
            Content = content,
            SequenceNo = userSequence,
            CreatedAt = now
        };
        userMessage.MessageId = await _llmRepository.InsertMessageAsync(userMessage, cancellationToken);

        var assistantMessage = new LlmMessage
        {
            ConversationId = conversation.ConversationId,
            Role = "assistant",
            ContentType = "text",
            Content = assistantContent.ToString(),
            ModelName = modelName,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            TotalTokens = totalTokens,
            ResponseMs = null,
            SequenceNo = userSequence + 1,
            ParentMessageId = userMessage.MessageId,
            CreatedAt = DateTime.Now
        };
        assistantMessage.MessageId = await _llmRepository.InsertMessageAsync(assistantMessage, cancellationToken);
        await _llmRepository.TouchConversationAsync(conversation.ConversationId, DateTime.Now, cancellationToken);

        await onEvent(new LlmStreamEvent
        {
            Type = "done",
            ConversationCode = conversation.ConversationCode,
            MessageId = assistantMessage.MessageId,
            ModelName = assistantMessage.ModelName,
            PromptTokens = assistantMessage.PromptTokens,
            CompletionTokens = assistantMessage.CompletionTokens,
            TotalTokens = assistantMessage.TotalTokens
        });
    }

    public async Task DeleteConversationAsync(
        ILlmBizTypeHandler handler, int userId, string conversationCode, CancellationToken cancellationToken)
    {
        var conversation = await GetConversationOrThrowAsync(conversationCode, handler.BizType, cancellationToken);
        if (conversation.UserId != userId)
        {
            throw new InvalidOperationException("Conversation does not belong to the current user.");
        }

        await _llmRepository.DeleteConversationAsync(conversation.ConversationId, cancellationToken);
    }

    public string GenerateConversationCode(ILlmBizTypeHandler handler)
    {
        return $"{handler.ConversationCodePrefix}-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }

    public async Task<LlmConversation> GetConversationOrThrowAsync(string conversationCode, string bizType, CancellationToken cancellationToken)
    {
        var conversation = await _llmRepository.GetConversationByCodeAsync(conversationCode, cancellationToken);
        if (conversation == null)
        {
            throw new InvalidOperationException("Conversation does not exist.");
        }

        if (!string.Equals(conversation.BizType, bizType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Conversation business type does not match.");
        }

        return conversation;
    }

    private async Task<LlmConversation> ResolveConversationAsync(
        ILlmBizTypeHandler handler, SendLlmMessageInput input, DateTime now, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(input.ConversationCode))
        {
            return await GetConversationOrThrowAsync(input.ConversationCode, handler.BizType, cancellationToken);
        }

        return new LlmConversation
        {
            ConversationCode = GenerateConversationCode(handler),
            UserId = input.UserId,
            BizType = handler.BizType,
            Title = BuildConversationTitle(input.Content, handler),
            LastMessageAt = now
        };
    }

    private async Task PersistConversationIfNeededAsync(LlmConversation conversation, CancellationToken cancellationToken)
    {
        if (conversation.ConversationId > 0)
        {
            return;
        }

        conversation.ConversationId = await _llmRepository.InsertConversationAsync(conversation, cancellationToken);
    }

    private async Task<int> GetNextSequenceNoAsync(LlmConversation conversation, CancellationToken cancellationToken)
    {
        if (conversation.ConversationId <= 0)
        {
            return 1;
        }

        return await _llmRepository.GetNextMessageSequenceNoAsync(conversation.ConversationId, cancellationToken);
    }

    private static string BuildConversationTitle(string? content, ILlmBizTypeHandler handler)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            var trimmed = content.Trim();
            return trimmed.Length <= 20 ? trimmed : trimmed[..20];
        }

        return handler.DefaultTitle;
    }

    private async Task<OllamaChatResult> GenerateAssistantReplyAsync(
        ILlmBizTypeHandler handler,
        LlmConversation conversation,
        string content,
        IReadOnlyCollection<long>? attachmentIds,
        CancellationToken cancellationToken)
    {
        var provider = await _selector.SelectProviderAsync(cancellationToken);
        var model = _selector.GetActiveModel();
        var history = conversation.ConversationId > 0
            ? await _llmRepository.GetMessagesAsync(conversation.ConversationId, cancellationToken)
            : Array.Empty<LlmMessage>();

        var messages = BuildOllamaMessages(handler, history, content, attachmentIds);

        try
        {
            return await provider.ChatAsync(model, messages, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call LLM provider={Provider} for bizType={BizType}", _selector.GetActiveProvider(), handler.BizType);
            return new OllamaChatResult
            {
                Model = "fallback",
                Content = $"当前大模型服务暂不可用，请稍后重试。你的问题是：{content}",
                ResponseMs = 0
            };
        }
    }

    private async IAsyncEnumerable<OllamaStreamChunk> GenerateAssistantReplyStreamAsync(
        ILlmBizTypeHandler handler,
        LlmConversation conversation,
        string content,
        IReadOnlyCollection<long>? attachmentIds,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var provider = await _selector.SelectProviderAsync(cancellationToken);
        var model = _selector.GetActiveModel();
        var history = conversation.ConversationId > 0
            ? await _llmRepository.GetMessagesAsync(conversation.ConversationId, cancellationToken)
            : Array.Empty<LlmMessage>();

        var messages = BuildOllamaMessages(handler, history, content, attachmentIds);

        await foreach (var chunk in provider.StreamChatAsync(model, messages, cancellationToken))
        {
            yield return chunk;
        }
    }

    private static List<OllamaChatMessage> BuildOllamaMessages(
        ILlmBizTypeHandler handler,
        IReadOnlyCollection<LlmMessage> history,
        string content,
        IReadOnlyCollection<long>? attachmentIds)
    {
        var messages = new List<OllamaChatMessage>
        {
            new()
            {
                Role = "system",
                Content = handler.SystemPrompt
            }
        };

        messages.AddRange(history
            .Where(m => m.Role is "user" or "assistant")
            .Select(m => new OllamaChatMessage
            {
                Role = m.Role,
                Content = m.Content
            }));

        var userContent = handler.BuildUserContent(content, attachmentIds);

        messages.Add(new OllamaChatMessage
        {
            Role = "user",
            Content = userContent
        });

        return messages;
    }

    private static LlmMessageDto ToDto(LlmMessage message)
    {
        return new LlmMessageDto
        {
            MessageId = message.MessageId,
            Role = message.Role,
            ContentType = message.ContentType,
            Content = message.Content,
            ModelName = message.ModelName,
            SequenceNo = message.SequenceNo,
            CreatedAt = message.CreatedAt == default ? DateTime.Now : message.CreatedAt
        };
    }
}
