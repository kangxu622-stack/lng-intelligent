using backend.Llm.Dtos;
using backend.Llm.Entities;
using backend.Llm.Handlers;
using backend.Llm.Repositories;

namespace backend.Llm.Services;

public sealed class LlmAppService : ILlmAppService
{
    private readonly LlmConversationEngine _engine;
    private readonly Dictionary<string, ILlmBizTypeHandler> _handlers;
    private readonly ILlmRepository _llmRepository;
    private readonly IWebHostEnvironment _environment;

    public LlmAppService(
        LlmConversationEngine engine,
        IEnumerable<ILlmBizTypeHandler> handlers,
        ILlmRepository llmRepository,
        IWebHostEnvironment environment)
    {
        _engine = engine;
        _handlers = handlers.ToDictionary(h => h.BizType, StringComparer.OrdinalIgnoreCase);
        _llmRepository = llmRepository;
        _environment = environment;
    }

    private ILlmBizTypeHandler ResolveHandler(string bizType)
    {
        var key = bizType.Trim().Replace('-', '_').ToLowerInvariant();
        if (!_handlers.TryGetValue(key, out var handler))
        {
            throw new InvalidOperationException($"Unsupported bizType: {bizType}");
        }

        return handler;
    }

    public Task<CreateConversationResult> CreateConversationAsync(string bizType, CreateConversationInput input, CancellationToken cancellationToken)
        => _engine.CreateConversationAsync(ResolveHandler(bizType), input, cancellationToken);

    public Task<IReadOnlyList<LlmConversationDto>> GetConversationsAsync(int userId, string bizType, CancellationToken cancellationToken)
        => _engine.GetConversationsAsync(ResolveHandler(bizType), userId, cancellationToken);

    public Task<IReadOnlyList<LlmMessageDto>> GetMessagesAsync(string bizType, string conversationCode, CancellationToken cancellationToken)
        => _engine.GetMessagesAsync(ResolveHandler(bizType), conversationCode, cancellationToken);

    public Task<SendLlmMessageResult> SendMessageAsync(string bizType, SendLlmMessageInput input, CancellationToken cancellationToken)
        => _engine.SendMessageAsync(ResolveHandler(bizType), input, cancellationToken);

    public Task StreamMessageAsync(
        string bizType,
        SendLlmMessageInput input,
        Func<LlmStreamEvent, Task> onEvent,
        CancellationToken cancellationToken)
        => _engine.StreamMessageAsync(ResolveHandler(bizType), input, onEvent, cancellationToken);

    public Task DeleteConversationAsync(int userId, string bizType, string conversationCode, CancellationToken cancellationToken)
        => _engine.DeleteConversationAsync(ResolveHandler(bizType), userId, conversationCode, cancellationToken);

    public async Task<UploadFaultImageResult> UploadFaultImageAsync(string? conversationCode, IFormFile file, int userId, CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
        {
            throw new InvalidOperationException("Image file is required.");
        }

        var handler = ResolveHandler("fault_diagnosis");
        var now = DateTime.Now;
        var conversation = string.IsNullOrWhiteSpace(conversationCode)
            ? await CreatePendingConversationAsync(handler, userId, "图片故障分析", now, cancellationToken)
            : await _engine.GetConversationOrThrowAsync(conversationCode!, handler.BizType, cancellationToken);

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

    private async Task<LlmConversation> CreatePendingConversationAsync(
        ILlmBizTypeHandler handler, int userId, string title, DateTime now, CancellationToken cancellationToken)
    {
        var conversation = new LlmConversation
        {
            ConversationCode = _engine.GenerateConversationCode(handler),
            UserId = userId,
            BizType = handler.BizType,
            Title = title,
            LastMessageAt = now
        };

        conversation.ConversationId = await _llmRepository.InsertConversationAsync(conversation, cancellationToken);
        return conversation;
    }
}
