using System.Text.Json;
using backend.Llm.Training.Dtos;
using backend.Llm.Training.Entities;
using backend.Llm.Training.Repositories;
using backend.Llm.Services;

namespace backend.Llm.Training.Services;

public interface IKnowledgeService
{
    Task<List<ManualChunkDto>> GetChunksByManualAsync(string manualId, CancellationToken ct);
    Task<KnowledgeExtractResult> ExtractKnowledgeAsync(KnowledgeExtractInput input, CancellationToken ct);
    Task<List<KnowledgePointDto>> GetPendingKnowledgeAsync(CancellationToken ct);
    Task<List<KnowledgePointDto>> GetConfirmedKnowledgeAsync(CancellationToken ct);
    Task ConfirmKnowledgeAsync(string knowledgeId, string userId, CancellationToken ct);
    Task UpdateKnowledgeAsync(string knowledgeId, KnowledgeUpdateInput updates, string userId, CancellationToken ct);
    Task DeleteKnowledgeAsync(string knowledgeId, string userId, CancellationToken ct);
}

public sealed class KnowledgeService : IKnowledgeService
{
    private readonly ITrainingRepository _repo;
    private readonly LlmProviderSelector _selector;

    public KnowledgeService(ITrainingRepository repo, LlmProviderSelector selector)
    {
        _repo = repo;
        _selector = selector;
    }

    public async Task<List<ManualChunkDto>> GetChunksByManualAsync(string manualId, CancellationToken ct)
    {
        var chunks = await _repo.GetChunksByManualAsync(manualId, ct);
        return chunks.Select(c => new ManualChunkDto
        {
            ChunkId = c.ChunkId, ManualId = c.ManualId,
            ChapterTitle = c.ChapterTitle, SectionNo = c.SectionNo,
            Content = c.Content, PageNo = c.PageNo,
            SystemName = c.SystemName, CreatedTime = c.CreatedTime
        }).ToList();
    }

    public async Task<KnowledgeExtractResult> ExtractKnowledgeAsync(KnowledgeExtractInput input, CancellationToken ct)
    {
        var chunksData = new List<TrainingManualChunk>();
        foreach (var cid in input.ChunkIds)
        {
            var chunk = await _repo.GetChunkByIdAsync(cid, ct);
            if (chunk != null) chunksData.Add(chunk);
        }
        if (chunksData.Count == 0)
            throw new InvalidOperationException("未选择有效的文本片段");

        var combined = string.Join("\n\n", chunksData.Select(c =>
            $"## {c.ChapterTitle ?? ""}\n{c.Content ?? ""}"));

        var prompt = TrainingPrompts.KnowledgeExtractionPrompt.Replace("{manual_content}", Truncate(combined, 8000));
        var reply = await CallLlmAsync(prompt, ct);
        var kps = ParseJsonArray(reply);

        var saved = new List<string>();
        foreach (var item in kps)
        {
            if (item is not JsonElement elem) continue;
            var kp = new TrainingKnowledgePoint
            {
                KnowledgeId = Guid.NewGuid().ToString("N")[..16],
                Name = GetString(elem, "知识点名称") ?? GetString(elem, "name") ?? "",
                SystemName = GetString(elem, "所属系统") ?? GetString(elem, "system"),
                ChapterTitle = GetString(elem, "所属章节") ?? GetString(elem, "chapter"),
                Position = GetString(elem, "适用岗位") ?? GetString(elem, "position"),
                Difficulty = GetString(elem, "难度等级") ?? GetString(elem, "difficulty") ?? "中级",
                RiskLevel = GetString(elem, "风险等级") ?? GetString(elem, "risk_level") ?? "一般",
                SourceChunkId = input.ChunkIds[0],
                ManualBasis = GetString(elem, "手册依据") ?? input.ChunkIds[0],
                Status = "pending",
                CreatedTime = DateTime.Now
            };
            await _repo.InsertKnowledgePointAsync(kp, ct);
            saved.Add(kp.KnowledgeId);
        }

        await _repo.InsertOperationLogAsync(input.UserId, "extract_knowledge", $"{saved.Count} knowledge points", ct);
        return new KnowledgeExtractResult { SavedIds = saved, Count = saved.Count };
    }

    public async Task<List<KnowledgePointDto>> GetPendingKnowledgeAsync(CancellationToken ct)
    {
        var kps = await _repo.GetPendingKnowledgeAsync(ct);
        return await ToDtoList(kps, ct);
    }

    public async Task<List<KnowledgePointDto>> GetConfirmedKnowledgeAsync(CancellationToken ct)
    {
        var kps = await _repo.GetConfirmedKnowledgeAsync(ct);
        return await ToDtoList(kps, ct);
    }

