using backend.Dtos;
using MySqlConnector;
using System.Globalization;
using System.Text.Json;

namespace backend.Repositories;

public class ScheduleQueryRepository : IScheduleQueryRepository
{
    private readonly string _connectionString;

    public ScheduleQueryRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<(List<SchedulePlanDto> Items, int TotalCount)> GetSchedulePlansAsync(
        string? status, int pageIndex, int pageSize, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var whereClauses = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrWhiteSpace(status))
        {
            whereClauses.Add("sp.status = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        // 总数
        var countSql = $"SELECT COUNT(*) FROM schedule_plan sp {whereSql}";
        var countCmd = new MySqlCommand(countSql, connection);
        countCmd.Parameters.AddRange(parameters.ToArray());
        var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken));

        // 分页数据
        var offset = (pageIndex - 1) * pageSize;
        var dataSql = $@"SELECT sp.plan_id, sp.plan_name, sp.plan_code, sp.created_by, sp.created_at, sp.updated_at,
                         sp.status, sic.priority_mode AS calculation_mode,
                         sp.total_output_m3, sp.total_power_kwh, sp.total_cost_cny,
                         sp.optimization_score, sp.fitness_value
                         FROM schedule_plan sp
                         LEFT JOIN schedule_initial_condition sic ON sic.condition_id = sp.condition_id
                         {whereSql}
                         ORDER BY sp.created_at DESC
                         LIMIT @pageSize OFFSET @offset";
        var dataCmd = new MySqlCommand(dataSql, connection);
        dataCmd.Parameters.AddRange(parameters.ToArray());
        dataCmd.Parameters.Add(new MySqlParameter("@pageSize", pageSize));
        dataCmd.Parameters.Add(new MySqlParameter("@offset", offset));

