using backend.Llm.Training.Entities;
using MySqlConnector;

namespace backend.Llm.Training.Repositories;

public interface ITrainingRepository
{
    // Manual CRUD
    Task InsertManualAsync(TrainingManual manual, CancellationToken ct);
    Task<List<TrainingManual>> GetManualsAsync(CancellationToken ct);
    Task<TrainingManual?> GetManualByIdAsync(string manualId, CancellationToken ct);
    Task DeleteManualAsync(string manualId, CancellationToken ct);
    Task UpdateManualStatusAsync(string manualId, string status, CancellationToken ct);

    // Chunks
    Task DeleteChunksByManualAsync(string manualId, CancellationToken ct);
    Task InsertChunkAsync(TrainingManualChunk chunk, CancellationToken ct);
    Task<List<TrainingManualChunk>> GetChunksByManualAsync(string manualId, CancellationToken ct);
    Task<TrainingManualChunk?> GetChunkByIdAsync(string chunkId, CancellationToken ct);
    Task<List<TrainingManualChunk>> SearchChunksAsync(string keyword, string? manualId, CancellationToken ct);
    Task<int> GetChunkCountByManualAsync(string manualId, CancellationToken ct);

    // Knowledge Points
    Task InsertKnowledgePointAsync(TrainingKnowledgePoint kp, CancellationToken ct);
    Task<List<TrainingKnowledgePoint>> GetPendingKnowledgeAsync(CancellationToken ct);
    Task<List<TrainingKnowledgePoint>> GetConfirmedKnowledgeAsync(CancellationToken ct);
    Task<TrainingKnowledgePoint?> GetKnowledgeByIdAsync(string knowledgeId, CancellationToken ct);
    Task ConfirmKnowledgeAsync(string knowledgeId, CancellationToken ct);
    Task UpdateKnowledgeAsync(string knowledgeId, Dictionary<string, object?> updates, CancellationToken ct);
    Task DeleteKnowledgeAsync(string knowledgeId, CancellationToken ct);
    Task<int> GetKnowledgeQuestionCountAsync(string knowledgeId, CancellationToken ct);
    Task<int> GetKnowledgeWrongCountAsync(string knowledgeId, CancellationToken ct);

    // Questions
    Task InsertQuestionAsync(TrainingQuestion q, CancellationToken ct);
    Task<List<TrainingQuestion>> GetPendingQuestionsAsync(CancellationToken ct);
    Task<List<TrainingQuestion>> GetApprovedQuestionsAsync(string? knowledgeId, string? position, string? questionType, string? difficulty, int limit, CancellationToken ct);
    Task<TrainingQuestion?> GetQuestionByIdAsync(string questionId, CancellationToken ct);
    Task ApproveQuestionAsync(string questionId, CancellationToken ct);
    Task RejectQuestionAsync(string questionId, CancellationToken ct);
    Task UpdateQuestionAsync(string questionId, Dictionary<string, object?> updates, CancellationToken ct);
    Task DeleteQuestionAsync(string questionId, CancellationToken ct);
    Task<int> GetQuestionCountByKnowledgeAsync(string knowledgeId, CancellationToken ct);

    // Answer Records
    Task<string> InsertAnswerRecordAsync(TrainingAnswerRecord record, CancellationToken ct);
    Task<List<TrainingAnswerRecord>> GetAnswerRecordsAsync(string userId, int limit, CancellationToken ct);
    Task<int> GetTotalAnswersAsync(string userId, CancellationToken ct);
    Task<int> GetCorrectAnswersAsync(string userId, CancellationToken ct);

    // Wrong Questions
    Task UpsertWrongQuestionAsync(TrainingWrongQuestion wq, CancellationToken ct);
    Task<List<TrainingWrongQuestion>> GetWrongQuestionsAsync(string userId, string? knowledgeId, CancellationToken ct);
    Task<List<TrainingWrongQuestion>> GetDistinctWrongKnowledgeAsync(string userId, CancellationToken ct);
    Task<int> GetWrongCountByKnowledgeAsync(string knowledgeId, CancellationToken ct);

    // Operation Log
    Task InsertOperationLogAsync(string userId, string actionType, string actionDetail, CancellationToken ct);

