using backend.Entities;
using MySqlConnector;

namespace backend.Repositories;

public sealed class EquipmentRepository : IEquipmentRepository
{
    private readonly string _connectionString;

    public EquipmentRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string is not configured.");
    }

    public async Task<List<EquipmentType>> GetEquipmentTypesAsync(CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT 
                type_id,
                type_code,
                type_name,
                num AS expected_count
            FROM equipment_type
            ORDER BY type_id;";

        var result = new List<EquipmentType>();

        await using var cmd = new MySqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new EquipmentType
            {
                TypeId = reader.GetInt32("type_id"),
                TypeCode = reader.GetString("type_code"),
                TypeName = reader.GetString("type_name"),
                ExpectedCount = reader.IsDBNull(reader.GetOrdinal("expected_count")) ? 0 : reader.GetInt32("expected_count")
            });
        }

        return result;
    }




public async Task<(List<Equipment> Items, int TotalCount)> GetEquipmentListAsync(
        int? typeId, string? status, string? systemCode, string? keyword,
        int pageIndex, int pageSize, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var whereClauses = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (typeId.HasValue)
        {
            whereClauses.Add("e.type_id = @typeId");
            parameters.Add(new MySqlParameter("@typeId", typeId.Value));
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            whereClauses.Add("e.status = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }
        if (!string.IsNullOrWhiteSpace(systemCode))
        {
            whereClauses.Add("e.system_code = @systemCode");
            parameters.Add(new MySqlParameter("@systemCode", systemCode));
        }
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            whereClauses.Add("(e.equipment_name LIKE @keyword OR e.equipment_code LIKE @keyword)");
            parameters.Add(new MySqlParameter("@keyword", $"%{keyword}%"));
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        // 总数
        var countSql = $"SELECT COUNT(*) FROM equipment e {whereSql}";
        var countCmd = new MySqlCommand(countSql, connection);
        countCmd.Parameters.AddRange(parameters.ToArray());
        var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken));

        // 分页数据
        var offset = (pageIndex - 1) * pageSize;
        var dataSql = $@"
            SELECT e.equipment_id, e.equipment_code, e.equipment_name, e.type_id,
                   e.parent_equipment_id, e.system_code, e.process_area, e.manufacturer,
                   e.model, e.location, e.status, e.is_controllable,
                   e.install_date, e.commissioned_date, e.remark
            FROM equipment e
            {whereSql}
            ORDER BY e.equipment_id
            LIMIT @pageSize OFFSET @offset";

        var dataCmd = new MySqlCommand(dataSql, connection);
        dataCmd.Parameters.AddRange(parameters.ToArray());
        dataCmd.Parameters.Add(new MySqlParameter("@pageSize", pageSize));
        dataCmd.Parameters.Add(new MySqlParameter("@offset", offset));

        var items = new List<Equipment>();
        await using var reader = await dataCmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new Equipment
            {
               
         EquipmentId = reader.GetInt32("equipment_id"),
         EquipmentCode = reader.IsDBNull(reader.GetOrdinal("equipment_code")) ? string.Empty : reader.GetString("equipment_code"),
         EquipmentName = reader.IsDBNull(reader.GetOrdinal("equipment_name")) ? string.Empty : reader.GetString("equipment_name"),
         TypeId = reader.GetInt32("type_id"),
         ParentEquipmentId = reader.IsDBNull(reader.GetOrdinal("parent_equipment_id")) ? null : reader.GetInt32("parent_equipment_id"),
         SystemCode = reader.IsDBNull(reader.GetOrdinal("system_code")) ? string.Empty : reader.GetString("system_code"),
         ProcessArea = reader.IsDBNull(reader.GetOrdinal("process_area")) ? string.Empty : reader.GetString("process_area"),
         Manufacturer = reader.IsDBNull(reader.GetOrdinal("manufacturer")) ? string.Empty : reader.GetString("manufacturer"),
         Model = reader.IsDBNull(reader.GetOrdinal("model")) ? string.Empty : reader.GetString("model"),
         Location = reader.IsDBNull(reader.GetOrdinal("location")) ? string.Empty : reader.GetString("location"),
         Status = reader.IsDBNull(reader.GetOrdinal("status")) ? string.Empty : reader.GetString("status"),
         IsControllable = reader.GetBoolean("is_controllable"),
        InstallDate = reader.IsDBNull(reader.GetOrdinal("install_date")) ? null : reader.GetDateTime("install_date"),
         CommissionedDate = reader.IsDBNull(reader.GetOrdinal("commissioned_date")) ? null : reader.GetDateTime("commissioned_date"),
         Remark = reader.IsDBNull(reader.GetOrdinal("remark")) ? string.Empty : reader.GetString("remark")
            });
        }

        return (items, totalCount);
    }

    public async Task<Equipment?> GetEquipmentByIdAsync(int equipmentId, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT e.equipment_id, e.equipment_code, e.equipment_name, e.type_id,
                   e.parent_equipment_id, e.system_code, e.process_area, e.manufacturer,
                   e.model, e.location, e.status, e.is_controllable,
                   e.install_date, e.commissioned_date, e.remark
            FROM equipment e
            WHERE e.equipment_id = @id";

        var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", equipmentId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new Equipment
            {
         EquipmentId = reader.GetInt32("equipment_id"),
         EquipmentCode = reader.IsDBNull(reader.GetOrdinal("equipment_code")) ? string.Empty : reader.GetString("equipment_code"),
         EquipmentName = reader.IsDBNull(reader.GetOrdinal("equipment_name")) ? string.Empty : reader.GetString("equipment_name"),
         TypeId = reader.GetInt32("type_id"),
         ParentEquipmentId = reader.IsDBNull(reader.GetOrdinal("parent_equipment_id")) ? null : reader.GetInt32("parent_equipment_id"),
         SystemCode = reader.IsDBNull(reader.GetOrdinal("system_code")) ? string.Empty : reader.GetString("system_code"),
         ProcessArea = reader.IsDBNull(reader.GetOrdinal("process_area")) ? string.Empty : reader.GetString("process_area"),
         Manufacturer = reader.IsDBNull(reader.GetOrdinal("manufacturer")) ? string.Empty : reader.GetString("manufacturer"),
         Model = reader.IsDBNull(reader.GetOrdinal("model")) ? string.Empty : reader.GetString("model"),
         Location = reader.IsDBNull(reader.GetOrdinal("location")) ? string.Empty : reader.GetString("location"),
         Status = reader.IsDBNull(reader.GetOrdinal("status")) ? string.Empty : reader.GetString("status"),
         IsControllable = reader.GetBoolean("is_controllable"),
         InstallDate = reader.IsDBNull(reader.GetOrdinal("install_date")) ? null : reader.GetDateTime("install_date"),
         CommissionedDate = reader.IsDBNull(reader.GetOrdinal("commissioned_date")) ? null : reader.GetDateTime("commissioned_date"),
         Remark = reader.IsDBNull(reader.GetOrdinal("remark")) ? string.Empty : reader.GetString("remark")
            };
        }
        return null;
    }

    public async Task<List<EquipmentMonitoringRecord>> GetEquipmentMonitoringRecordsAsync(string? typeCode, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT
                e.equipment_id,
                e.equipment_code,
                e.equipment_name,
                et.type_code,
                et.type_name,
                e.process_area,
                e.location,
                COALESCE(es.run_status, e.status, '') AS run_status,
                e.is_controllable,
                es.flow_rate,
                es.pressure,
                es.current_power,
                es.temperature,
                es.liquid_level,
                es.update_time,
                e.remark
            FROM equipment e
            INNER JOIN equipment_type et ON e.type_id = et.type_id
            LEFT JOIN equipment_status es ON e.equipment_id = es.equipment_id
            WHERE (@typeCode IS NULL OR et.type_code = @typeCode)
            ORDER BY et.type_id, e.process_area, e.equipment_code;";

        var result = new List<EquipmentMonitoringRecord>();
        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@typeCode", string.IsNullOrWhiteSpace(typeCode) ? DBNull.Value : typeCode);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new EquipmentMonitoringRecord
            {
                EquipmentId = reader.GetInt32("equipment_id"),
                EquipmentCode = reader.GetString("equipment_code"),
                EquipmentName = reader.IsDBNull(reader.GetOrdinal("equipment_name")) ? string.Empty : reader.GetString("equipment_name"),
                TypeCode = reader.GetString("type_code"),
                TypeName = reader.GetString("type_name"),
                ProcessArea = reader.IsDBNull(reader.GetOrdinal("process_area")) ? string.Empty : reader.GetString("process_area"),
                Location = reader.IsDBNull(reader.GetOrdinal("location")) ? string.Empty : reader.GetString("location"),
                Status = reader.IsDBNull(reader.GetOrdinal("run_status")) ? string.Empty : reader.GetString("run_status"),
                IsControllable = reader.GetBoolean("is_controllable"),
                FlowRate = reader.IsDBNull(reader.GetOrdinal("flow_rate")) ? null : reader.GetDecimal("flow_rate"),
                Pressure = reader.IsDBNull(reader.GetOrdinal("pressure")) ? null : reader.GetDecimal("pressure"),
                CurrentPower = reader.IsDBNull(reader.GetOrdinal("current_power")) ? null : reader.GetDecimal("current_power"),
                Temperature = reader.IsDBNull(reader.GetOrdinal("temperature")) ? null : reader.GetDecimal("temperature"),
                LiquidLevel = reader.IsDBNull(reader.GetOrdinal("liquid_level")) ? null : reader.GetDecimal("liquid_level"),
                UpdateTime = reader.IsDBNull(reader.GetOrdinal("update_time")) ? null : reader.GetDateTime("update_time"),
                Remark = reader.IsDBNull(reader.GetOrdinal("remark")) ? string.Empty : reader.GetString("remark")
            });
        }

        return result;
    }

    public async Task<EquipmentMonitoringTrendData?> GetEquipmentMonitoringTrendAsync(
        int equipmentId,
        DateTime? start,
        DateTime? end,
        CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string infoSql = @"
            SELECT equipment_id, equipment_code, equipment_name
            FROM equipment
            WHERE equipment_id = @equipmentId;";

        await using var infoCmd = new MySqlCommand(infoSql, connection);
        infoCmd.Parameters.AddWithValue("@equipmentId", equipmentId);
        await using var infoReader = await infoCmd.ExecuteReaderAsync(cancellationToken);
        if (!await infoReader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var result = new EquipmentMonitoringTrendData
        {
            EquipmentId = infoReader.GetInt32("equipment_id"),
            EquipmentCode = infoReader.GetString("equipment_code"),
            EquipmentName = infoReader.IsDBNull(infoReader.GetOrdinal("equipment_name")) ? string.Empty : infoReader.GetString("equipment_name")
        };
        await infoReader.CloseAsync();

        const string historySql = @"
            SELECT
                record_time,
                flow_rate,
                pressure,
                current_power,
                temperature,
                liquid_level,
                run_status
            FROM equipment_status_history
            WHERE equipment_id = @equipmentId
              AND (@start IS NULL OR record_time >= @start)
              AND (@end IS NULL OR record_time <= @end)
            ORDER BY record_time;";

        await using var historyCmd = new MySqlCommand(historySql, connection);
        historyCmd.Parameters.AddWithValue("@equipmentId", equipmentId);
        historyCmd.Parameters.AddWithValue("@start", start.HasValue ? start.Value : DBNull.Value);
        historyCmd.Parameters.AddWithValue("@end", end.HasValue ? end.Value : DBNull.Value);

        await using var historyReader = await historyCmd.ExecuteReaderAsync(cancellationToken);
        while (await historyReader.ReadAsync(cancellationToken))
        {
            result.Items.Add(new EquipmentMonitoringTrendPoint
            {
                Timestamp = historyReader.GetDateTime("record_time"),
                FlowRate = historyReader.IsDBNull(historyReader.GetOrdinal("flow_rate")) ? null : historyReader.GetDecimal("flow_rate"),
                Pressure = historyReader.IsDBNull(historyReader.GetOrdinal("pressure")) ? null : historyReader.GetDecimal("pressure"),
                CurrentPower = historyReader.IsDBNull(historyReader.GetOrdinal("current_power")) ? null : historyReader.GetDecimal("current_power"),
                Temperature = historyReader.IsDBNull(historyReader.GetOrdinal("temperature")) ? null : historyReader.GetDecimal("temperature"),
                LiquidLevel = historyReader.IsDBNull(historyReader.GetOrdinal("liquid_level")) ? null : historyReader.GetDecimal("liquid_level"),
                Status = historyReader.IsDBNull(historyReader.GetOrdinal("run_status")) ? string.Empty : historyReader.GetString("run_status")
            });
        }

        if (result.Items.Count == 0)
        {
            const string currentSql = @"
                SELECT flow_rate, pressure, current_power, temperature, liquid_level, update_time, run_status
                FROM equipment_status
                WHERE equipment_id = @equipmentId;";

            await using var currentCmd = new MySqlCommand(currentSql, connection);
            currentCmd.Parameters.AddWithValue("@equipmentId", equipmentId);
            await using var currentReader = await currentCmd.ExecuteReaderAsync(cancellationToken);
            if (await currentReader.ReadAsync(cancellationToken))
            {
                result.Items.Add(new EquipmentMonitoringTrendPoint
                {
                    Timestamp = currentReader.IsDBNull(currentReader.GetOrdinal("update_time"))
                        ? DateTime.Now
                        : currentReader.GetDateTime("update_time"),
                    FlowRate = currentReader.IsDBNull(currentReader.GetOrdinal("flow_rate")) ? null : currentReader.GetDecimal("flow_rate"),
                    Pressure = currentReader.IsDBNull(currentReader.GetOrdinal("pressure")) ? null : currentReader.GetDecimal("pressure"),
                    CurrentPower = currentReader.IsDBNull(currentReader.GetOrdinal("current_power")) ? null : currentReader.GetDecimal("current_power"),
                    Temperature = currentReader.IsDBNull(currentReader.GetOrdinal("temperature")) ? null : currentReader.GetDecimal("temperature"),
                    LiquidLevel = currentReader.IsDBNull(currentReader.GetOrdinal("liquid_level")) ? null : currentReader.GetDecimal("liquid_level"),
                    Status = currentReader.IsDBNull(currentReader.GetOrdinal("run_status")) ? string.Empty : currentReader.GetString("run_status")
                });
            }
        }

        return result;
    }




    public async Task<Dictionary<int, (int Actual, int Online, int Offline)>> GetEquipmentCountsByTypeAsync(CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT 
                e.type_id,
                COUNT(*) as actual_count,
                SUM(CASE WHEN e.status = '在线' THEN 1 ELSE 0 END) as online_count,
                SUM(CASE WHEN e.status = '离线' THEN 1 ELSE 0 END) as offline_count
            FROM equipment e
            GROUP BY e.type_id;";

        var result = new Dictionary<int, (int Actual, int Online, int Offline)>();

        await using var cmd = new MySqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var typeId = reader.GetInt32("type_id");
            var actual = reader.GetInt32("actual_count");
            var online = reader.IsDBNull(reader.GetOrdinal("online_count")) ? 0 : reader.GetInt32("online_count");
            var offline = reader.IsDBNull(reader.GetOrdinal("offline_count")) ? 0 : reader.GetInt32("offline_count");
            result[typeId] = (actual, online, offline);
        }

        return result;
    }
}