        var items = new List<SchedulePlanDto>();
        await using var reader = await dataCmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new SchedulePlanDto
            {
                PlanId = reader.GetInt32("plan_id"),
                PlanName = reader.IsDBNull(reader.GetOrdinal("plan_name")) ? string.Empty : reader.GetString("plan_name"),
                PlanCode = reader.GetString("plan_code"),
                CreatedBy = reader.GetInt32("created_by"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetDateTime("updated_at"),
                Status = reader.GetString("status"),
                CalculationMode = reader.IsDBNull(reader.GetOrdinal("calculation_mode"))
                    ? null
                    : reader.GetString("calculation_mode"),
                TotalOutputM3 = GetNullableDecimal(reader, "total_output_m3"),
                TotalPowerKwh = GetNullableDecimal(reader, "total_power_kwh"),
                TotalCostCny = GetNullableDecimal(reader, "total_cost_cny"),
                OptimizationScore = GetNullableDecimal(reader, "optimization_score"),
                FitnessValue = GetNullableDecimal(reader, "fitness_value")
            });
        }
        return (items, totalCount);
    }

    public async Task<SchedulePlanDto?> GetPlanByIdAsync(int planId, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"SELECT sp.plan_id, sp.plan_name, sp.plan_code, sp.created_by, sp.created_at, sp.updated_at,
                             sp.status, sic.priority_mode AS calculation_mode,
                             sp.total_output_m3, sp.total_power_kwh, sp.total_cost_cny,
                             sp.optimization_score, sp.fitness_value
                             FROM schedule_plan sp
                             LEFT JOIN schedule_initial_condition sic ON sic.condition_id = sp.condition_id
                             WHERE sp.plan_id = @planId";
        var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@planId", planId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new SchedulePlanDto
            {
                PlanId = reader.GetInt32("plan_id"),
                PlanName = reader.IsDBNull(reader.GetOrdinal("plan_name")) ? string.Empty : reader.GetString("plan_name"),
                PlanCode = reader.GetString("plan_code"),
                CreatedBy = reader.GetInt32("created_by"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetDateTime("updated_at"),
                Status = reader.GetString("status"),
                CalculationMode = reader.IsDBNull(reader.GetOrdinal("calculation_mode"))
                    ? null
                    : reader.GetString("calculation_mode"),
                TotalOutputM3 = GetNullableDecimal(reader, "total_output_m3"),
                TotalPowerKwh = GetNullableDecimal(reader, "total_power_kwh"),
                TotalCostCny = GetNullableDecimal(reader, "total_cost_cny"),
                OptimizationScore = GetNullableDecimal(reader, "optimization_score"),
                FitnessValue = GetNullableDecimal(reader, "fitness_value")
            };
        }
        return null;
    }

    public async Task<List<SchedulePlanDetailDto>> GetDetailsByPlanIdAsync(int planId, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"SELECT detail_id, plan_id, hour, equipment_id, equipment_code,
                             device_group, action, sequence_order, delay_time_sec,
                             target_flow_m3h, target_load_pct, target_pressure_mpa
                             FROM schedule_plan_detail WHERE plan_id = @planId ORDER BY sequence_order";
        var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@planId", planId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var list = new List<SchedulePlanDetailDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new SchedulePlanDetailDto
            {
                DetailId = reader.GetInt32("detail_id"),
                Hour = reader.GetInt32("hour"),
                EquipmentId = reader.IsDBNull(reader.GetOrdinal("equipment_id"))
                    ? null
                    : reader.GetInt32("equipment_id"),
                EquipmentCode = reader.GetString("equipment_code"),
                DeviceGroup = reader.GetString("device_group"),
                Action = reader.GetString("action"),
                SequenceOrder = reader.GetInt32("sequence_order"),
                DelayTimeSec = reader.GetInt32("delay_time_sec"),
                TargetFlowM3h = GetNullableDecimal(reader, "target_flow_m3h"),
                TargetLoadPct = GetNullableDecimal(reader, "target_load_pct"),
                TargetPressureMpa = GetNullableDecimal(reader, "target_pressure_mpa")
            });
        }
        return list;
    }

    private static decimal? GetNullableDecimal(MySqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    }

    public async Task<List<ScheduleReportSheetDto>> GetReportSheetsByPlanIdAsync(int planId, CancellationToken cancellationToken)
    {
        var storedSheets = await GetStoredReportSheetsByPlanIdAsync(planId, cancellationToken);
        if (storedSheets.Count > 0)
        {
            return storedSheets;
        }

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sheets = new List<ScheduleReportSheetDto>
        {
            await BuildHourlySummarySheetAsync(connection, planId, cancellationToken),
            await BuildTankTimeseriesSheetAsync(connection, planId, cancellationToken),
            await BuildBogSheetAsync(connection, planId, cancellationToken),
            await BuildOnOffSheetAsync(connection, planId, "LP_PUMP", "lp_onoff", cancellationToken),
            await BuildOnOffSheetAsync(connection, planId, "HP_PUMP", "hp_onoff", cancellationToken),
            await BuildCombinedSwOnOffSheetAsync(connection, planId, cancellationToken),
            await BuildOnOffSheetAsync(connection, planId, "ORV", "orv_onoff", cancellationToken),
            await BuildCompLevelsSheetAsync(connection, planId, cancellationToken),
            await BuildLinePowerLogSheetAsync(connection, planId, cancellationToken),
            await BuildKpiSummarySheetAsync(connection, planId, cancellationToken)
        };

        return sheets;
    }

    private async Task<List<ScheduleReportSheetDto>> GetStoredReportSheetsByPlanIdAsync(
        int planId,
        CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT report_code, display_name, columns_json, rows_json
            FROM schedule_report_dataset
            WHERE plan_id = @planId
            ORDER BY report_id;";

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@planId", planId);

        var result = new List<ScheduleReportSheetDto>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var columnsJson = reader["columns_json"]?.ToString() ?? "[]";
            var rowsJson = reader["rows_json"]?.ToString() ?? "[]";

            result.Add(new ScheduleReportSheetDto
            {
                SheetName = reader["report_code"]?.ToString() ?? string.Empty,
                DisplayName = reader["display_name"]?.ToString() ?? string.Empty,
                Columns = JsonSerializer.Deserialize<List<string>>(columnsJson) ?? new List<string>(),
                Rows = DeserializeRows(rowsJson)
            });
        }

        return result;
    }

    private static async Task<ScheduleReportSheetDto> BuildHourlySummarySheetAsync(
        MySqlConnection connection,
        int planId,
        CancellationToken cancellationToken)
    {
        const string sql = @"
WITH hours AS (
    SELECT hour FROM hourly_pump_schedule WHERE plan_id = @planId
    UNION
    SELECT hour FROM hourly_auxiliary_schedule WHERE plan_id = @planId
    UNION
    SELECT hour FROM hourly_energy_cost WHERE plan_id = @planId
    UNION
    SELECT hour FROM hourly_tank_status WHERE plan_id = @planId
    UNION
    SELECT hour FROM bog_forecast WHERE plan_id = @planId
    UNION
    SELECT hour FROM compressor_schedule WHERE plan_id = @planId
),
comp_ranked AS (
    SELECT
        hour,
        level_ratio,
        ROW_NUMBER() OVER (PARTITION BY hour ORDER BY comp_id, equipment_id, comp_schedule_id) AS seq_num
    FROM compressor_schedule
    WHERE plan_id = @planId
),
comp AS (
    SELECT
        hour,
        MAX(CASE WHEN seq_num = 1 THEN level_ratio END) AS Comp1_Level,
        MAX(CASE WHEN seq_num = 2 THEN level_ratio END) AS Comp2_Level
    FROM comp_ranked
    GROUP BY hour
)
SELECT
    h.hour AS Hour,
    bf.unload_m3h AS Unload_m3h,
    bf.bog_pred_kgph AS BOG_pred_kgph,
    hp.lp_num AS LP_Num,
    hp.hp_num AS HP_Num,
    hp.hp_flow_total AS Actual_LNG_Output_m3h,
    hp.lp_flow_per_pump AS LP_Flow_perPump_m3h,
    hp.hp_flow_per_pump AS HP_Flow_perPump_m3h,
    hp.ideal_lp_pressure AS Ideal_LP_Pressure_MPa,
    hp.ideal_hp_pressure AS Ideal_HP_Pressure_MPa,
    ha.sw_big_count AS SW_Big,
    ha.sw_small_count AS SW_Small,
    ha.orv_count AS ORV_Count,
    comp.Comp1_Level,
    comp.Comp2_Level,
    he.total_power AS Hourly_Power_kW,
    he.hourly_cost AS Hourly_Cost_CNY,
    ht.tank_level AS Tank_Level_m,
    CASE
        WHEN COALESCE(he.total_power, 0) = 0 THEN NULL
        ELSE ROUND(he.hourly_cost / he.total_power, 4)
    END AS Elec_Price
FROM hours h
LEFT JOIN hourly_pump_schedule hp ON hp.plan_id = @planId AND hp.hour = h.hour
LEFT JOIN hourly_auxiliary_schedule ha ON ha.plan_id = @planId AND ha.hour = h.hour
LEFT JOIN hourly_energy_cost he ON he.plan_id = @planId AND he.hour = h.hour
LEFT JOIN hourly_tank_status ht ON ht.plan_id = @planId AND ht.hour = h.hour
LEFT JOIN bog_forecast bf ON bf.plan_id = @planId AND bf.hour = h.hour
LEFT JOIN comp ON comp.hour = h.hour
ORDER BY h.hour;";

        var columns = new List<string>
        {
            "Hour",
            "Unload_m3h",
            "BOG_pred_kgph",
            "LP_Num",
            "HP_Num",
            "Actual_LNG_Output_m3h",
            "LP_Flow_perPump_m3h",
            "HP_Flow_perPump_m3h",
            "Ideal_LP_Pressure_MPa",
            "Ideal_HP_Pressure_MPa",
            "SW_Big",
            "SW_Small",
            "ORV_Count",
            "Comp1_Level",
            "Comp2_Level",
            "Hourly_Power_kW",
            "Hourly_Cost_CNY",
            "Tank_Level_m",
            "Elec_Price"
        };

        return new ScheduleReportSheetDto
        {
            SheetName = "summary_timeseries",
            DisplayName = "summary_timeseries",
            Columns = columns,
            Rows = await QueryRowsAsync(connection, sql, columns, cancellationToken, ("@planId", planId))
        };
    }

    private static async Task<ScheduleReportSheetDto> BuildTankTimeseriesSheetAsync(
        MySqlConnection connection,
        int planId,
        CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT
    hour AS Hour,
    tank_level AS Tank_Level_m,
    inflow AS Inflow,
    outflow AS Outflow,
    delta_level AS DeltaLevel,
    level_status AS LevelStatus
FROM hourly_tank_status
WHERE plan_id = @planId
ORDER BY hour;";

        var columns = new List<string> { "Hour", "Tank_Level_m", "Inflow", "Outflow", "DeltaLevel", "LevelStatus" };

        return new ScheduleReportSheetDto
        {
            SheetName = "tank_timeseries",
            DisplayName = "tank_timeseries",
            Columns = columns,
            Rows = await QueryRowsAsync(connection, sql, columns, cancellationToken, ("@planId", planId))
        };
    }

    private static async Task<ScheduleReportSheetDto> BuildOnOffSheetAsync(
        MySqlConnection connection,
        int planId,
        string deviceGroup,
        string sheetName,
        CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT hour, device_code, on_off
FROM equipment_onoff_matrix
WHERE plan_id = @planId AND device_group = @deviceGroup
ORDER BY hour, device_code;";

        var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@planId", planId);
        cmd.Parameters.AddWithValue("@deviceGroup", deviceGroup);

        var codes = new List<string>();
        var rowsByHour = new SortedDictionary<int, Dictionary<string, object?>>();

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var hour = reader.GetInt32("hour");
            var codeOrdinal = reader.GetOrdinal("device_code");
            var onOffOrdinal = reader.GetOrdinal("on_off");
            var code = reader.IsDBNull(codeOrdinal) ? string.Empty : reader.GetString("device_code");
            var onOff = reader.IsDBNull(onOffOrdinal) ? 0 : reader.GetInt32("on_off");

            if (string.IsNullOrWhiteSpace(code))
            {
                continue;
            }

            if (!codes.Contains(code))
            {
                codes.Add(code);
            }

            if (!rowsByHour.TryGetValue(hour, out var row))
            {
                row = new Dictionary<string, object?>();
                rowsByHour[hour] = row;
            }

            row[code] = onOff;
        }

        var orderedCodes = codes
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(GetDeviceSortPrefix)
            .ThenBy(GetDeviceSortNumber)
            .ThenBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rows = new List<Dictionary<string, object?>>();
        foreach (var hour in rowsByHour.Keys)
        {
            var source = rowsByHour[hour];
            var row = new Dictionary<string, object?>();
            foreach (var code in orderedCodes)
            {
                row[code] = source.TryGetValue(code, out var value) ? value : 0;
            }

            rows.Add(row);
        }

        return new ScheduleReportSheetDto
        {
            SheetName = sheetName,
            DisplayName = GetSheetDisplayName(sheetName),
            Columns = orderedCodes,
            Rows = rows
        };
    }

    private static async Task<ScheduleReportSheetDto> BuildCombinedSwOnOffSheetAsync(
        MySqlConnection connection,
        int planId,
        CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT hour, device_code, on_off
FROM equipment_onoff_matrix
WHERE plan_id = @planId AND device_group IN ('SW_BIG', 'SW_SMALL')
ORDER BY hour, device_code;";

        var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@planId", planId);

        var codes = new List<string>();
        var rowsByHour = new SortedDictionary<int, Dictionary<string, object?>>();

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var hour = reader.GetInt32("hour");
            var codeOrdinal = reader.GetOrdinal("device_code");
            var onOffOrdinal = reader.GetOrdinal("on_off");
            var code = reader.IsDBNull(codeOrdinal) ? string.Empty : reader.GetString("device_code");
            var onOff = reader.IsDBNull(onOffOrdinal) ? 0 : reader.GetInt32("on_off");

            if (string.IsNullOrWhiteSpace(code))
            {
                continue;
            }

            if (!codes.Contains(code))
            {
                codes.Add(code);
            }

            if (!rowsByHour.TryGetValue(hour, out var row))
            {
                row = new Dictionary<string, object?>();
                rowsByHour[hour] = row;
            }

            row[code] = onOff;
        }

        var orderedCodes = codes
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(GetDeviceSortPrefix)
            .ThenBy(GetDeviceSortNumber)
            .ThenBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rows = new List<Dictionary<string, object?>>();
        foreach (var hour in rowsByHour.Keys)
        {
            var source = rowsByHour[hour];
            var row = new Dictionary<string, object?>();
            foreach (var code in orderedCodes)
            {
                row[code] = source.TryGetValue(code, out var value) ? value : 0;
            }

            rows.Add(row);
        }

        return new ScheduleReportSheetDto
        {
            SheetName = "sw_onoff",
            DisplayName = "sw_onoff",
            Columns = orderedCodes,
            Rows = rows
        };
    }

    private static async Task<ScheduleReportSheetDto> BuildCompLevelsSheetAsync(
        MySqlConnection connection,
        int planId,
        CancellationToken cancellationToken)
    {
        const string sql = @"
WITH ranked AS (
    SELECT
        hour,
        level_ratio,
        ROW_NUMBER() OVER (PARTITION BY hour ORDER BY comp_id, equipment_id, comp_schedule_id) AS seq_num
    FROM compressor_schedule
    WHERE plan_id = @planId
)
SELECT
    MAX(CASE WHEN seq_num = 1 THEN level_ratio END) AS Comp1_Level,
    MAX(CASE WHEN seq_num = 2 THEN level_ratio END) AS Comp2_Level
FROM ranked
GROUP BY hour
ORDER BY hour;";

        var columns = new List<string> { "Comp1_Level", "Comp2_Level" };

        return new ScheduleReportSheetDto
        {
            SheetName = "compressor_levels",
            DisplayName = "compressor_levels",
            Columns = columns,
            Rows = await QueryRowsAsync(connection, sql, columns, cancellationToken, ("@planId", planId))
        };
    }

    private static async Task<ScheduleReportSheetDto> BuildBogSheetAsync(
        MySqlConnection connection,
        int planId,
        CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT
    hour + 1 AS Hour,
    bog_mech_kgph AS BOG_mech_kgph,
    bog_pred_kgph AS BOG_pred_kgph
FROM bog_forecast
WHERE plan_id = @planId
ORDER BY hour;";

        var columns = new List<string> { "Hour", "BOG_mech_kgph", "BOG_pred_kgph" };

        return new ScheduleReportSheetDto
        {
            SheetName = "bog_components",
            DisplayName = "bog_components",
            Columns = columns,
            Rows = await QueryRowsAsync(connection, sql, columns, cancellationToken, ("@planId", planId))
        };
    }

    private static async Task<ScheduleReportSheetDto> BuildLinePowerLogSheetAsync(
        MySqlConnection connection,
        int planId,
        CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT
    h.hour AS Hour,
    he.total_power AS Hourly_Power_kW,
    he.hourly_cost AS Hourly_Cost_CNY,
    bf.unload_m3h AS Unload_m3h,
    CASE
        WHEN COALESCE(he.total_power, 0) = 0 THEN NULL
        ELSE ROUND(he.hourly_cost / he.total_power, 4)
    END AS Elec_Price
FROM (
    SELECT hour FROM hourly_energy_cost WHERE plan_id = @planId
    UNION
    SELECT hour FROM bog_forecast WHERE plan_id = @planId
) h
LEFT JOIN hourly_energy_cost he ON he.plan_id = @planId AND he.hour = h.hour
LEFT JOIN bog_forecast bf ON bf.plan_id = @planId AND bf.hour = h.hour
WHERE MOD(h.hour, 2) = 0
ORDER BY h.hour;";

        var columns = new List<string> { "Hour", "Hourly_Power_kW", "Hourly_Cost_CNY", "Unload_m3h", "Elec_Price" };

        return new ScheduleReportSheetDto
        {
            SheetName = "line_power_log_2h",
            DisplayName = "line_power_log_2h",
            Columns = columns,
            Rows = await QueryRowsAsync(connection, sql, columns, cancellationToken, ("@planId", planId))
        };
    }

    private static async Task<ScheduleReportSheetDto> BuildKpiSummarySheetAsync(
        MySqlConnection connection,
        int planId,
        CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT simulation_start_time, actual_duration_h, total_output_m3, total_power_kwh, total_cost_cny
FROM schedule_plan
WHERE plan_id = @planId
LIMIT 1;";

        var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@planId", planId);

        var rows = new List<Dictionary<string, object?>>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var simulationStartOrdinal = reader.GetOrdinal("simulation_start_time");
            var actualDurationOrdinal = reader.GetOrdinal("actual_duration_h");
            var totalOutputOrdinal = reader.GetOrdinal("total_output_m3");
            var totalPowerOrdinal = reader.GetOrdinal("total_power_kwh");
            var totalCostOrdinal = reader.GetOrdinal("total_cost_cny");

            rows.Add(new Dictionary<string, object?> { ["metric"] = "仿真起点", ["value"] = reader.IsDBNull(simulationStartOrdinal) ? null : reader.GetValue(simulationStartOrdinal) });
            rows.Add(new Dictionary<string, object?> { ["metric"] = "仿真时长(h)", ["value"] = reader.IsDBNull(actualDurationOrdinal) ? null : reader.GetValue(actualDurationOrdinal) });
            rows.Add(new Dictionary<string, object?> { ["metric"] = "仿真期外输总量(m3)", ["value"] = reader.IsDBNull(totalOutputOrdinal) ? null : reader.GetValue(totalOutputOrdinal) });
            rows.Add(new Dictionary<string, object?> { ["metric"] = "估算总能耗(kWh)", ["value"] = reader.IsDBNull(totalPowerOrdinal) ? null : reader.GetValue(totalPowerOrdinal) });
            rows.Add(new Dictionary<string, object?> { ["metric"] = "估算总电费(元)", ["value"] = reader.IsDBNull(totalCostOrdinal) ? null : reader.GetValue(totalCostOrdinal) });
        }

        return new ScheduleReportSheetDto
        {
            SheetName = "kpi_summary",
            DisplayName = "kpi_summary",
            Columns = new List<string> { "metric", "value" },
            Rows = rows
        };
    }

    private static string GetSheetDisplayName(string sheetName) => sheetName switch
    {
        "LP_OnOff" => "低压泵启停",
        "HP_OnOff" => "高压泵启停",
        "SWBig_OnOff" => "大海水泵启停",
        "SWSmall_OnOff" => "小海水泵启停",
        "ORV_OnOff" => "ORV启停",
        "lp_onoff" => "lp_onoff",
        "hp_onoff" => "hp_onoff",
        "sw_onoff" => "sw_onoff",
        "orv_onoff" => "orv_onoff",
        _ => sheetName
    };

    private static async Task<List<Dictionary<string, object?>>> QueryRowsAsync(
        MySqlConnection connection,
        string sql,
        IReadOnlyList<string> columns,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        var cmd = new MySqlCommand(sql, connection);
        foreach (var parameter in parameters)
        {
            cmd.Parameters.AddWithValue(parameter.Name, parameter.Value);
        }

        var rows = new List<Dictionary<string, object?>>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>();
            foreach (var column in columns)
            {
                var ordinal = reader.GetOrdinal(column);
                row[column] = reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);
            }

            rows.Add(row);
        }

        return rows;
    }

    private static List<Dictionary<string, object?>> DeserializeRows(string rowsJson)
    {
        if (string.IsNullOrWhiteSpace(rowsJson))
        {
            return new List<Dictionary<string, object?>>();
        }

        using var document = JsonDocument.Parse(rowsJson);
        var rows = new List<Dictionary<string, object?>>();
        foreach (var rowElement in document.RootElement.EnumerateArray())
        {
            var row = new Dictionary<string, object?>();
            foreach (var property in rowElement.EnumerateObject())
            {
                row[property.Name] = ConvertJsonElement(property.Value);
            }

            rows.Add(row);
        }

        return rows;
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var longValue)
                ? longValue
                : element.TryGetDecimal(out var decimalValue)
                    ? decimalValue
                    : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => element.GetRawText()
        };
    }

    private static string GetDeviceSortPrefix(string code)
    {
        var hashIndex = code.IndexOf('#');
        return hashIndex >= 0 ? code[..hashIndex] : code;
    }

    private static int GetDeviceSortNumber(string code)
    {
        var hashIndex = code.IndexOf('#');
        if (hashIndex < 0 || hashIndex == code.Length - 1)
        {
            return int.MaxValue;
        }

        var suffix = code[(hashIndex + 1)..];
        return int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? number
            : int.MaxValue;
    }
}
