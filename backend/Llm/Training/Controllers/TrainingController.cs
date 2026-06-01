using backend.Dtos;
using backend.Llm.Training.Dtos;
using backend.Llm.Training.Services;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace backend.Llm.Training.Controllers;

[ApiController]
[Route("api/training")]
public sealed class TrainingController : ControllerBase
{
    private readonly IManualService _manualService;
    private readonly IKnowledgeService _knowledgeService;
    private readonly IQuestionService _questionService;
    private readonly ITrainingSessionService _sessionService;
    private readonly IAnalyticsService _analyticsService;

    public TrainingController(
        IManualService manualService,
        IKnowledgeService knowledgeService,
        IQuestionService questionService,
        ITrainingSessionService sessionService,
        IAnalyticsService analyticsService)
    {
        _manualService = manualService;
        _knowledgeService = knowledgeService;
        _questionService = questionService;
        _sessionService = sessionService;
        _analyticsService = analyticsService;
    }

    // ─── System ─────────────────────────────────────────────────────

    [HttpGet("system/config")]
    public IActionResult GetConfig()
    {
        return Ok(ApiResponse<object>.Success(new
        {
            positions = new[] { "操作员", "班长", "调度员", "设备工程师" },
            systems = new[] { "储罐系统", "BOG 系统", "气化外输系统", "安全应急", "设备启停", "其他" },
            questionTypes = new[] { "单选题", "判断题", "简答题" },
            difficultyLevels = new[] { "初级", "中级", "高级" }
        }, "ok"));
    }

    // ─── Manuals ────────────────────────────────────────────────────

