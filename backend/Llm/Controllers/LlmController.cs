using backend.Dtos;
using backend.Llm.Dtos;
using backend.Llm.Services;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Text.Json;

namespace backend.Llm.Controllers;

[ApiController]
[Route("api/llm")]
public sealed class LlmController : ControllerBase
{
    private static readonly JsonSerializerOptions SseJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILlmAppService _llmAppService;

    public LlmController(ILlmAppService llmAppService)
    {
        _llmAppService = llmAppService;
    }

    [HttpPost("{bizType}/conversations")]
    public async Task<IActionResult> CreateConversation(
        [FromRoute] string bizType,
        [FromBody] CreateConversationInput request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return Ok(ApiResponse<CreateConversationResult>.Error(400, "CreateConversation request body must be provided."));
        }

        if (request.UserId <= 0)
        {
            return Ok(ApiResponse<CreateConversationResult>.Error(400, "UserId is required."));
        }

        try
        {
            var result = await _llmAppService.CreateConversationAsync(bizType, request, cancellationToken);
            return Ok(ApiResponse<CreateConversationResult>.Success(result, "创建会话成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<CreateConversationResult>.Error(400, ex.Message));
        }
        catch (MySqlException ex)
        {
            return Ok(ApiResponse<CreateConversationResult>.Error(500, $"Database error: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<CreateConversationResult>.Error(500, ex.Message));
        }
    }

    [HttpGet("{bizType}/conversations")]
    public async Task<IActionResult> GetConversations(
        [FromRoute] string bizType,
        [FromQuery] int userId,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            return Ok(ApiResponse<IReadOnlyList<LlmConversationDto>>.Error(400, "UserId is required."));
        }

        try
        {
            var result = await _llmAppService.GetConversationsAsync(userId, bizType, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<LlmConversationDto>>.Success(result, "获取会话列表成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<IReadOnlyList<LlmConversationDto>>.Error(400, ex.Message));
        }
        catch (MySqlException ex)
        {
            return Ok(ApiResponse<IReadOnlyList<LlmConversationDto>>.Error(500, $"Database error: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<IReadOnlyList<LlmConversationDto>>.Error(500, ex.Message));
        }
    }

    [HttpGet("{bizType}/conversations/{conversationCode}/messages")]
    public async Task<IActionResult> GetMessages(
        [FromRoute] string bizType,
        [FromRoute] string conversationCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(conversationCode))
        {
            return Ok(ApiResponse<IReadOnlyList<LlmMessageDto>>.Error(400, "ConversationCode is required."));
        }

        try
        {
            var result = await _llmAppService.GetMessagesAsync(bizType, conversationCode, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<LlmMessageDto>>.Success(result, "获取消息成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<IReadOnlyList<LlmMessageDto>>.Error(400, ex.Message));
        }
        catch (MySqlException ex)
        {
            return Ok(ApiResponse<IReadOnlyList<LlmMessageDto>>.Error(500, $"Database error: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<IReadOnlyList<LlmMessageDto>>.Error(500, ex.Message));
        }
    }

    [HttpDelete("{bizType}/conversations/{conversationCode}")]
    public async Task<IActionResult> DeleteConversation(
        [FromRoute] string bizType,
        [FromRoute] string conversationCode,
        [FromQuery] int userId,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            return Ok(ApiResponse.Error(400, "UserId is required."));
        }

        if (string.IsNullOrWhiteSpace(conversationCode))
        {
            return Ok(ApiResponse.Error(400, "ConversationCode is required."));
        }

        try
        {
            await _llmAppService.DeleteConversationAsync(userId, bizType, conversationCode, cancellationToken);
            return Ok(ApiResponse.Success(message: "Conversation deleted."));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse.Error(400, ex.Message));
        }
        catch (MySqlException ex)
        {
            return Ok(ApiResponse.Error(500, $"Database error: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    [HttpPost("{bizType}/send")]
    public async Task<IActionResult> SendMessage(
        [FromRoute] string bizType,
        [FromBody] SendLlmMessageInput request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return Ok(ApiResponse<SendLlmMessageResult>.Error(400, "SendMessage request body must be provided."));
        }

        if (request.UserId <= 0)
        {
            return Ok(ApiResponse<SendLlmMessageResult>.Error(400, "UserId is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return Ok(ApiResponse<SendLlmMessageResult>.Error(400, "Content is required."));
        }

        try
        {
            var result = await _llmAppService.SendMessageAsync(bizType, request, cancellationToken);
            return Ok(ApiResponse<SendLlmMessageResult>.Success(result, "发送消息成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<SendLlmMessageResult>.Error(400, ex.Message));
        }
        catch (MySqlException ex)
        {
            return Ok(ApiResponse<SendLlmMessageResult>.Error(500, $"Database error: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<SendLlmMessageResult>.Error(500, ex.Message));
        }
    }

    [HttpPost("{bizType}/stream")]
    public async Task StreamMessage(
        [FromRoute] string bizType,
        [FromBody] SendLlmMessageInput request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteSseEventAsync("error", new { message = "SendMessage request body must be provided." }, cancellationToken);
            return;
        }

        if (request.UserId <= 0)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteSseEventAsync("error", new { message = "UserId is required." }, cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteSseEventAsync("error", new { message = "Content is required." }, cancellationToken);
            return;
        }

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers.Append("X-Accel-Buffering", "no");

        try
        {
            await _llmAppService.StreamMessageAsync(
                bizType,
                request,
                async evt =>
                {
                    await WriteSseEventAsync(evt.Type, evt, cancellationToken);
                },
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            await WriteSseEventAsync("error", new { message = ex.Message }, cancellationToken);
        }
        catch (MySqlException ex)
        {
            await WriteSseEventAsync("error", new { message = $"Database error: {ex.Message}" }, cancellationToken);
        }
        catch (Exception ex)
        {
            await WriteSseEventAsync("error", new { message = ex.Message }, cancellationToken);
        }
    }

    [HttpPost("fault-diagnosis/upload")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> UploadFaultImage(
        [FromForm] IFormFile? file,
        [FromForm] string? conversationCode,
        [FromForm] int userId,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            return Ok(ApiResponse<UploadFaultImageResult>.Error(400, "UserId is required."));
        }

        if (file == null)
        {
            return Ok(ApiResponse<UploadFaultImageResult>.Error(400, "上传文件不能为空。"));
        }

        try
        {
            var result = await _llmAppService.UploadFaultImageAsync(conversationCode, file, userId, cancellationToken);
            return Ok(ApiResponse<UploadFaultImageResult>.Success(result, "上传图片成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<UploadFaultImageResult>.Error(400, ex.Message));
        }
        catch (MySqlException ex)
        {
            return Ok(ApiResponse<UploadFaultImageResult>.Error(500, $"Database error: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<UploadFaultImageResult>.Error(500, ex.Message));
        }
    }

    private async Task WriteSseEventAsync(string eventName, object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, SseJsonOptions);
        await Response.WriteAsync($"event: {eventName}\n", cancellationToken);
        await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