    public async Task ConfirmKnowledgeAsync(string knowledgeId, string userId, CancellationToken ct)
    {
        await _repo.ConfirmKnowledgeAsync(knowledgeId, ct);
        await _repo.InsertOperationLogAsync(userId, "confirm_knowledge", knowledgeId, ct);
    }

    public async Task UpdateKnowledgeAsync(string knowledgeId, KnowledgeUpdateInput updates, string userId, CancellationToken ct)
    {
        var dict = new Dictionary<string, object?>();
        if (updates.Name != null) dict["name"] = updates.Name;
        if (updates.SystemName != null) dict["system_name"] = updates.SystemName;
        if (updates.ChapterTitle != null) dict["chapter_title"] = updates.ChapterTitle;
        if (updates.Position != null) dict["position"] = updates.Position;
        if (updates.Difficulty != null) dict["difficulty"] = updates.Difficulty;
        if (updates.RiskLevel != null) dict["risk_level"] = updates.RiskLevel;
        if (updates.ManualBasis != null) dict["manual_basis"] = updates.ManualBasis;
        await _repo.UpdateKnowledgeAsync(knowledgeId, dict, ct);
        await _repo.InsertOperationLogAsync(userId, "update_knowledge", knowledgeId, ct);
    }

    public async Task DeleteKnowledgeAsync(string knowledgeId, string userId, CancellationToken ct)
    {
        var questionCount = await _repo.GetQuestionCountByKnowledgeAsync(knowledgeId, ct);
        var wrongCount = await _repo.GetWrongCountByKnowledgeAsync(knowledgeId, ct);
        if (questionCount > 0 || wrongCount > 0)
            throw new InvalidOperationException($"知识点已关联 {questionCount} 道题目、{wrongCount} 条错题记录，请先清理关联数据后再删除。");
        await _repo.DeleteKnowledgeAsync(knowledgeId, ct);
        await _repo.InsertOperationLogAsync(userId, "delete_knowledge", knowledgeId, ct);
    }

    private async Task<List<KnowledgePointDto>> ToDtoList(List<TrainingKnowledgePoint> kps, CancellationToken ct)
    {
        var result = new List<KnowledgePointDto>();
        foreach (var kp in kps)
        {
            result.Add(new KnowledgePointDto
            {
                KnowledgeId = kp.KnowledgeId,
                Name = kp.Name,
                SystemName = kp.SystemName,
                ChapterTitle = kp.ChapterTitle,
                Position = kp.Position,
                Difficulty = kp.Difficulty,
                RiskLevel = kp.RiskLevel,
                SourceChunkId = kp.SourceChunkId,
                ManualBasis = kp.ManualBasis,
                Status = kp.Status,
                CreatedTime = kp.CreatedTime,
                QuestionCount = await _repo.GetQuestionCountByKnowledgeAsync(kp.KnowledgeId, ct),
                WrongCount = await _repo.GetWrongCountByKnowledgeAsync(kp.KnowledgeId, ct)
            });
        }
        return result;
    }

    private async Task<string> CallLlmAsync(string prompt, CancellationToken ct)
    {
        var provider = await _selector.SelectProviderAsync(ct);
        var model = _selector.GetActiveModel();
        var messages = new List<OllamaChatMessage> { new() { Role = "user", Content = prompt } };
        var result = await provider.ChatAsync(model, messages, ct);
        return result.Content;
    }

    private static string Truncate(string text, int maxLen) =>
        text.Length <= maxLen ? text : text[..maxLen];

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
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
                return new List<object> { doc.RootElement.Clone() };
        }
        catch { }

        // Try extract from markdown code block
        var match = System.Text.RegularExpressions.Regex.Match(text, @"```(?:json)?\s*([\s\S]*?)```");
        if (match.Success)
        {
            try
            {
                var inner = JsonDocument.Parse(match.Groups[1].Value.Trim());
                if (inner.RootElement.ValueKind == JsonValueKind.Array)
                    return inner.RootElement.EnumerateArray().Cast<object>().ToList();
            }
            catch { }
        }

        // Try find array
        var arrMatch = System.Text.RegularExpressions.Regex.Match(text, @"\[[\s\S]*\]");
        if (arrMatch.Success)
        {
            try
            {
                var inner = JsonDocument.Parse(arrMatch.Value);
                if (inner.RootElement.ValueKind == JsonValueKind.Array)
                    return inner.RootElement.EnumerateArray().Cast<object>().ToList();
            }
            catch { }
        }

        return new List<object> { new Dictionary<string, object> { ["raw_output"] = text } };
    }
}
