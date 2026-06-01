using System.Text.Json;
using backend.Llm.Training.Dtos;
using backend.Llm.Training.Entities;
using backend.Llm.Training.Repositories;
using backend.Llm.Services;

namespace backend.Llm.Training.Services;

public interface ITrainingSessionService
{
    Task<List<QuestionDto>> GetTrainingQuestionsAsync(TrainingQueryInput input, CancellationToken ct);
    Task<AnswerSubmitResult> SubmitAnswerAsync(AnswerSubmitInput input, CancellationToken ct);
    Task<List<AnswerRecordDto>> GetAnswerRecordsAsync(string userId, int limit, CancellationToken ct);
    Task<List<WrongQuestionDto>> GetWrongQuestionsAsync(string userId, string? knowledgeId, CancellationToken ct);
}

public sealed class TrainingSessionService : ITrainingSessionService
{
    private readonly ITrainingRepository _repo;

    public TrainingSessionService(ITrainingRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<QuestionDto>> GetTrainingQuestionsAsync(TrainingQueryInput input, CancellationToken ct)
    {
        var questions = await _repo.GetApprovedQuestionsAsync(
            input.KnowledgeId, input.Position, input.QuestionType, input.Difficulty, input.Limit, ct);
        return questions.Select(q => new QuestionDto
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
        }).ToList();
    }

    public async Task<AnswerSubmitResult> SubmitAnswerAsync(AnswerSubmitInput input, CancellationToken ct)
    {
        var q = await _repo.GetQuestionByIdAsync(input.QuestionId, ct)
            ?? throw new InvalidOperationException("题目不存在");

        var correctAnswer = q.Answer ?? "";
        var isCorrect = input.UserAnswer.Trim() == correctAnswer.Trim();

        var record = new TrainingAnswerRecord
        {
            RecordId = Guid.NewGuid().ToString("N")[..16],
            UserId = input.UserId,
            QuestionId = input.QuestionId,
            UserAnswer = input.UserAnswer,
            CorrectAnswer = correctAnswer,
            IsCorrect = isCorrect ? 1 : 0,
            Score = isCorrect ? 1.0m : 0.0m,
            AnswerTime = DateTime.Now,
            Duration = input.Duration
        };
        await _repo.InsertAnswerRecordAsync(record, ct);

        if (!isCorrect)
        {
            var qRow = await _repo.GetQuestionByIdAsync(input.QuestionId, ct);
            var wrong = new TrainingWrongQuestion
            {
                WrongId = Guid.NewGuid().ToString("N")[..16],
                UserId = input.UserId,
                QuestionId = input.QuestionId,
                KnowledgeId = qRow?.KnowledgeId ?? "",
                WrongAnswer = input.UserAnswer,
                CorrectAnswer = correctAnswer,
                CreatedTime = DateTime.Now,
                ReviewCount = 0
            };
            await _repo.UpsertWrongQuestionAsync(wrong, ct);
        }

        return new AnswerSubmitResult
        {
            RecordId = record.RecordId,
            IsCorrect = isCorrect,
            CorrectAnswer = correctAnswer,
            Explanation = q.Explanation,
            KnowledgeName = q.KnowledgeName
        };
    }

    public async Task<List<AnswerRecordDto>> GetAnswerRecordsAsync(string userId, int limit, CancellationToken ct)
    {
        var records = await _repo.GetAnswerRecordsAsync(userId, limit, ct);
        return records.Select(r => new AnswerRecordDto
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
    }

    public async Task<List<WrongQuestionDto>> GetWrongQuestionsAsync(string userId, string? knowledgeId, CancellationToken ct)
    {
        var wqs = await _repo.GetWrongQuestionsAsync(userId, knowledgeId, ct);
        return wqs.Select(w => new WrongQuestionDto
        {
            WrongId = w.WrongId,
            UserId = w.UserId,
            QuestionId = w.QuestionId,
            KnowledgeId = w.KnowledgeId,
            WrongAnswer = w.WrongAnswer,
            CorrectAnswer = w.CorrectAnswer,
            CreatedTime = w.CreatedTime,
            ReviewCount = w.ReviewCount,
            Stem = w.Stem,
            QuestionType = w.QuestionType,
            KnowledgeName = w.KnowledgeName,
            QAnswer = w.QAnswer,
            Explanation = w.Explanation,
            ManualBasis = w.ManualBasis
        }).ToList();
    }
}
