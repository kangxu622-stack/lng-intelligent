using backend.Llm.Dtos;
using backend.Llm.Entities;
using backend.Llm.Repositories;
using System.Text;

namespace backend.Llm.Services;

public sealed class LlmAppService : ILlmAppService
{
    private static readonly HashSet<string> AllowedBizTypes =
    [
        "question",
        "fault_diagnosis",
        "scheme_explanation"
    ];

    private readonly ILlmRepository _llmRepository;
    private readonly IOllamaService _ollamaService;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LlmAppService> _logger;

    public LlmAppService(
        ILlmRepository llmRepository,
        IOllamaService ollamaService,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<LlmAppService> logger)
    {
        _llmRepository = llmRepository;
        _ollamaService = ollamaService;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task<CreateConversationResult> CreateConversationAsync(string bizType, CreateConversationInput input, CancellationToken cancellationToken)
    {
        bizType = NormalizeBizType(bizType);
        ValidateBizType(bizType);
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTime.Now;
        var conversation = new LlmConversation
        {
            ConversationCode = GenerateConversationCode(bizType),
            UserId = input.UserId,
            BizType = bizType,
            Title = string.IsNullOrWhiteSpace(input.Title) ? GetDefaultTitle(bizType) : input.Title.Trim(),
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

    public async Task<IReadOnlyList<LlmConversationDto>> GetConversationsAsync(int userId, string bizType, CancellationToken cancellationToken)
    {
        bizType = NormalizeBizType(bizType);
        ValidateBizType(bizType);

        var conversations = await _llmRepository.GetConversationsAsync(userId, bizType, cancellationToken);
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

    public async Task<IReadOnlyList<LlmMessageDto>> GetMessagesAsync(string bizType, string conversationCode, CancellationToken cancellationToken)
    {
        bizType = NormalizeBizType(bizType);
        ValidateBizType(bizType);

        var conversation = await GetConversationOrThrowAsync(conversationCode, bizType, cancellationToken);
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

    public async Task<SendLlmMessageResult> SendMessageAsync(string bizType, SendLlmMessageInput input, CancellationToken cancellationToken)
    {
        bizType = NormalizeBizType(bizType);
        ValidateBizType(bizType);
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTime.Now;
        var content = input.Content?.Trim() ?? string.Empty;
        var conversation = await ResolveConversationAsync(bizType, input, now, cancellationToken);
        var userSequence = await GetNextSequenceNoAsync(conversation, cancellationToken);
        var reply = await GenerateAssistantReplyAsync(bizType, conversation, content, input.AttachmentIds, cancellationToken);

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
            BizType = bizType,
            UserMessage = ToDto(userMessage),
            AssistantMessage = ToDto(assistantMessage)
        };
    }

    public async Task StreamMessageAsync(
        string bizType,
        SendLlmMessageInput input,
        Func<LlmStreamEvent, Task> onEvent,
        CancellationToken cancellationToken)
    {
        bizType = NormalizeBizType(bizType);
        ValidateBizType(bizType);
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTime.Now;
        var content = input.Content?.Trim() ?? string.Empty;
        var attachmentCount = input.AttachmentIds?.Count ?? 0;
        var conversation = await ResolveConversationAsync(bizType, input, now, cancellationToken);
        var userSequence = await GetNextSequenceNoAsync(conversation, cancellationToken);

        await onEvent(new LlmStreamEvent
        {
            Type = "start",
            ConversationCode = conversation.ConversationCode
        });

        var assistantContent = new StringBuilder();
        string modelName = _configuration["Ollama:Model"] ?? "qwen2.5:7b";
        int? promptTokens = null;
        int? completionTokens = null;
        int? totalTokens = null;
        var chunkCount = 0;

        try
        {
            await foreach (var chunk in GenerateAssistantReplyStreamAsync(
                               bizType,
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
                bizType,
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
                bizType,
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

    public async Task<UploadFaultImageResult> UploadFaultImageAsync(string? conversationCode, IFormFile file, int userId, CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
        {
            throw new InvalidOperationException("Image file is required.");
        }

        var now = DateTime.Now;
        var conversation = string.IsNullOrWhiteSpace(conversationCode)
            ? await CreatePendingConversationAsync("fault_diagnosis", userId, "图片故障分析", now, cancellationToken)
            : await GetConversationOrThrowAsync(conversationCode!, "fault_diagnosis", cancellationToken);

        var uploadsRoot = Path.Combine(_environment.ContentRootPath, "uploads", "llm", "fault-diagnosis");
        Directory.CreateDirectory(uploadsRoot);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{DateTime.Now:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = File.Create(fullPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var relativePath = Path.Combine("uploads", "llm", "fault-diagnosis", fileName).Replace("\\", "/");
        var attachment = new LlmAttachment
        {
            ConversationId = conversation.ConversationId,
            AttachmentType = "image",
            FileName = file.FileName,
            FileExt = ext,
            MimeType = file.ContentType,
            FileSize = file.Length,
            StoragePath = relativePath,
            PreviewUrl = relativePath
        };

        attachment.AttachmentId = await _llmRepository.InsertAttachmentAsync(attachment, cancellationToken);
        await _llmRepository.TouchConversationAsync(conversation.ConversationId, now, cancellationToken);

        return new UploadFaultImageResult
        {
            AttachmentId = attachment.AttachmentId,
            ConversationCode = conversation.ConversationCode,
            FileName = attachment.FileName,
            StoragePath = attachment.StoragePath,
            PreviewUrl = string.IsNullOrWhiteSpace(attachment.PreviewUrl) ? null : $"/{attachment.PreviewUrl!.TrimStart('/')}"
        };
    }

    public async Task DeleteConversationAsync(int userId, string bizType, string conversationCode, CancellationToken cancellationToken)
    {
        bizType = NormalizeBizType(bizType);
        ValidateBizType(bizType);

        var conversation = await GetConversationOrThrowAsync(conversationCode, bizType, cancellationToken);
        if (conversation.UserId != userId)
        {
            throw new InvalidOperationException("Conversation does not belong to the current user.");
        }

        await _llmRepository.DeleteConversationAsync(conversation.ConversationId, cancellationToken);
    }

    private async Task<LlmConversation> ResolveConversationAsync(string bizType, SendLlmMessageInput input, DateTime now, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(input.ConversationCode))
        {
            return await GetConversationOrThrowAsync(input.ConversationCode, bizType, cancellationToken);
        }

        return new LlmConversation
        {
            ConversationCode = GenerateConversationCode(bizType),
            UserId = input.UserId,
            BizType = bizType,
            Title = BuildConversationTitle(input.Content, bizType),
            LastMessageAt = now
        };
    }

    private async Task<LlmConversation> CreatePendingConversationAsync(string bizType, int userId, string title, DateTime now, CancellationToken cancellationToken)
    {
        var conversation = new LlmConversation
        {
            ConversationCode = GenerateConversationCode(bizType),
            UserId = userId,
            BizType = bizType,
            Title = title,
            LastMessageAt = now
        };

        conversation.ConversationId = await _llmRepository.InsertConversationAsync(conversation, cancellationToken);
        return conversation;
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

    private async Task<LlmConversation> GetConversationOrThrowAsync(string conversationCode, string bizType, CancellationToken cancellationToken)
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

    private static void ValidateBizType(string bizType)
    {
        if (!AllowedBizTypes.Contains(bizType))
        {
            throw new InvalidOperationException($"Unsupported bizType: {bizType}");
        }
    }

    private static string NormalizeBizType(string bizType)
    {
        return bizType.Trim().Replace('-', '_').ToLowerInvariant();
    }

    private static string GenerateConversationCode(string bizType)
    {
        var prefix = bizType switch
        {
            "question" => "Q",
            "fault_diagnosis" => "F",
            "scheme_explanation" => "S",
            _ => "L"
        };

        return $"{prefix}-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }

    private static string GetDefaultTitle(string bizType)
    {
        return bizType switch
        {
            "question" => "智能问答",
            "fault_diagnosis" => "故障诊断",
            "scheme_explanation" => "方案解读",
            _ => "LLM 会话"
        };
    }

    private static string BuildConversationTitle(string? content, string bizType)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            var trimmed = content.Trim();
            return trimmed.Length <= 20 ? trimmed : trimmed[..20];
        }

        return GetDefaultTitle(bizType);
    }

    private async Task<OllamaChatResult> GenerateAssistantReplyAsync(
        string bizType,
        LlmConversation conversation,
        string content,
        IReadOnlyCollection<long>? attachmentIds,
        CancellationToken cancellationToken)
    {
        var model = _configuration["Ollama:Model"] ?? "qwen2.5:7b";
        var systemPrompt = BuildSystemPrompt(bizType);
        var history = conversation.ConversationId > 0
            ? await _llmRepository.GetMessagesAsync(conversation.ConversationId, cancellationToken)
            : Array.Empty<LlmMessage>();

        var messages = BuildOllamaMessages(systemPrompt, history, content, bizType, attachmentIds);

        try
        {
            return await _ollamaService.ChatAsync(model, messages, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Ollama for bizType {BizType}", bizType);
            return new OllamaChatResult
            {
                Model = "fallback",
                Content = $"当前大模型服务暂不可用，请稍后重试。你的问题是：{content}",
                ResponseMs = 0
            };
        }
    }

    private async IAsyncEnumerable<OllamaStreamChunk> GenerateAssistantReplyStreamAsync(
        string bizType,
        LlmConversation conversation,
        string content,
        IReadOnlyCollection<long>? attachmentIds,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var model = _configuration["Ollama:Model"] ?? "qwen2.5:7b";
        var systemPrompt = BuildSystemPrompt(bizType);
        var history = conversation.ConversationId > 0
            ? await _llmRepository.GetMessagesAsync(conversation.ConversationId, cancellationToken)
            : Array.Empty<LlmMessage>();

        var messages = BuildOllamaMessages(systemPrompt, history, content, bizType, attachmentIds);

        await foreach (var chunk in _ollamaService.StreamChatAsync(model, messages, cancellationToken))
        {
            yield return chunk;
        }
    }

    private static List<OllamaChatMessage> BuildOllamaMessages(
        string systemPrompt,
        IReadOnlyCollection<LlmMessage> history,
        string content,
        string bizType,
        IReadOnlyCollection<long>? attachmentIds)
    {
        var messages = new List<OllamaChatMessage>
        {
            new()
            {
                Role = "system",
                Content = systemPrompt
            }
        };

        messages.AddRange(history
            .Where(m => m.Role is "user" or "assistant")
            .Select(m => new OllamaChatMessage
            {
                Role = m.Role,
                Content = m.Content
            }));

        var userContent = content;
        if (bizType == "fault_diagnosis" && (attachmentIds?.Count ?? 0) > 0)
        {
            userContent += $"{Environment.NewLine}{Environment.NewLine}本次消息附带 {attachmentIds!.Count} 张故障图片，请结合图片场景进行分析。";
        }

        messages.Add(new OllamaChatMessage
        {
            Role = "user",
            Content = userContent
        });

        return messages;
    }

    private static string BuildSystemPrompt(string bizType)
    {
        return bizType switch
        {
            "question" => "你是 LNG 接收站业务助手。请结合 LNG 接收站、BOG、储罐、泵、能耗与调度等场景，给出准确、简洁、可执行的回答。",
            "fault_diagnosis" => "你是 LNG 接收站设备故障诊断助手。请基于设备现象、图片线索和上下文，给出可能原因、排查步骤和处理建议。",
            "scheme_explanation" => "你是 LNG 调度方案解读助手。请用清晰的语言解释方案含义、关键约束、执行步骤和风险点。",
            _ => "你是 LNG 业务智能助手。"
        };
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