    // Analytics
    Task<int> GetTotalUsersAsync(CancellationToken ct);
    Task<int> GetTotalAnswersAllAsync(CancellationToken ct);
    Task<int> GetCorrectAnswersAllAsync(CancellationToken ct);
    Task<List<PerUserStat>> GetPerUserStatsAsync(CancellationToken ct);
    Task<List<KnowledgeStat>> GetPerKnowledgeStatsAsync(CancellationToken ct);
    Task<List<KnowledgeAccuracyRow>> GetKnowledgeAccuracyAsync(string userId, CancellationToken ct);
}

public sealed class PerUserStat
{
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Correct { get; set; }
}

public sealed class KnowledgeStat
{
    public string KnowledgeName { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Correct { get; set; }
}

public sealed class KnowledgeAccuracyRow
{
    public string KnowledgeId { get; set; } = string.Empty;
    public string KnowledgeName { get; set; } = "未分类";
    public int Total { get; set; }
    public int Correct { get; set; }
}

public sealed class TrainingRepository : ITrainingRepository
{
    private readonly string _connectionString;

    public TrainingRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("MySQL")
            ?? throw new InvalidOperationException("MySQL connection string is not configured.");
    }

    private MySqlConnection CreateConnection() => new(_connectionString);

    // ─── Manuals ──────────────────────────────────────────────

