using backend.Entities;
using MySqlConnector;

namespace backend.Repositories;

public class TagTrendRepository : ITagTrendRepository
{
    private readonly string _connectionString;

    public TagTrendRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task InsertBatchAsync(List<TagTrendRecord> records, CancellationToken cancellationToken)
    {
        if (records.Count == 0) return;

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = @"INSERT INTO tag_trend_history (tag_name, value, quality, unit, timestamp)
                    VALUES (@tagName, @value, @quality, @unit, @timestamp)";

        foreach (var rec in records)
        {
            await using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@tagName", rec.TagName);
            cmd.Parameters.AddWithValue("@value", rec.Value);
            cmd.Parameters.AddWithValue("@quality", rec.Quality);
            cmd.Parameters.AddWithValue("@unit", rec.Unit);
            cmd.Parameters.AddWithValue("@timestamp", rec.Timestamp);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<(List<TagTrendRecord> Items, int TotalCount)> GetHistoryAsync(
        string tagName, DateTime? start, DateTime? end,
        int pageIndex, int pageSize, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var whereClauses = new List<string> { "tag_name = @tagName" };
        var parameters = new List<MySqlParameter> { new MySqlParameter("@tagName", tagName) };

        if (start.HasValue)
        {
            whereClauses.Add("timestamp >= @start");
            parameters.Add(new MySqlParameter("@start", start.Value));
        }
        if (end.HasValue)
        {
            whereClauses.Add("timestamp <= @end");
            parameters.Add(new MySqlParameter("@end", end.Value));
        }

        var whereSql = string.Join(" AND ", whereClauses);

        // 总数
        var countSql = $"SELECT COUNT(*) FROM tag_trend_history WHERE {whereSql}";
        var countCmd = new MySqlCommand(countSql, connection);
        countCmd.Parameters.AddRange(parameters.ToArray());
        var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken));

        // 分页数据
        var offset = (pageIndex - 1) * pageSize;
        var dataSql = $@"SELECT id, tag_name, value, quality, unit, timestamp
                         FROM tag_trend_history
                         WHERE {whereSql}
                         ORDER BY timestamp ASC
                         LIMIT @pageSize OFFSET @offset";
        var dataCmd = new MySqlCommand(dataSql, connection);
        dataCmd.Parameters.AddRange(parameters.ToArray());
        dataCmd.Parameters.Add(new MySqlParameter("@pageSize", pageSize));
        dataCmd.Parameters.Add(new MySqlParameter("@offset", offset));

        var items = new List<TagTrendRecord>();
        await using var reader = await dataCmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new TagTrendRecord
            {
                Id = reader.GetInt64("id"),
                TagName = reader.GetString("tag_name"),
                Value = reader.GetDouble("value"),
                Quality = reader.IsDBNull(reader.GetOrdinal("quality")) ? 0 : reader.GetInt32("quality"),
                Unit = reader.IsDBNull(reader.GetOrdinal("unit")) ? string.Empty : reader.GetString("unit"),
                Timestamp = reader.GetDateTime("timestamp")
            });
        }
        return (items, totalCount);
    }
}