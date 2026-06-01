using backend.Entities;
using MySqlConnector;

namespace backend.Repositories;

public class AlarmRepository : IAlarmRepository
{
    private readonly string _connectionString;

    public AlarmRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<List<AlarmConfig>> GetAlarmConfigsAsync(CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"SELECT alarm_config_id, alarm_code, alarm_name, alarm_type,
                             equipment_id, tag_id, threshold_value, hysteresis,
                             priority, enabled, description
                             FROM alarm_config ORDER BY alarm_config_id";

        var list = new List<AlarmConfig>();
        await using var cmd = new MySqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new AlarmConfig
            {
                AlarmConfigId = reader.GetInt32("alarm_config_id"),
                AlarmCode = reader.GetString("alarm_code"),
                AlarmName = reader.GetString("alarm_name"),
                AlarmType = reader.GetString("alarm_type"),
                EquipmentId = reader.GetInt32("equipment_id"),
                TagId = reader.GetInt32("tag_id"),
                ThresholdValue = reader.GetDecimal("threshold_value"),
                Hysteresis = reader.GetDecimal("hysteresis"),
                Priority = reader.GetString("priority"),
                Enabled = reader.GetBoolean("enabled"),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? string.Empty : reader.GetString("description")
            });
        }
        return list;
    }

    public async Task<List<AlarmHistory>> GetActiveAlarmsAsync(CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"SELECT alarm_id, alarm_config_id, equipment_id, tag_id,
                             alarm_level, alarm_value, threshold_value,
                             start_time, end_time, duration_sec, ack_time, ack_by
                             FROM alarm_history
                             WHERE end_time IS NULL AND ack_time IS NULL
                             ORDER BY start_time DESC";

        var list = new List<AlarmHistory>();
        await using var cmd = new MySqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new AlarmHistory
            {
                AlarmId = reader.GetInt64("alarm_id"),
                AlarmConfigId = reader.GetInt32("alarm_config_id"),
                EquipmentId = reader.GetInt32("equipment_id"),
                TagId = reader.GetInt32("tag_id"),
                AlarmLevel = reader.GetString("alarm_level"),
                AlarmValue = reader.GetDecimal("alarm_value"),
                ThresholdValue = reader.GetDecimal("threshold_value"),
                StartTime = reader.GetDateTime("start_time"),
                EndTime = reader.IsDBNull(reader.GetOrdinal("end_time")) ? null : reader.GetDateTime("end_time"),
                DurationSec = reader.IsDBNull(reader.GetOrdinal("duration_sec")) ? null : reader.GetInt32("duration_sec"),
                AckTime = reader.IsDBNull(reader.GetOrdinal("ack_time")) ? null : reader.GetDateTime("ack_time"),
                AckBy = reader.IsDBNull(reader.GetOrdinal("ack_by")) ? null : reader.GetInt32("ack_by")
            });
        }
        return list;
    }

    public async Task<(List<AlarmHistory> Items, int TotalCount)> GetAlarmHistoryAsync(
        int? equipmentId, int? tagId, DateTime? startTime, DateTime? endTime,
        int pageIndex, int pageSize, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var whereClauses = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (equipmentId.HasValue)
        {
            whereClauses.Add("equipment_id = @equipmentId");
            parameters.Add(new MySqlParameter("@equipmentId", equipmentId.Value));
        }
        if (tagId.HasValue)
        {
            whereClauses.Add("tag_id = @tagId");
            parameters.Add(new MySqlParameter("@tagId", tagId.Value));
        }
        if (startTime.HasValue)
        {
            whereClauses.Add("start_time >= @startTime");
            parameters.Add(new MySqlParameter("@startTime", startTime.Value));
        }
        if (endTime.HasValue)
        {
            whereClauses.Add("start_time <= @endTime");
            parameters.Add(new MySqlParameter("@endTime", endTime.Value));
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        // 总数
        var countSql = $"SELECT COUNT(*) FROM alarm_history {whereSql}";
        var countCmd = new MySqlCommand(countSql, connection);
        countCmd.Parameters.AddRange(parameters.ToArray());
        var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken));

        // 分页
        var offset = (pageIndex - 1) * pageSize;
        var dataSql = $@"SELECT alarm_id, alarm_config_id, equipment_id, tag_id,
                         alarm_level, alarm_value, threshold_value,
                         start_time, end_time, duration_sec, ack_time, ack_by
                         FROM alarm_history
                         {whereSql}
                         ORDER BY start_time DESC
                         LIMIT @pageSize OFFSET @offset";
        var dataCmd = new MySqlCommand(dataSql, connection);
        dataCmd.Parameters.AddRange(parameters.ToArray());
        dataCmd.Parameters.Add(new MySqlParameter("@pageSize", pageSize));
        dataCmd.Parameters.Add(new MySqlParameter("@offset", offset));

        var items = new List<AlarmHistory>();
        await using var reader = await dataCmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new AlarmHistory
            {
                AlarmId = reader.GetInt64("alarm_id"),
                AlarmConfigId = reader.GetInt32("alarm_config_id"),
                EquipmentId = reader.GetInt32("equipment_id"),
                TagId = reader.GetInt32("tag_id"),
                AlarmLevel = reader.GetString("alarm_level"),
                AlarmValue = reader.GetDecimal("alarm_value"),
                ThresholdValue = reader.GetDecimal("threshold_value"),
                StartTime = reader.GetDateTime("start_time"),
                EndTime = reader.IsDBNull(reader.GetOrdinal("end_time")) ? null : reader.GetDateTime("end_time"),
                DurationSec = reader.IsDBNull(reader.GetOrdinal("duration_sec")) ? null : reader.GetInt32("duration_sec"),
                AckTime = reader.IsDBNull(reader.GetOrdinal("ack_time")) ? null : reader.GetDateTime("ack_time"),
                AckBy = reader.IsDBNull(reader.GetOrdinal("ack_by")) ? null : reader.GetInt32("ack_by")
            });
        }
        return (items, totalCount);
    }
}