    public async Task InsertManualAsync(TrainingManual manual, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        const string sql = "INSERT INTO training_manual (manual_id, manual_name, file_type, file_path, upload_user, upload_time, status) VALUES (@id, @name, @type, @path, @user, @time, @status)";
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", manual.ManualId);
        cmd.Parameters.AddWithValue("@name", manual.ManualName);
        cmd.Parameters.AddWithValue("@type", manual.FileType ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@path", manual.FilePath ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@user", manual.UploadUser ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@time", manual.UploadTime ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@status", manual.Status);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<List<TrainingManual>> GetManualsAsync(CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        const string sql = "SELECT * FROM training_manual ORDER BY upload_time DESC";
        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return ReadManuals(reader);
    }

    public async Task<TrainingManual?> GetManualByIdAsync(string manualId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        const string sql = "SELECT * FROM training_manual WHERE manual_id = @id";
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", manualId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = ReadManuals(reader);
        return list.Count > 0 ? list[0] : null;
    }

    public async Task DeleteManualAsync(string manualId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand("DELETE FROM training_manual WHERE manual_id = @id", conn);
        cmd.Parameters.AddWithValue("@id", manualId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateManualStatusAsync(string manualId, string status, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand("UPDATE training_manual SET status = @status WHERE manual_id = @id", conn);
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@id", manualId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    // ─── Chunks ───────────────────────────────────────────────

    public async Task DeleteChunksByManualAsync(string manualId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand("DELETE FROM training_manual_chunk WHERE manual_id = @id", conn);
        cmd.Parameters.AddWithValue("@id", manualId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task InsertChunkAsync(TrainingManualChunk chunk, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        const string sql = "INSERT INTO training_manual_chunk (chunk_id, manual_id, chapter_title, section_no, content, page_no, system_name, embedding_id, created_time) VALUES (@cid, @mid, @title, @sno, @content, @page, @sys, @emb, @time)";
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@cid", chunk.ChunkId);
        cmd.Parameters.AddWithValue("@mid", chunk.ManualId);
        cmd.Parameters.AddWithValue("@title", chunk.ChapterTitle ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@sno", chunk.SectionNo ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@content", chunk.Content ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@page", chunk.PageNo ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@sys", chunk.SystemName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@emb", chunk.EmbeddingId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@time", chunk.CreatedTime ?? (object)DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<List<TrainingManualChunk>> GetChunksByManualAsync(string manualId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        const string sql = "SELECT * FROM training_manual_chunk WHERE manual_id = @id ORDER BY section_no";
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", manualId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return ReadChunks(reader);
    }

    public async Task<TrainingManualChunk?> GetChunkByIdAsync(string chunkId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        const string sql = "SELECT * FROM training_manual_chunk WHERE chunk_id = @id";
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", chunkId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = ReadChunks(reader);
        return list.Count > 0 ? list[0] : null;
    }

    public async Task<List<TrainingManualChunk>> SearchChunksAsync(string keyword, string? manualId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        string sql;
        var cmd = new MySqlCommand();
        if (!string.IsNullOrEmpty(manualId))
        {
            sql = "SELECT * FROM training_manual_chunk WHERE manual_id = @mid AND content LIKE @kw ORDER BY section_no";
            cmd.Parameters.AddWithValue("@mid", manualId);
        }
        else
        {
            sql = "SELECT * FROM training_manual_chunk WHERE content LIKE @kw ORDER BY section_no";
        }
        cmd.CommandText = sql;
        cmd.Connection = conn;
        cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return ReadChunks(reader);
    }

    public async Task<int> GetChunkCountByManualAsync(string manualId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        const string sql = "SELECT COUNT(*) FROM training_manual_chunk WHERE manual_id = @id";
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", manualId);
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result);
    }

    // ─── Knowledge Points ─────────────────────────────────────

    public async Task InsertKnowledgePointAsync(TrainingKnowledgePoint kp, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        const string sql = "INSERT INTO training_knowledge_point (knowledge_id, name, system_name, chapter_title, position, difficulty, risk_level, source_chunk_id, manual_basis, status, created_time) VALUES (@id, @name, @sys, @ch, @pos, @diff, @risk, @scid, @mb, @status, @time)";
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", kp.KnowledgeId);
        cmd.Parameters.AddWithValue("@name", kp.Name);
        cmd.Parameters.AddWithValue("@sys", kp.SystemName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ch", kp.ChapterTitle ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@pos", kp.Position ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@diff", kp.Difficulty ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@risk", kp.RiskLevel ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@scid", kp.SourceChunkId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@mb", kp.ManualBasis ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@status", kp.Status);
        cmd.Parameters.AddWithValue("@time", kp.CreatedTime ?? (object)DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<List<TrainingKnowledgePoint>> GetPendingKnowledgeAsync(CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        const string sql = "SELECT * FROM training_knowledge_point WHERE status = 'pending' ORDER BY created_time DESC";
        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return ReadKnowledgePoints(reader);
    }

    public async Task<List<TrainingKnowledgePoint>> GetConfirmedKnowledgeAsync(CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        const string sql = "SELECT * FROM training_knowledge_point WHERE status = 'confirmed' ORDER BY system_name, name";
        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return ReadKnowledgePoints(reader);
    }

    public async Task<TrainingKnowledgePoint?> GetKnowledgeByIdAsync(string knowledgeId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        const string sql = "SELECT * FROM training_knowledge_point WHERE knowledge_id = @id";
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", knowledgeId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = ReadKnowledgePoints(reader);
        return list.Count > 0 ? list[0] : null;
    }

    public async Task ConfirmKnowledgeAsync(string knowledgeId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand("UPDATE training_knowledge_point SET status = 'confirmed' WHERE knowledge_id = @id", conn);
        cmd.Parameters.AddWithValue("@id", knowledgeId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateKnowledgeAsync(string knowledgeId, Dictionary<string, object?> updates, CancellationToken ct)
    {
        if (updates.Count == 0) return;
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        var setClauses = new List<string>();
        var cmd = new MySqlCommand();
        cmd.Connection = conn;
        foreach (var kv in updates)
        {
            var paramName = $"@p_{kv.Key}";
            setClauses.Add($"{kv.Key} = {paramName}");
            cmd.Parameters.AddWithValue(paramName, kv.Value ?? DBNull.Value);
        }
        cmd.Parameters.AddWithValue("@id", knowledgeId);
        cmd.CommandText = $"UPDATE training_knowledge_point SET {string.Join(", ", setClauses)} WHERE knowledge_id = @id";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteKnowledgeAsync(string knowledgeId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand("DELETE FROM training_knowledge_point WHERE knowledge_id = @id", conn);
        cmd.Parameters.AddWithValue("@id", knowledgeId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<int> GetKnowledgeQuestionCountAsync(string knowledgeId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM training_question WHERE knowledge_id = @id", conn);
        cmd.Parameters.AddWithValue("@id", knowledgeId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }

    public async Task<int> GetKnowledgeWrongCountAsync(string knowledgeId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM training_wrong_question WHERE knowledge_id = @id", conn);
        cmd.Parameters.AddWithValue("@id", knowledgeId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }

    // ─── Questions ────────────────────────────────────────────

    public async Task InsertQuestionAsync(TrainingQuestion q, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        const string sql = @"INSERT INTO training_question (question_id, question_type, stem, options_json, answer, explanation, knowledge_id, knowledge_name, position, difficulty, source, manual_basis, review_status, created_time, updated_time)
            VALUES (@id, @type, @stem, @opts, @ans, @exp, @kid, @kname, @pos, @diff, @src, @mb, @status, @ctime, @utime)";
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", q.QuestionId);
        cmd.Parameters.AddWithValue("@type", q.QuestionType ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@stem", q.Stem);
        cmd.Parameters.AddWithValue("@opts", q.OptionsJson ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ans", q.Answer ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@exp", q.Explanation ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@kid", q.KnowledgeId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@kname", q.KnowledgeName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@pos", q.Position ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@diff", q.Difficulty ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@src", q.Source ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@mb", q.ManualBasis ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@status", q.ReviewStatus);
        cmd.Parameters.AddWithValue("@ctime", q.CreatedTime ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@utime", q.UpdatedTime ?? (object)DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<List<TrainingQuestion>> GetPendingQuestionsAsync(CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        const string sql = "SELECT * FROM training_question WHERE review_status = 'pending' ORDER BY created_time DESC";
        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return ReadQuestions(reader);
    }

    public async Task<List<TrainingQuestion>> GetApprovedQuestionsAsync(string? knowledgeId, string? position, string? questionType, string? difficulty, int limit, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        var conditions = new List<string> { "review_status = 'approved'" };
        var cmd = new MySqlCommand();
        cmd.Connection = conn;
        if (!string.IsNullOrEmpty(knowledgeId)) { conditions.Add("knowledge_id = @kid"); cmd.Parameters.AddWithValue("@kid", knowledgeId); }
        if (!string.IsNullOrEmpty(position)) { conditions.Add("position = @pos"); cmd.Parameters.AddWithValue("@pos", position); }
        if (!string.IsNullOrEmpty(questionType)) { conditions.Add("question_type = @type"); cmd.Parameters.AddWithValue("@type", questionType); }
        if (!string.IsNullOrEmpty(difficulty)) { conditions.Add("difficulty = @diff"); cmd.Parameters.AddWithValue("@diff", difficulty); }
        cmd.Parameters.AddWithValue("@limit", limit);
        cmd.CommandText = $"SELECT * FROM training_question WHERE {string.Join(" AND ", conditions)} ORDER BY RAND() LIMIT @limit";
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return ReadQuestions(reader);
    }

    public async Task<TrainingQuestion?> GetQuestionByIdAsync(string questionId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        const string sql = "SELECT * FROM training_question WHERE question_id = @id";
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", questionId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = ReadQuestions(reader);
        return list.Count > 0 ? list[0] : null;
    }

    public async Task ApproveQuestionAsync(string questionId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand("UPDATE training_question SET review_status = 'approved', updated_time = NOW() WHERE question_id = @id", conn);
        cmd.Parameters.AddWithValue("@id", questionId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task RejectQuestionAsync(string questionId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand("UPDATE training_question SET review_status = 'rejected', updated_time = NOW() WHERE question_id = @id", conn);
        cmd.Parameters.AddWithValue("@id", questionId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateQuestionAsync(string questionId, Dictionary<string, object?> updates, CancellationToken ct)
    {
        if (updates.Count == 0) return;
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        var setClauses = new List<string>();
        var cmd = new MySqlCommand();
        cmd.Connection = conn;
        foreach (var kv in updates)
        {
            var paramName = $"@p_{kv.Key}";
            setClauses.Add($"{kv.Key} = {paramName}");
            cmd.Parameters.AddWithValue(paramName, kv.Value ?? DBNull.Value);
        }
        cmd.Parameters.AddWithValue("@id", questionId);
        cmd.CommandText = $"UPDATE training_question SET {string.Join(", ", setClauses)}, updated_time = NOW() WHERE question_id = @id";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteQuestionAsync(string questionId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand("DELETE FROM training_question WHERE question_id = @id", conn);
        cmd.Parameters.AddWithValue("@id", questionId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<int> GetQuestionCountByKnowledgeAsync(string knowledgeId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM training_question WHERE knowledge_id = @id", conn);
        cmd.Parameters.AddWithValue("@id", knowledgeId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }

    // ─── Answer Records ───────────────────────────────────────

    public async Task<string> InsertAnswerRecordAsync(TrainingAnswerRecord record, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        const string sql = "INSERT INTO training_answer_record (record_id, user_id, question_id, user_answer, correct_answer, is_correct, score, answer_time, duration) VALUES (@id, @uid, @qid, @ua, @ca, @ic, @score, @time, @dur)";
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", record.RecordId);
        cmd.Parameters.AddWithValue("@uid", record.UserId);
        cmd.Parameters.AddWithValue("@qid", record.QuestionId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ua", record.UserAnswer ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ca", record.CorrectAnswer ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ic", record.IsCorrect);
        cmd.Parameters.AddWithValue("@score", record.Score ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@time", record.AnswerTime ?? DateTime.Now);
        cmd.Parameters.AddWithValue("@dur", record.Duration ?? (object)DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
        return record.RecordId;
    }

    public async Task<List<TrainingAnswerRecord>> GetAnswerRecordsAsync(string userId, int limit, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        const string sql = @"SELECT ar.*, q.stem, q.question_type, q.knowledge_name
            FROM training_answer_record ar
            LEFT JOIN training_question q ON ar.question_id = q.question_id
            WHERE ar.user_id = @uid ORDER BY ar.answer_time DESC LIMIT @limit";
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@uid", userId);
        cmd.Parameters.AddWithValue("@limit", limit);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return ReadAnswerRecords(reader);
    }

    public async Task<int> GetTotalAnswersAsync(string userId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM training_answer_record WHERE user_id = @uid", conn);
        cmd.Parameters.AddWithValue("@uid", userId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }

    public async Task<int> GetCorrectAnswersAsync(string userId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM training_answer_record WHERE user_id = @uid AND is_correct = 1", conn);
        cmd.Parameters.AddWithValue("@uid", userId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }

    // ─── Wrong Questions ──────────────────────────────────────

    public async Task UpsertWrongQuestionAsync(TrainingWrongQuestion wq, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        // Check if exists
        await using var checkCmd = new MySqlCommand(
            "SELECT wrong_id, review_count FROM training_wrong_question WHERE user_id = @uid AND question_id = @qid", conn);
        checkCmd.Parameters.AddWithValue("@uid", wq.UserId);
        checkCmd.Parameters.AddWithValue("@qid", wq.QuestionId ?? (object)DBNull.Value);
        await using var reader = await checkCmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            var wrongId = reader.GetString(0);
            reader.Close();
            await using var updateCmd = new MySqlCommand(
                "UPDATE training_wrong_question SET wrong_answer = @wa, correct_answer = @ca, review_count = review_count + 1 WHERE wrong_id = @id", conn);
            updateCmd.Parameters.AddWithValue("@wa", wq.WrongAnswer ?? (object)DBNull.Value);
            updateCmd.Parameters.AddWithValue("@ca", wq.CorrectAnswer ?? (object)DBNull.Value);
            updateCmd.Parameters.AddWithValue("@id", wrongId);
            await updateCmd.ExecuteNonQueryAsync(ct);
        }
        else
        {
            reader.Close();
            const string sql = "INSERT INTO training_wrong_question (wrong_id, user_id, question_id, knowledge_id, wrong_answer, correct_answer, created_time, review_count) VALUES (@id, @uid, @qid, @kid, @wa, @ca, @time, 0)";
            await using var insertCmd = new MySqlCommand(sql, conn);
            insertCmd.Parameters.AddWithValue("@id", wq.WrongId);
            insertCmd.Parameters.AddWithValue("@uid", wq.UserId);
            insertCmd.Parameters.AddWithValue("@qid", wq.QuestionId ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("@kid", wq.KnowledgeId ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("@wa", wq.WrongAnswer ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("@ca", wq.CorrectAnswer ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("@time", wq.CreatedTime ?? DateTime.Now);
            await insertCmd.ExecuteNonQueryAsync(ct);
        }
    }

    public async Task<List<TrainingWrongQuestion>> GetWrongQuestionsAsync(string userId, string? knowledgeId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        string sql;
        var cmd = new MySqlCommand();
        cmd.Connection = conn;
        if (!string.IsNullOrEmpty(knowledgeId))
        {
            sql = @"SELECT wq.*, q.stem, q.question_type, q.knowledge_name, q.answer as q_answer, q.explanation, q.manual_basis
                FROM training_wrong_question wq LEFT JOIN training_question q ON wq.question_id = q.question_id
                WHERE wq.user_id = @uid AND wq.knowledge_id = @kid ORDER BY wq.created_time DESC";
            cmd.Parameters.AddWithValue("@kid", knowledgeId);
        }
        else
        {
            sql = @"SELECT wq.*, q.stem, q.question_type, q.knowledge_name, q.answer as q_answer, q.explanation, q.manual_basis
                FROM training_wrong_question wq LEFT JOIN training_question q ON wq.question_id = q.question_id
                WHERE wq.user_id = @uid ORDER BY wq.created_time DESC";
        }
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@uid", userId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return ReadWrongQuestions(reader);
    }

    public async Task<List<TrainingWrongQuestion>> GetDistinctWrongKnowledgeAsync(string userId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand(
            "SELECT DISTINCT knowledge_id FROM training_wrong_question WHERE user_id = @uid AND knowledge_id IS NOT NULL AND knowledge_id != ''", conn);
        cmd.Parameters.AddWithValue("@uid", userId);
        var ids = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            ids.Add(reader.GetString(0));
        return ids.Select(id => new TrainingWrongQuestion { KnowledgeId = id }).ToList();
    }

    public async Task<int> GetWrongCountByKnowledgeAsync(string knowledgeId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM training_wrong_question WHERE knowledge_id = @id", conn);
        cmd.Parameters.AddWithValue("@id", knowledgeId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }

    // ─── Operation Log ────────────────────────────────────────

    public async Task InsertOperationLogAsync(string userId, string actionType, string actionDetail, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        const string sql = "INSERT INTO training_operation_log (log_id, user_id, action_type, action_detail, created_time) VALUES (@id, @uid, @type, @detail, NOW())";
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString("N")[..16]);
        cmd.Parameters.AddWithValue("@uid", userId);
        cmd.Parameters.AddWithValue("@type", actionType);
        cmd.Parameters.AddWithValue("@detail", actionDetail);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    // ─── Analytics ────────────────────────────────────────────

    public async Task<int> GetTotalUsersAsync(CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM sys_user WHERE role_id = (SELECT role_id FROM sys_role WHERE role_code = 'OPERATOR')", conn);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }

    public async Task<int> GetTotalAnswersAllAsync(CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM training_answer_record", conn);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }

    public async Task<int> GetCorrectAnswersAllAsync(CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM training_answer_record WHERE is_correct = 1", conn);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }

    public async Task<List<PerUserStat>> GetPerUserStatsAsync(CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        const string sql = @"SELECT u.username as name, u.department as position, COUNT(ar.record_id) as total, SUM(CASE WHEN ar.is_correct = 1 THEN 1 ELSE 0 END) as correct
            FROM sys_user u LEFT JOIN training_answer_record ar ON CAST(u.user_id AS CHAR) = ar.user_id
            WHERE u.role_id = (SELECT role_id FROM sys_role WHERE role_code = 'OPERATOR' LIMIT 1)
            GROUP BY u.user_id, u.username, u.department ORDER BY total DESC";
        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<PerUserStat>();
        while (await reader.ReadAsync(ct))
            list.Add(new PerUserStat { Name = reader.GetString(0), Position = reader.IsDBNull(1) ? "" : reader.GetString(1), Total = reader.GetInt32(2), Correct = reader.GetInt32(3) });
        return list;
    }

    public async Task<List<KnowledgeStat>> GetPerKnowledgeStatsAsync(CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        const string sql = @"SELECT q.knowledge_name, COUNT(*) as total, SUM(CASE WHEN ar.is_correct = 1 THEN 1 ELSE 0 END) as correct
            FROM training_answer_record ar LEFT JOIN training_question q ON ar.question_id = q.question_id
            WHERE q.knowledge_name IS NOT NULL GROUP BY q.knowledge_id, q.knowledge_name ORDER BY total DESC LIMIT 20";
        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<KnowledgeStat>();
        while (await reader.ReadAsync(ct))
            list.Add(new KnowledgeStat { KnowledgeName = reader.IsDBNull(0) ? "未分类" : reader.GetString(0), Total = reader.GetInt32(1), Correct = reader.GetInt32(2) });
        return list;
    }

    public async Task<List<KnowledgeAccuracyRow>> GetKnowledgeAccuracyAsync(string userId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        const string sql = @"SELECT q.knowledge_id, q.knowledge_name, COUNT(*) as total, SUM(CASE WHEN ar.is_correct = 1 THEN 1 ELSE 0 END) as correct
            FROM training_answer_record ar LEFT JOIN training_question q ON ar.question_id = q.question_id
            WHERE ar.user_id = @uid GROUP BY q.knowledge_id, q.knowledge_name ORDER BY total DESC";
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@uid", userId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<KnowledgeAccuracyRow>();
        while (await reader.ReadAsync(ct))
            list.Add(new KnowledgeAccuracyRow
            {
                KnowledgeId = reader.IsDBNull(0) ? "" : reader.GetString(0),
                KnowledgeName = reader.IsDBNull(1) ? "未分类" : reader.GetString(1),
                Total = reader.GetInt32(2),
                Correct = reader.GetInt32(3)
            });
        return list;
    }

    // ─── Private readers ──────────────────────────────────────

    private static List<TrainingManual> ReadManuals(MySqlDataReader reader)
    {
        var list = new List<TrainingManual>();
        while (reader.Read())
            list.Add(new TrainingManual
            {
                ManualId = reader.GetString("manual_id"),
                ManualName = reader.GetString("manual_name"),
                FileType = reader.GetValueSafe("file_type"),
                FilePath = reader.GetValueSafe("file_path"),
                UploadUser = reader.GetValueSafe("upload_user"),
                UploadTime = reader.GetDateTimeSafe("upload_time"),
                Status = reader.GetString("status")
            });
        return list;
    }

    private static List<TrainingManualChunk> ReadChunks(MySqlDataReader reader)
    {
        var list = new List<TrainingManualChunk>();
        while (reader.Read())
            list.Add(new TrainingManualChunk
            {
                ChunkId = reader.GetString("chunk_id"),
                ManualId = reader.GetString("manual_id"),
                ChapterTitle = reader.GetValueSafe("chapter_title"),
                SectionNo = reader.GetValueSafe("section_no"),
                Content = reader.GetValueSafe("content"),
                PageNo = reader.GetValueSafe("page_no"),
                SystemName = reader.GetValueSafe("system_name"),
                EmbeddingId = reader.GetValueSafe("embedding_id"),
                CreatedTime = reader.GetDateTimeSafe("created_time")
            });
        return list;
    }

    private static List<TrainingKnowledgePoint> ReadKnowledgePoints(MySqlDataReader reader)
    {
        var list = new List<TrainingKnowledgePoint>();
        while (reader.Read())
            list.Add(new TrainingKnowledgePoint
            {
                KnowledgeId = reader.GetString("knowledge_id"),
                Name = reader.GetString("name"),
                SystemName = reader.GetValueSafe("system_name"),
                ChapterTitle = reader.GetValueSafe("chapter_title"),
                Position = reader.GetValueSafe("position"),
                Difficulty = reader.GetValueSafe("difficulty"),
                RiskLevel = reader.GetValueSafe("risk_level"),
                SourceChunkId = reader.GetValueSafe("source_chunk_id"),
                ManualBasis = reader.GetValueSafe("manual_basis"),
                Status = reader.GetString("status"),
                CreatedTime = reader.GetDateTimeSafe("created_time")
            });
        return list;
    }

    private static List<TrainingQuestion> ReadQuestions(MySqlDataReader reader)
    {
        var list = new List<TrainingQuestion>();
        while (reader.Read())
            list.Add(new TrainingQuestion
            {
                QuestionId = reader.GetString("question_id"),
                QuestionType = reader.GetValueSafe("question_type"),
                Stem = reader.GetString("stem"),
                OptionsJson = reader.GetValueSafe("options_json"),
                Answer = reader.GetValueSafe("answer"),
                Explanation = reader.GetValueSafe("explanation"),
                KnowledgeId = reader.GetValueSafe("knowledge_id"),
                KnowledgeName = reader.GetValueSafe("knowledge_name"),
                Position = reader.GetValueSafe("position"),
                Difficulty = reader.GetValueSafe("difficulty"),
                Source = reader.GetValueSafe("source"),
                ManualBasis = reader.GetValueSafe("manual_basis"),
                ReviewStatus = reader.GetString("review_status"),
                CreatedTime = reader.GetDateTimeSafe("created_time"),
                UpdatedTime = reader.GetDateTimeSafe("updated_time")
            });
        return list;
    }

    private static List<TrainingAnswerRecord> ReadAnswerRecords(MySqlDataReader reader)
    {
        var list = new List<TrainingAnswerRecord>();
        while (reader.Read())
            list.Add(new TrainingAnswerRecord
            {
                RecordId = reader.GetString("record_id"),
                UserId = reader.GetString("user_id"),
                QuestionId = reader.GetValueSafe("question_id"),
                UserAnswer = reader.GetValueSafe("user_answer"),
                CorrectAnswer = reader.GetValueSafe("correct_answer"),
                IsCorrect = reader.GetInt32("is_correct"),
                Score = reader.GetDecimalSafe("score"),
                AnswerTime = reader.GetDateTimeSafe("answer_time"),
                Duration = reader.GetInt32Safe("duration"),
                Stem = reader.GetValueSafe("stem"),
                QuestionType = reader.GetValueSafe("question_type"),
                KnowledgeName = reader.GetValueSafe("knowledge_name")
            });
        return list;
    }

    private static List<TrainingWrongQuestion> ReadWrongQuestions(MySqlDataReader reader)
    {
        var list = new List<TrainingWrongQuestion>();
        while (reader.Read())
            list.Add(new TrainingWrongQuestion
            {
                WrongId = reader.GetString("wrong_id"),
                UserId = reader.GetString("user_id"),
                QuestionId = reader.GetValueSafe("question_id"),
                KnowledgeId = reader.GetValueSafe("knowledge_id"),
                WrongAnswer = reader.GetValueSafe("wrong_answer"),
                CorrectAnswer = reader.GetValueSafe("correct_answer"),
                CreatedTime = reader.GetDateTimeSafe("created_time"),
                ReviewCount = reader.GetInt32("review_count"),
                Stem = reader.GetValueSafe("stem"),
                QuestionType = reader.GetValueSafe("question_type"),
                KnowledgeName = reader.GetValueSafe("knowledge_name"),
                QAnswer = reader.GetValueSafe("q_answer"),
                Explanation = reader.GetValueSafe("explanation"),
                ManualBasis = reader.GetValueSafe("manual_basis")
            });
        return list;
    }
}

internal static class MySqlDataReaderExtensions
{
    public static string? GetValueSafe(this MySqlDataReader reader, string name)
    {
        var idx = reader.GetOrdinal(name);
        return reader.IsDBNull(idx) ? null : reader.GetString(idx);
    }

    public static DateTime? GetDateTimeSafe(this MySqlDataReader reader, string name)
    {
        var idx = reader.GetOrdinal(name);
        return reader.IsDBNull(idx) ? null : reader.GetDateTime(idx);
    }

    public static decimal? GetDecimalSafe(this MySqlDataReader reader, string name)
    {
        var idx = reader.GetOrdinal(name);
        return reader.IsDBNull(idx) ? null : reader.GetDecimal(idx);
    }

    public static int? GetInt32Safe(this MySqlDataReader reader, string name)
    {
        var idx = reader.GetOrdinal(name);
        return reader.IsDBNull(idx) ? null : reader.GetInt32(idx);
    }
}
