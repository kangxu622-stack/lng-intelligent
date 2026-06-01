using System.Text.Json;
using backend.Llm.Training.Dtos;
using backend.Llm.Training.Repositories;
using backend.Llm.Services;

namespace backend.Llm.Training.Services;

public interface IAnalyticsService
{
    Task<UserAnalyticsDto> GetUserStatsAsync(string userId, CancellationToken ct);
    Task<AdminStatsDto> GetAdminStatsAsync(CancellationToken ct);
    Task<string> AnalyzeWrongQuestionsAsync(string userId, CancellationToken ct);
}

public sealed class AnalyticsService : IAnalyticsService
{
    private readonly ITrainingRepository _repo;
    private readonly LlmProviderSelector _selector;

    public AnalyticsService(ITrainingRepository repo, LlmProviderSelector selector)
    {
        _repo = repo;
        _selector = selector;
    }

    public async Task<UserAnalyticsDto> GetUserStatsAsync(string userId, CancellationToken ct)
    {
        var total = await _repo.GetTotalAnswersAsync(userId, ct);
        var correct = await _repo.GetCorrectAnswersAsync(userId, ct);
        var accuracy = total > 0 ? Math.Round((double)correct / total * 100, 1) : 0.0;

        var stats = new UserStatsDto
        {
            Total = total,
            Correct = correct,
            Wrong = total - correct,
            Accuracy = accuracy
        };

        var kpRows = await _repo.GetKnowledgeAccuracyAsync(userId, ct);
        var knowledgeAccuracy = kpRows.Select(r =>
        {
            var acc = r.Total > 0 ? Math.Round((double)r.Correct / r.Total * 100, 1) : 0.0;
            return new KnowledgeAccuracyDto
            {
                KnowledgeId = r.KnowledgeId,
                KnowledgeName = r.KnowledgeName,
                Total = r.Total,
                Correct = r.Correct,
                Accuracy = acc,
                Level = MasteryLevel(acc)
            };
        }).ToList();

        var weakKnowledge = knowledgeAccuracy.Where(k => k.Accuracy < 70).ToList();
        var recentRecords = (await _repo.GetAnswerRecordsAsync(userId, 10, ct))
            .Select(r => new AnswerRecordDto
            {
                RecordId = r.RecordId,
                UserId = r.UserId,
                QuestionId = r.QuestionId,
                UserAnswer = r.UserAnswer,
                CorrectAnswer = r.CorrectAnswer,
                IsCorrect = r.IsCorrect,
                Score = r.Score,
                AnswerTime = r.AnswerTime,
                Duration = r.Duration,
                Stem = r.Stem,
                QuestionType = r.QuestionType,
                KnowledgeName = r.KnowledgeName
            }).ToList();

        return new UserAnalyticsDto
        {
            Stats = stats,
            KnowledgeAccuracy = knowledgeAccuracy,
            WeakKnowledge = weakKnowledge,
            RecentRecords = recentRecords
        };
    }

    public async Task<AdminStatsDto> GetAdminStatsAsync(CancellationToken ct)
    {
        var totalUsers = await _repo.GetTotalUsersAsync(ct);
        var totalAnswers = await _repo.GetTotalAnswersAllAsync(ct);
        var correctAnswers = await _repo.GetCorrectAnswersAllAsync(ct);
        var avgAccuracy = totalAnswers > 0 ? Math.Round((double)correctAnswers / totalAnswers * 100, 1) : 0.0;

        var perUser = (await _repo.GetPerUserStatsAsync(ct))
            .Select(p => new PerUserStatDto
            {
                Name = p.Name,
                Position = p.Position,
                Total = p.Total,
                Correct = p.Correct
            }).ToList();

        var knowledgeStats = (await _repo.GetPerKnowledgeStatsAsync(ct))
            .Select(k => new KnowledgeStatDto
            {
                KnowledgeName = k.KnowledgeName,
                Total = k.Total,
                Correct = k.Correct
            }).ToList();

        return new AdminStatsDto
        {
            TotalUsers = totalUsers,
            TotalAnswers = totalAnswers,
            AvgAccuracy = avgAccuracy,
            PerUser = perUser,
            KnowledgeStats = knowledgeStats
        };
    }

    public async Task<string> AnalyzeWrongQuestionsAsync(string userId, CancellationToken ct)
    {
        var weakKps = (await _repo.GetKnowledgeAccuracyAsync(userId, ct))
            .Select(r =>
            {
                var acc = r.Total > 0 ? Math.Round((double)r.Correct / r.Total * 100, 1) : 0.0;
                return new { r.KnowledgeName, Accuracy = acc };
            })
            .Where(k => k.Accuracy < 70)
            .ToList();

        if (weakKps.Count == 0)
            return "暂无薄弱知识点，继续保持！";

        var records = await _repo.GetAnswerRecordsAsync(userId, 50, ct);
        var recordsData = records.Select(r => new
        {
            question = r.Stem ?? "",
            user_answer = r.UserAnswer ?? "",
            correct_answer = r.CorrectAnswer ?? "",
            is_correct = r.IsCorrect == 1,
            knowledge = r.KnowledgeName ?? ""
        }).ToList();

        try
        {
            var prompt = TrainingPrompts.WrongQuestionAnalysisPrompt
                .Replace("{answer_records}", JsonSerializer.Serialize(recordsData))
                .Replace("{knowledge_points}", JsonSerializer.Serialize(weakKps));

            var provider = await _selector.SelectProviderAsync(ct);
            var model = _selector.GetActiveModel();
            var messages = new List<OllamaChatMessage> { new() { Role = "user", Content = prompt } };
            var result = await provider.ChatAsync(model, messages, ct);
            return result.Content;
        }
        catch
        {
            return string.Join("\n", weakKps.Take(5).Select(k =>
                $"• {k.KnowledgeName}: 正确率 {k.Accuracy}%，建议重点复习此知识点相关的手册内容。"));
        }
    }

    private static string MasteryLevel(double accuracy) => accuracy switch
    {
        >= 85 => "掌握较好",
        >= 70 => "基本掌握",
        >= 50 => "需强化",
        _ => "薄弱"
    };
}
