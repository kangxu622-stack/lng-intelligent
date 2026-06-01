using System.Text.Json;
using backend.Llm.Training.Dtos;
using backend.Llm.Training.Entities;
using backend.Llm.Training.Repositories;
using backend.Llm.Services;

namespace backend.Llm.Training.Services;

public interface IQuestionService
{
    Task<QuestionGenerateResult> GenerateQuestionsAsync(QuestionGenerateInput input, CancellationToken ct);
    Task<List<QuestionDto>> GetPendingQuestionsAsync(CancellationToken ct);
    Task<List<QuestionDto>> GetApprovedQuestionsAsync(string? knowledgeId, string? position, string? questionType, string? difficulty, int limit, CancellationToken ct);
    Task<QuestionDto> GetQuestionByIdAsync(string questionId, CancellationToken ct);
    Task ApproveQuestionAsync(string questionId, string userId, CancellationToken ct);
    Task RejectQuestionAsync(string questionId, string userId, CancellationToken ct);
    Task UpdateQuestionAsync(string questionId, QuestionUpdateInput updates, string userId, CancellationToken ct);
    Task DeleteQuestionAsync(string questionId, string userId, CancellationToken ct);
}

public sealed class QuestionService : IQuestionService
{
    private readonly ITrainingRepository _repo;
    private readonly LlmProviderSelector _selector;

    public QuestionService(ITrainingRepository repo, LlmProviderSelector selector)
    {
        _repo = repo;
        _selector = selector;
    }

    public async Task<QuestionGenerateResult> GenerateQuestionsAsync(QuestionGenerateInput input, CancellationToken ct)
    {
        var context = "";
        if (input.ChunkIds?.Count > 0)
        {
            var chunksData = new List<string>();
            foreach (var cid in input.ChunkIds)
            {
                var chunk = await _repo.GetChunkByIdAsync(cid, ct);
                if (chunk?.Content != null) chunksData.Add(chunk.Content);
            }
            context = string.Join("\n\n", chunksData);
        }
        else if (!string.IsNullOrEmpty(input.CustomText))
        {
            context = input.CustomText;
        }
        else
        {
            throw new InvalidOperationException("请提供文本片段或自定义文本");
        }

        var prompt = TrainingPrompts.QuestionGenerationPrompt
            .Replace("{position}", input.Position)
            .Replace("{count}", input.Count.ToString())
            .Replace("{question_type}", input.QuestionType)
            .Replace("{difficulty}", input.Difficulty)
            .Replace("{retrieved_context}", Truncate(context, 8000));

        var provider = await _selector.SelectProviderAsync(ct);
        var model = _selector.GetActiveModel();
        var messages = new List<OllamaChatMessage> { new() { Role = "user", Content = prompt } };
        var reply = await provider.ChatAsync(model, messages, ct);

        var questions = ParseJsonArray(reply.Content);

        var saved = new List<string>();
        foreach (var item in questions)
        {
            if (item is not JsonElement elem) continue;
            var q = new TrainingQuestion
            {
                QuestionId = Guid.NewGuid().ToString("N")[..16],
                QuestionType = GetString(elem, "题型"),
                Stem = GetString(elem, "题干") ?? "",
                OptionsJson = SerializeOptions(elem),
                Answer = GetString(elem, "正确答案"),
                Explanation = GetString(elem, "答案解析"),
                KnowledgeId = input.KnowledgeId ?? "",
                KnowledgeName = GetString(elem, "对应知识点") ?? input.KnowledgeName ?? "",
                Position = GetString(elem, "适用岗位") ?? input.Position,
                Difficulty = GetString(elem, "难度") ?? input.Difficulty,
                Source = "ai_generated",
                ManualBasis = GetString(elem, "手册依据"),
                ReviewStatus = "pending",
                CreatedTime = DateTime.Now,
                UpdatedTime = DateTime.Now
            };
            await _repo.InsertQuestionAsync(q, ct);
            saved.Add(q.QuestionId);
        }

        await _repo.InsertOperationLogAsync(input.UserId, "generate_questions", $"{saved.Count} questions", ct);
        return new QuestionGenerateResult { SavedIds = saved, Count = saved.Count };
    }

    public async Task<List<QuestionDto>> GetPendingQuestionsAsync(CancellationToken ct)
    {
        var questions = await _repo.GetPendingQuestionsAsync(ct);
        return questions.Select(ToDto).ToList();
    }

