using backend.Llm.Entities;
using MySqlConnector;

namespace backend.Llm.Repositories;

public sealed class LlmRepository : ILlmRepository
{
    private readonly string _connectionString;

    public LlmRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string is not configured.");
    }

    public async Task<long> InsertConversationAsync(LlmConversation conversation, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            INSERT INTO llm_conversation
            (conversation_code, user_id, biz_type, title, summary, last_message_at)
            VALUES
            (@conversationCode, @userId, @bizType, @title, @summary, @lastMessageAt);
            SELECT LAST_INSERT_ID();
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@conversationCode", conversation.ConversationCode);
        cmd.Parameters.AddWithValue("@userId", conversation.UserId);
        cmd.Parameters.AddWithValue("@bizType", conversation.BizType);
        cmd.Parameters.AddWithValue("@title", conversation.Title);
        cmd.Parameters.AddWithValue("@summary", conversation.Summary ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@lastMessageAt", conversation.LastMessageAt.HasValue ? conversation.LastMessageAt.Value : (object)DBNull.Value);

        var scalar = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(scalar);
    }

    public async Task<LlmConversation?> GetConversationByCodeAsync(string conversationCode, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT conversation_id, conversation_code, user_id, biz_type, title, summary, status, last_message_at, created_at, updated_at
            FROM llm_conversation
            WHERE conversation_code = @conversationCode
            LIMIT 1;
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@conversationCode", conversationCode);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return ReadConversation(reader);
    }

    public async Task<IReadOnlyList<LlmConversation>> GetConversationsAsync(int userId, string bizType, CancellationToken cancellationToken)
    {
        var result = new List<LlmConversation>();

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT conversation_id, conversation_code, user_id, biz_type, title, summary, status, last_message_at, created_at, updated_at
            FROM llm_conversation
            WHERE user_id = @userId AND biz_type = @bizType
            ORDER BY COALESCE(last_message_at, created_at) DESC, conversation_id DESC;
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@bizType", bizType);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(ReadConversation(reader));
        }

        return result;
    }

    public async Task<IReadOnlyList<LlmMessage>> GetMessagesAsync(long conversationId, CancellationToken cancellationToken)
    {
        var result = new List<LlmMessage>();

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT message_id, conversation_id, role, content_type, content, model_name,
                   prompt_tokens, completion_tokens, total_tokens, response_ms,
                   sequence_no, parent_message_id, created_at
            FROM llm_message
            WHERE conversation_id = @conversationId
            ORDER BY sequence_no ASC, message_id ASC;
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@conversationId", conversationId);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(ReadMessage(reader));
        }

        return result;
    }

    public async Task<int> GetNextMessageSequenceNoAsync(long conversationId, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT COALESCE(MAX(sequence_no), 0) + 1
            FROM llm_message
            WHERE conversation_id = @conversationId;
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@conversationId", conversationId);

        var scalar = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(scalar);
    }

    public async Task<long> InsertMessageAsync(LlmMessage message, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            INSERT INTO llm_message
            (conversation_id, role, content_type, content, model_name,
             prompt_tokens, completion_tokens, total_tokens, response_ms,
             sequence_no, parent_message_id)
            VALUES
            (@conversationId, @role, @contentType, @content, @modelName,
             @promptTokens, @completionTokens, @totalTokens, @responseMs,
             @sequenceNo, @parentMessageId);
            SELECT LAST_INSERT_ID();
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@conversationId", message.ConversationId);
        cmd.Parameters.AddWithValue("@role", message.Role);
        cmd.Parameters.AddWithValue("@contentType", message.ContentType);
        cmd.Parameters.AddWithValue("@content", message.Content);
        cmd.Parameters.AddWithValue("@modelName", message.ModelName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@promptTokens", message.PromptTokens.HasValue ? message.PromptTokens.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@completionTokens", message.CompletionTokens.HasValue ? message.CompletionTokens.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@totalTokens", message.TotalTokens.HasValue ? message.TotalTokens.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@responseMs", message.ResponseMs.HasValue ? message.ResponseMs.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@sequenceNo", message.SequenceNo);
        cmd.Parameters.AddWithValue("@parentMessageId", message.ParentMessageId.HasValue ? message.ParentMessageId.Value : (object)DBNull.Value);

        var scalar = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(scalar);
    }

    public async Task<long> InsertAttachmentAsync(LlmAttachment attachment, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            INSERT INTO llm_attachment
            (conversation_id, message_id, attachment_type, file_name, file_ext, mime_type, file_size, storage_path, preview_url, ocr_text)
            VALUES
            (@conversationId, @messageId, @attachmentType, @fileName, @fileExt, @mimeType, @fileSize, @storagePath, @previewUrl, @ocrText);
            SELECT LAST_INSERT_ID();
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@conversationId", attachment.ConversationId);
        cmd.Parameters.AddWithValue("@messageId", attachment.MessageId.HasValue ? attachment.MessageId.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@attachmentType", attachment.AttachmentType);
        cmd.Parameters.AddWithValue("@fileName", attachment.FileName);
        cmd.Parameters.AddWithValue("@fileExt", attachment.FileExt ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@mimeType", attachment.MimeType ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@fileSize", attachment.FileSize.HasValue ? attachment.FileSize.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@storagePath", attachment.StoragePath);
        cmd.Parameters.AddWithValue("@previewUrl", attachment.PreviewUrl ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ocrText", attachment.OcrText ?? (object)DBNull.Value);

        var scalar = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(scalar);
    }

    public async Task TouchConversationAsync(long conversationId, DateTime lastMessageAt, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            UPDATE llm_conversation
            SET last_message_at = @lastMessageAt,
                updated_at = CURRENT_TIMESTAMP
            WHERE conversation_id = @conversationId;
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@conversationId", conversationId);
        cmd.Parameters.AddWithValue("@lastMessageAt", lastMessageAt);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteConversationAsync(long conversationId, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            DELETE FROM llm_conversation
            WHERE conversation_id = @conversationId;
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@conversationId", conversationId);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static LlmConversation ReadConversation(MySqlDataReader reader)
    {
        return new LlmConversation
        {
            ConversationId = reader.GetInt64("conversation_id"),
            ConversationCode = reader.GetString("conversation_code"),
            UserId = reader.GetInt32("user_id"),
            BizType = reader.GetString("biz_type"),
            Title = reader.GetString("title"),
            Summary = reader.IsDBNull(reader.GetOrdinal("summary")) ? null : reader.GetString("summary"),
            Status = reader.GetString("status"),
            LastMessageAt = reader.IsDBNull(reader.GetOrdinal("last_message_at")) ? null : reader.GetDateTime("last_message_at"),
            CreatedAt = reader.GetDateTime("created_at"),
            UpdatedAt = reader.GetDateTime("updated_at")
        };
    }

    private static LlmMessage ReadMessage(MySqlDataReader reader)
    {
        return new LlmMessage
        {
            MessageId = reader.GetInt64("message_id"),
            ConversationId = reader.GetInt64("conversation_id"),
            Role = reader.GetString("role"),
            ContentType = reader.GetString("content_type"),
            Content = reader.GetString("content"),
            ModelName = reader.IsDBNull(reader.GetOrdinal("model_name")) ? null : reader.GetString("model_name"),
            PromptTokens = reader.IsDBNull(reader.GetOrdinal("prompt_tokens")) ? null : reader.GetInt32("prompt_tokens"),
            CompletionTokens = reader.IsDBNull(reader.GetOrdinal("completion_tokens")) ? null : reader.GetInt32("completion_tokens"),
            TotalTokens = reader.IsDBNull(reader.GetOrdinal("total_tokens")) ? null : reader.GetInt32("total_tokens"),
            ResponseMs = reader.IsDBNull(reader.GetOrdinal("response_ms")) ? null : reader.GetInt32("response_ms"),
            SequenceNo = reader.GetInt32("sequence_no"),
            ParentMessageId = reader.IsDBNull(reader.GetOrdinal("parent_message_id")) ? null : reader.GetInt64("parent_message_id"),
            CreatedAt = reader.GetDateTime("created_at")
        };
    }
}