    [HttpPost("manuals/upload")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> UploadManual(
        [FromForm] IFormFile file,
        [FromForm] string userId = "admin",
        CancellationToken ct = default)
    {
        if (file == null)
            return Ok(ApiResponse.Error(400, "上传文件不能为空。"));

        try
        {
            var result = await _manualService.UploadManualAsync(file, userId, ct);
            return Ok(ApiResponse<ManualDto>.Success(result, "上传成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    [HttpGet("manuals")]
    public async Task<IActionResult> GetManuals(CancellationToken ct)
    {
        try
        {
            var result = await _manualService.GetManualsAsync(ct);
            return Ok(ApiResponse<List<ManualDto>>.Success(result, "ok"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    [HttpPost("manuals/{manualId}/parse")]
    public async Task<IActionResult> ParseManual(
        string manualId,
        [FromQuery] string userId = "admin",
        CancellationToken ct = default)
    {
        try
        {
            var result = await _manualService.ParseManualAsync(manualId, userId, ct);
            return Ok(ApiResponse<ManualParseResultDto>.Success(result, "解析成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse.Error(404, ex.Message));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    [HttpPost("manuals/{manualId}/chunk")]
    public async Task<IActionResult> ChunkManual(
        string manualId,
        [FromQuery] string userId = "admin",
        CancellationToken ct = default)
    {
        try
        {
            var result = await _manualService.ChunkManualAsync(manualId, userId, ct);
            return Ok(ApiResponse<List<ManualChunkDto>>.Success(result, $"拆分完成，共 {result.Count} 个片段"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse.Error(404, ex.Message));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    [HttpDelete("manuals/{manualId}")]
    public async Task<IActionResult> DeleteManual(
        string manualId,
        [FromQuery] string userId = "admin",
        CancellationToken ct = default)
    {
        try
        {
            await _manualService.DeleteManualAsync(manualId, userId, ct);
            return Ok(ApiResponse.Success(message: "删除成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse.Error(400, ex.Message));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    [HttpGet("manuals/search")]
    public async Task<IActionResult> SearchChunks(
        [FromQuery] string keyword = "",
        [FromQuery] string? manualId = null,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _manualService.SearchChunksAsync(keyword, manualId, ct);
            return Ok(ApiResponse<List<ManualChunkDto>>.Success(result, "ok"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    // ─── Knowledge Points ───────────────────────────────────────────

    [HttpGet("knowledge/chunks/{manualId}")]
    public async Task<IActionResult> GetKnowledgeChunks(string manualId, CancellationToken ct)
    {
        try
        {
            var result = await _knowledgeService.GetChunksByManualAsync(manualId, ct);
            return Ok(ApiResponse<List<ManualChunkDto>>.Success(result, "ok"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    [HttpPost("knowledge/extract")]
    public async Task<IActionResult> ExtractKnowledge(
        [FromBody] KnowledgeExtractInput input,
        CancellationToken ct)
    {
        try
        {
            var result = await _knowledgeService.ExtractKnowledgeAsync(input, ct);
            return Ok(ApiResponse<KnowledgeExtractResult>.Success(result, $"提取完成，共 {result.Count} 个知识点"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse.Error(400, ex.Message));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    [HttpGet("knowledge/pending")]
    public async Task<IActionResult> GetPendingKnowledge(CancellationToken ct)
    {
        try
        {
            var result = await _knowledgeService.GetPendingKnowledgeAsync(ct);
            return Ok(ApiResponse<List<KnowledgePointDto>>.Success(result, "ok"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    [HttpGet("knowledge/confirmed")]
    public async Task<IActionResult> GetConfirmedKnowledge(CancellationToken ct)
    {
        try
        {
            var result = await _knowledgeService.GetConfirmedKnowledgeAsync(ct);
            return Ok(ApiResponse<List<KnowledgePointDto>>.Success(result, "ok"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    [HttpPost("knowledge/{knowledgeId}/confirm")]
    public async Task<IActionResult> ConfirmKnowledge(
        string knowledgeId,
        [FromQuery] string userId = "admin",
        CancellationToken ct = default)
    {
        try
        {
            await _knowledgeService.ConfirmKnowledgeAsync(knowledgeId, userId, ct);
            return Ok(ApiResponse.Success(message: "已确认"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    [HttpPut("knowledge/{knowledgeId}")]
    public async Task<IActionResult> UpdateKnowledge(
        string knowledgeId,
        [FromBody] KnowledgeUpdateInput updates,
        [FromQuery] string userId = "admin",
        CancellationToken ct = default)
    {
        try
        {
            await _knowledgeService.UpdateKnowledgeAsync(knowledgeId, updates, userId, ct);
            return Ok(ApiResponse.Success(message: "更新成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    [HttpDelete("knowledge/{knowledgeId}")]
    public async Task<IActionResult> DeleteKnowledge(
        string knowledgeId,
        [FromQuery] string userId = "admin",
        CancellationToken ct = default)
    {
        try
        {
            await _knowledgeService.DeleteKnowledgeAsync(knowledgeId, userId, ct);
            return Ok(ApiResponse.Success(message: "已删除"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse.Error(400, ex.Message));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    // ─── Questions ──────────────────────────────────────────────────

    [HttpPost("questions/generate")]
    public async Task<IActionResult> GenerateQuestions(
        [FromBody] QuestionGenerateInput input,
        CancellationToken ct)
    {
        try
        {
            var result = await _questionService.GenerateQuestionsAsync(input, ct);
            return Ok(ApiResponse<QuestionGenerateResult>.Success(result, $"生成完成，共 {result.Count} 道题目"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse.Error(400, ex.Message));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    [HttpGet("questions/pending")]
    public async Task<IActionResult> GetPendingQuestions(CancellationToken ct)
    {
        try
        {
            var result = await _questionService.GetPendingQuestionsAsync(ct);
            return Ok(ApiResponse<List<QuestionDto>>.Success(result, "ok"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    [HttpGet("questions/approved")]
    public async Task<IActionResult> GetApprovedQuestions(
        [FromQuery] string? knowledgeId = null,
        [FromQuery] string? position = null,
        [FromQuery] string? questionType = null,
        [FromQuery] string? difficulty = null,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _questionService.GetApprovedQuestionsAsync(knowledgeId, position, questionType, difficulty, limit, ct);
            return Ok(ApiResponse<List<QuestionDto>>.Success(result, "ok"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    [HttpGet("questions/{questionId}")]
    public async Task<IActionResult> GetQuestion(string questionId, CancellationToken ct)
    {
        try
        {
            var result = await _questionService.GetQuestionByIdAsync(questionId, ct);
            return Ok(ApiResponse<QuestionDto>.Success(result, "ok"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse.Error(404, ex.Message));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    [HttpPost("questions/{questionId}/approve")]
    public async Task<IActionResult> ApproveQuestion(
        string questionId,
        [FromQuery] string userId = "admin",
        CancellationToken ct = default)
    {
        try
        {
            await _questionService.ApproveQuestionAsync(questionId, userId, ct);
            return Ok(ApiResponse.Success(message: "已通过"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    [HttpPost("questions/{questionId}/reject")]
    public async Task<IActionResult> RejectQuestion(
        string questionId,
        [FromQuery] string userId = "admin",
        CancellationToken ct = default)
    {
        try
        {
            await _questionService.RejectQuestionAsync(questionId, userId, ct);
            return Ok(ApiResponse.Success(message: "已拒绝"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    [HttpPut("questions/{questionId}")]
    public async Task<IActionResult> UpdateQuestion(
        string questionId,
        [FromBody] QuestionUpdateInput updates,
        [FromQuery] string userId = "admin",
        CancellationToken ct = default)
    {
        try
        {
            await _questionService.UpdateQuestionAsync(questionId, updates, userId, ct);
            return Ok(ApiResponse.Success(message: "更新成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    [HttpDelete("questions/{questionId}")]
    public async Task<IActionResult> DeleteQuestion(
        string questionId,
        [FromQuery] string userId = "admin",
        CancellationToken ct = default)
    {
        try
        {
            await _questionService.DeleteQuestionAsync(questionId, userId, ct);
            return Ok(ApiResponse.Success(message: "已删除"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    // ─── Training Sessions ──────────────────────────────────────────

    [HttpPost("sessions/questions")]
    public async Task<IActionResult> GetTrainingQuestions(
        [FromBody] TrainingQueryInput input,
        CancellationToken ct)
    {
        try
        {
            var result = await _sessionService.GetTrainingQuestionsAsync(input, ct);
            return Ok(ApiResponse<List<QuestionDto>>.Success(result, "ok"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    [HttpPost("sessions/answer")]
    public async Task<IActionResult> SubmitAnswer(
        [FromBody] AnswerSubmitInput input,
        CancellationToken ct)
    {
        try
        {
            var result = await _sessionService.SubmitAnswerAsync(input, ct);
            return Ok(ApiResponse<AnswerSubmitResult>.Success(result, "ok"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse.Error(404, ex.Message));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    // ─── Records ────────────────────────────────────────────────────

    [HttpGet("records/{userId}")]
    public async Task<IActionResult> GetAnswerRecords(
        string userId,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _sessionService.GetAnswerRecordsAsync(userId, limit, ct);
            return Ok(ApiResponse<List<AnswerRecordDto>>.Success(result, "ok"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    // ─── Wrong Questions ────────────────────────────────────────────

    [HttpGet("wrong-questions/{userId}")]
    public async Task<IActionResult> GetWrongQuestions(
        string userId,
        [FromQuery] string? knowledgeId = null,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _sessionService.GetWrongQuestionsAsync(userId, knowledgeId, ct);
            return Ok(ApiResponse<List<WrongQuestionDto>>.Success(result, "ok"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    // ─── Analytics ──────────────────────────────────────────────────

    [HttpGet("analytics/stats/{userId}")]
    public async Task<IActionResult> GetUserStats(string userId, CancellationToken ct)
    {
        try
        {
            var result = await _analyticsService.GetUserStatsAsync(userId, ct);
            return Ok(ApiResponse<UserAnalyticsDto>.Success(result, "ok"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    [HttpGet("analytics/admin")]
    public async Task<IActionResult> GetAdminStats(CancellationToken ct)
    {
        try
        {
            var result = await _analyticsService.GetAdminStatsAsync(ct);
            return Ok(ApiResponse<AdminStatsDto>.Success(result, "ok"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    [HttpPost("analytics/wrong-analysis/{userId}")]
    public async Task<IActionResult> AnalyzeWrongQuestions(string userId, CancellationToken ct)
    {
        try
        {
            var analysis = await _analyticsService.AnalyzeWrongQuestionsAsync(userId, ct);
            return Ok(ApiResponse<object>.Success(new { analysis }, "ok"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }
}