    public async Task<List<QuestionDto>> GetApprovedQuestionsAsync(string? knowledgeId, string? position, string? questionType, string? difficulty, int limit, CancellationToken ct)
    {
        var questions = await _repo.GetApprovedQuestionsAsync(knowledgeId, position, questionType, difficulty, limit, ct);
        return questions.Select(ToDto).ToList();
    }

    public async Task<QuestionDto> GetQuestionByIdAsync(string questionId, CancellationToken ct)
    {
        var q = await _repo.GetQuestionByIdAsync(questionId, ct)
            ?? throw new InvalidOperationException("题目不存在");
        return ToDto(q);
    }

    public async Task ApproveQuestionAsync(string questionId, string userId, CancellationToken ct)
    {
        await _repo.ApproveQuestionAsync(questionId, ct);
        await _repo.InsertOperationLogAsync(userId, "approve_question", questionId, ct);
    }

    public async Task RejectQuestionAsync(string questionId, string userId, CancellationToken ct)
    {
        await _repo.RejectQuestionAsync(questionId, ct);
        await _repo.InsertOperationLogAsync(userId, "reject_question", questionId, ct);
    }

    public async Task UpdateQuestionAsync(string questionId, QuestionUpdateInput updates, string userId, CancellationToken ct)
    {
        var dict = new Dictionary<string, object?>();
        if (updates.Stem != null) dict["stem"] = updates.Stem;
        if (updates.Answer != null) dict["answer"] = updates.Answer;
        if (updates.Explanation != null) dict["explanation"] = updates.Explanation;
        if (updates.OptionsJson != null) dict["options_json"] = updates.OptionsJson;
        if (updates.QuestionType != null) dict["question_type"] = updates.QuestionType;
        if (updates.KnowledgeName != null) dict["knowledge_name"] = updates.KnowledgeName;
        if (updates.Position != null) dict["position"] = updates.Position;
        if (updates.Difficulty != null) dict["difficulty"] = updates.Difficulty;
        if (updates.ManualBasis != null) dict["manual_basis"] = updates.ManualBasis;
        if (updates.KnowledgeId != null) dict["knowledge_id"] = updates.KnowledgeId;
        await _repo.UpdateQuestionAsync(questionId, dict, ct);
        await _repo.InsertOperationLogAsync(userId, "update_question", questionId, ct);
    }

    public async Task DeleteQuestionAsync(string questionId, string userId, CancellationToken ct)
    {
        await _repo.DeleteQuestionAsync(questionId, ct);
        await _repo.InsertOperationLogAsync(userId, "delete_question", questionId, ct);
    }

    private static QuestionDto ToDto(TrainingQuestion q) => new()
    {
        QuestionId = q.QuestionId,
        QuestionType = q.QuestionType,
        Stem = q.Stem,
        OptionsJson = q.OptionsJson,
        Answer = q.Answer,
        Explanation = q.Explanation,
        KnowledgeId = q.KnowledgeId,
        KnowledgeName = q.KnowledgeName,
        Position = q.Position,
        Difficulty = q.Difficulty,
        Source = q.Source,
        ManualBasis = q.ManualBasis,
        ReviewStatus = q.ReviewStatus,
        CreatedTime = q.CreatedTime,
        UpdatedTime = q.UpdatedTime
    };

    private static string? SerializeOptions(JsonElement elem)
    {
        if (elem.TryGetProperty("选项", out var opts) && opts.ValueKind == JsonValueKind.Object)
            return JsonSerializer.Serialize(opts);
        if (elem.TryGetProperty("options", out var opts2) && opts2.ValueKind == JsonValueKind.Object)
            return JsonSerializer.Serialize(opts2);
        return null;
    }

    private static string? GetString(JsonElement elem, string key)
    {
        if (elem.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private static List<object> ParseJsonArray(string text)
    {
        text = text.Trim();
        try
        {
            var doc = JsonDocument.Parse(text);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                return doc.RootElement.EnumerateArray().Cast<object>().ToList();
        }
        catch { }
        var match = System.Text.RegularExpressions.Regex.Match(text, @"```(?:json)?\s*([\s\S]*?)```");
        if (match.Success)
        {
            try
            {
                var doc = JsonDocument.Parse(match.Groups[1].Value.Trim());
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    return doc.RootElement.EnumerateArray().Cast<object>().ToList();
            }
            catch { }
        }
        return new List<object>();
    }

    private static string Truncate(string text, int maxLen) =>
        text.Length <= maxLen ? text : text[..maxLen];
}
