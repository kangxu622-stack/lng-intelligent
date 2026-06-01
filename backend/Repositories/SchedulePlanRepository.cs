using backend.Entities;
using MySqlConnector;
using System.Text.RegularExpressions;

namespace backend.Repositories;

public sealed class SchedulePlanRepository : ISchedulePlanRepository
{
    private readonly string _connectionString;

    public SchedulePlanRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string is not configured.");
    }

    public async Task<bool> DeleteSchedulePlanAsync(
        int planId,
        CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            int? conditionId = null;

            const string getConditionSql = @"
                SELECT condition_id
                FROM schedule_plan
                WHERE plan_id = @planId
                LIMIT 1;";

            await using (var getConditionCmd = new MySqlCommand(getConditionSql, connection, transaction))
            {
                getConditionCmd.Parameters.AddWithValue("@planId", planId);
                var conditionResult = await getConditionCmd.ExecuteScalarAsync(cancellationToken);
                if (conditionResult == null || conditionResult == DBNull.Value)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }

                conditionId = Convert.ToInt32(conditionResult);
            }

            var deleteSqlList = new[]
            {
                "DELETE FROM schedule_plan_detail WHERE plan_id = @planId;",
                "DELETE FROM hourly_pump_schedule WHERE plan_id = @planId;",
                "DELETE FROM hourly_auxiliary_schedule WHERE plan_id = @planId;",
                "DELETE FROM hourly_energy_cost WHERE plan_id = @planId;",
                "DELETE FROM hourly_tank_status WHERE plan_id = @planId;",
                "DELETE FROM bog_forecast WHERE plan_id = @planId;",
                "DELETE FROM equipment_onoff_matrix WHERE plan_id = @planId;",
                "DELETE FROM compressor_schedule WHERE plan_id = @planId;",
                "DELETE FROM schedule_report_dataset WHERE plan_id = @planId;"
            };

            foreach (var sql in deleteSqlList)
            {
                await using var deleteCmd = new MySqlCommand(sql, connection, transaction);
                deleteCmd.Parameters.AddWithValue("@planId", planId);
                await deleteCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            int affected;
            const string deletePlanSql = "DELETE FROM schedule_plan WHERE plan_id = @planId;";
            await using (var deletePlanCmd = new MySqlCommand(deletePlanSql, connection, transaction))
            {
                deletePlanCmd.Parameters.AddWithValue("@planId", planId);
                affected = await deletePlanCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            if (affected <= 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            if (conditionId.HasValue)
            {
                const string deleteConditionSql = @"
                    DELETE FROM schedule_initial_condition
                    WHERE condition_id = @conditionId
                      AND NOT EXISTS (
                          SELECT 1
                          FROM schedule_plan
                          WHERE condition_id = @conditionId
                      );";

                await using var deleteConditionCmd = new MySqlCommand(deleteConditionSql, connection, transaction);
                deleteConditionCmd.Parameters.AddWithValue("@conditionId", conditionId.Value);
                await deleteConditionCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            await ResetScheduleAutoIncrementIfEmptyAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task ResetScheduleAutoIncrementIfEmptyAsync(CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string countSql = "SELECT COUNT(*) FROM schedule_plan;";
        await using var countCmd = new MySqlCommand(countSql, connection);
        var remainingPlans = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken));
        if (remainingPlans > 0)
        {
            return;
        }

        var resetTables = new[]
        {
            "schedule_plan",
            "schedule_initial_condition",
            "schedule_plan_detail",
            "hourly_pump_schedule",
            "hourly_auxiliary_schedule",
            "hourly_energy_cost",
            "hourly_tank_status",
            "bog_forecast",
            "equipment_onoff_matrix",
            "compressor_schedule"
        };

        foreach (var tableName in resetTables)
        {
            var sql = $"ALTER TABLE {tableName} AUTO_INCREMENT = 1;";
            await using var cmd = new MySqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<int> InsertInitialConditionAsync(
        ScheduleInitialCondition condition,
        CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO schedule_initial_condition
            (condition_name, start_time, created_by, target_output_m3, target_pressure_mpa, lp_target_pressure_mpa, hp_target_pressure_mpa, initial_liquid_level_m, demand_duration_h, delta_t_h, priority_mode, constraints_json, remark)
            VALUES
            (@conditionName, @startTime, @createdBy, @targetOutput, @targetPressure, @lpTargetPressure, @hpTargetPressure, @initialLiquidLevel, @duration, @deltaT, @priorityMode, @constraintsJson, @remark);
            SELECT LAST_INSERT_ID();";

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@conditionName", condition.ConditionName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@startTime", condition.StartTime ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@createdBy", condition.CreatedBy);
        cmd.Parameters.AddWithValue("@targetOutput", condition.TargetOutputM3);
        cmd.Parameters.AddWithValue("@targetPressure", condition.TargetPressureMpa);
        cmd.Parameters.AddWithValue("@lpTargetPressure", condition.LpTargetPressureMpa);
        cmd.Parameters.AddWithValue("@hpTargetPressure", condition.HpTargetPressureMpa);
        cmd.Parameters.AddWithValue("@initialLiquidLevel", condition.InitialLiquidLevelM);
        cmd.Parameters.AddWithValue("@duration", condition.DemandDurationH);
        cmd.Parameters.AddWithValue("@deltaT", condition.DeltaTH);
        cmd.Parameters.AddWithValue("@priorityMode", await NormalizePriorityModeAsync(connection, condition.PriorityMode, cancellationToken));
        cmd.Parameters.AddWithValue("@constraintsJson", condition.ConstraintsJson ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@remark", condition.Remark ?? (object)DBNull.Value);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private static async Task<string> NormalizePriorityModeAsync(
        MySqlConnection connection,
        string? priorityMode,
        CancellationToken cancellationToken)
    {
        var input = (priorityMode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            input = "middle";
        }

        var enumValues = await GetPriorityModeEnumValuesAsync(connection, cancellationToken);
        if (enumValues.Count == 0)
        {
            return input;
        }

        var directMatch = enumValues.FirstOrDefault(value =>
            string.Equals(value, input, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(directMatch))
        {
            return directMatch;
        }

        var bucket = GetPriorityBucket(input);
        var candidates = bucket switch
        {
            "speed" => new[] { "low", "speed", "efficiency", "high_efficiency", "高效", "效率优先", "计算速度优先" },
            "quality" => new[] { "high", "quality", "energy_saving", "节能", "结果质量优先", "质量优先" },
            _ => new[] { "middle", "balanced", "balance", "平衡", "平衡模式" }
        };

        foreach (var candidate in candidates)
        {
            var matched = enumValues.FirstOrDefault(value =>
                string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(matched))
            {
                return matched;
            }
        }

        return bucket switch
        {
            "speed" => enumValues[0],
            "quality" => enumValues[^1],
            _ => enumValues[Math.Min(1, enumValues.Count - 1)]
        };
    }

    private static async Task<List<string>> GetPriorityModeEnumValuesAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT COLUMN_TYPE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'schedule_initial_condition'
              AND COLUMN_NAME = 'priority_mode'
            LIMIT 1;";

        await using var cmd = new MySqlCommand(sql, connection);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        var columnType = result as string;
        if (string.IsNullOrWhiteSpace(columnType))
        {
            return new List<string>();
        }

        return Regex.Matches(columnType, @"'([^']*)'")
            .Select(match => match.Groups[1].Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    private static string GetPriorityBucket(string input)
    {
        var normalized = input.Trim().ToLowerInvariant();
        return normalized switch
        {
            "low" or "speed" or "efficiency" or "high_efficiency" or "高效" or "效率优先" or "计算速度优先" => "speed",
            "high" or "quality" or "energy_saving" or "节能" or "结果质量优先" or "质量优先" => "quality",
            _ => "balanced"
        };
    }

    public async Task<int> InsertSchedulePlanAsync(
        SchedulePlan plan,
        CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO schedule_plan
            (plan_code, plan_name, simulation_start_time, condition_id, created_by, status,
             total_output_m3, total_power_kwh, total_cost_cny, optimization_score,
             fitness_value, total_penalty, actual_duration_h,
             max_lp_online, max_hp_online, total_startup_count,
             approval_comment)
            VALUES
            (@planCode, @planName, @simulationStartTime, @conditionId, @createdBy, @status,
             @totalOutput, @totalPower, @totalCost, @optimizationScore,
             @fitnessValue, @totalPenalty, @actualDuration,
             @maxLpOnline, @maxHpOnline, @totalStartupCount,
             @approvalComment);
            SELECT LAST_INSERT_ID();";

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@planCode", plan.PlanCode);
        cmd.Parameters.AddWithValue("@planName", plan.PlanName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@simulationStartTime", plan.SimulationStartTime ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@conditionId", plan.ConditionId);
        cmd.Parameters.AddWithValue("@createdBy", plan.CreatedBy);
        cmd.Parameters.AddWithValue("@status", plan.Status);
        cmd.Parameters.AddWithValue("@totalOutput", plan.TotalOutputM3);
        cmd.Parameters.AddWithValue("@totalPower", plan.TotalPowerKwh);
        cmd.Parameters.AddWithValue("@totalCost", plan.TotalCostCny);
        cmd.Parameters.AddWithValue("@optimizationScore", plan.OptimizationScore);
        cmd.Parameters.AddWithValue("@fitnessValue", plan.FitnessValue);
        cmd.Parameters.AddWithValue("@totalPenalty", plan.TotalPenalty);
        cmd.Parameters.AddWithValue("@actualDuration", plan.ActualDurationH);
        cmd.Parameters.AddWithValue("@maxLpOnline", plan.MaxLpOnline);
        cmd.Parameters.AddWithValue("@maxHpOnline", plan.MaxHpOnline);
        cmd.Parameters.AddWithValue("@totalStartupCount", plan.TotalStartupCount);
        cmd.Parameters.AddWithValue("@approvalComment", plan.ApprovalComment ?? (object)DBNull.Value);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task UpsertHourlyPumpScheduleAsync(
        int planId,
        int hour,
        HourlyPumpSchedule data,
        CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO hourly_pump_schedule
            (plan_id, hour, lp_num, lp_flow_per_pump, lp_flow_total, lp_power, ideal_lp_pressure,
             hp_num, hp_flow_per_pump, hp_flow_total, hp_power, ideal_hp_pressure)
            VALUES
            (@planId, @hour, @lpNum, @lpFlowPerPump, @lpFlowTotal, @lpPower, @lpPressure,
             @hpNum, @hpFlowPerPump, @hpFlowTotal, @hpPower, @hpPressure)
            ON DUPLICATE KEY UPDATE
            lp_num = VALUES(lp_num), lp_flow_per_pump = VALUES(lp_flow_per_pump), lp_flow_total = VALUES(lp_flow_total),
            lp_power = VALUES(lp_power), ideal_lp_pressure = VALUES(ideal_lp_pressure),
            hp_num = VALUES(hp_num), hp_flow_per_pump = VALUES(hp_flow_per_pump), hp_flow_total = VALUES(hp_flow_total),
            hp_power = VALUES(hp_power), ideal_hp_pressure = VALUES(ideal_hp_pressure);";

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@planId", planId);
        cmd.Parameters.AddWithValue("@hour", hour);
        cmd.Parameters.AddWithValue("@lpNum", data.LpNum);
        cmd.Parameters.AddWithValue("@lpFlowPerPump", data.LpFlowPerPump);
        cmd.Parameters.AddWithValue("@lpFlowTotal", data.LpFlowTotal);
        cmd.Parameters.AddWithValue("@lpPower", data.LpPower);
        cmd.Parameters.AddWithValue("@lpPressure", data.IdealLpPressure);
        cmd.Parameters.AddWithValue("@hpNum", data.HpNum);
        cmd.Parameters.AddWithValue("@hpFlowPerPump", data.HpFlowPerPump);
        cmd.Parameters.AddWithValue("@hpFlowTotal", data.HpFlowTotal);
        cmd.Parameters.AddWithValue("@hpPower", data.HpPower);
        cmd.Parameters.AddWithValue("@hpPressure", data.IdealHpPressure);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpsertHourlyAuxiliaryScheduleAsync(
        int planId,
        int hour,
        HourlyAuxiliarySchedule data,
        CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO hourly_auxiliary_schedule
            (plan_id, hour, sw_big_count, sw_small_count, sw_flow_total, sw_power, orv_count, orv_heat_duty)
            VALUES
            (@planId, @hour, @swBigCount, @swSmallCount, @swFlowTotal, @swPower, @orvCount, @orvHeatDuty)
            ON DUPLICATE KEY UPDATE
            sw_big_count = VALUES(sw_big_count), sw_small_count = VALUES(sw_small_count),
            sw_flow_total = VALUES(sw_flow_total), sw_power = VALUES(sw_power),
            orv_count = VALUES(orv_count), orv_heat_duty = VALUES(orv_heat_duty);";

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@planId", planId);
        cmd.Parameters.AddWithValue("@hour", hour);
        cmd.Parameters.AddWithValue("@swBigCount", data.SwBigCount);
        cmd.Parameters.AddWithValue("@swSmallCount", data.SwSmallCount);
        cmd.Parameters.AddWithValue("@swFlowTotal", data.SwFlowTotal);
        cmd.Parameters.AddWithValue("@swPower", data.SwPower);
        cmd.Parameters.AddWithValue("@orvCount", data.OrvCount);
        cmd.Parameters.AddWithValue("@orvHeatDuty", data.OrvHeatDuty);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpsertHourlyEnergyCostAsync(
        int planId,
        int hour,
        HourlyEnergyCost data,
        CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO hourly_energy_cost
            (plan_id, hour, lp_power, hp_power, sw_power, comp_power, rc_power, total_power, hourly_cost)
            VALUES
            (@planId, @hour, @lpPower, @hpPower, @swPower, @compPower, @rcPower, @totalPower, @hourlyCost)
            ON DUPLICATE KEY UPDATE
            lp_power = VALUES(lp_power), hp_power = VALUES(hp_power), sw_power = VALUES(sw_power),
            comp_power = VALUES(comp_power), rc_power = VALUES(rc_power),
            total_power = VALUES(total_power), hourly_cost = VALUES(hourly_cost);";

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@planId", planId);
        cmd.Parameters.AddWithValue("@hour", hour);
        cmd.Parameters.AddWithValue("@lpPower", data.LpPower);
        cmd.Parameters.AddWithValue("@hpPower", data.HpPower);
        cmd.Parameters.AddWithValue("@swPower", data.SwPower);
        cmd.Parameters.AddWithValue("@compPower", data.CompPower);
        cmd.Parameters.AddWithValue("@rcPower", data.RcPower);
        cmd.Parameters.AddWithValue("@totalPower", data.TotalPower);
        cmd.Parameters.AddWithValue("@hourlyCost", data.HourlyCost);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpsertHourlyTankStatusAsync(
        int planId,
        int hour,
        HourlyTankStatus data,
        CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO hourly_tank_status
            (plan_id, hour, tank_level, inflow, outflow, delta_level, level_status)
            VALUES
            (@planId, @hour, @tankLevel, @inflow, @outflow, @deltaLevel, @levelStatus)
            ON DUPLICATE KEY UPDATE
            tank_level = VALUES(tank_level), inflow = VALUES(inflow), outflow = VALUES(outflow),
            delta_level = VALUES(delta_level), level_status = VALUES(level_status);";

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@planId", planId);
        cmd.Parameters.AddWithValue("@hour", hour);
        cmd.Parameters.AddWithValue("@tankLevel", data.TankLevel);
        cmd.Parameters.AddWithValue("@inflow", data.Inflow);
        cmd.Parameters.AddWithValue("@outflow", data.Outflow);
        cmd.Parameters.AddWithValue("@deltaLevel", data.DeltaLevel);
        cmd.Parameters.AddWithValue("@levelStatus", data.LevelStatus);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpsertBogForecastAsync(
        int planId,
        int hour,
        BogForecast data,
        CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO bog_forecast
            (plan_id, hour, ambient_temp_c, atm_pressure_kpa, unload_m3h, bog_mech_kgph, bog_residual_kgph, bog_pred_kgph, condensation_capacity_kgph)
            VALUES
            (@planId, @hour, @temp, @pressure, @unload, @bogMech, @bogResidual, @bogPred, @condensationCapacity)
            ON DUPLICATE KEY UPDATE
            ambient_temp_c = VALUES(ambient_temp_c), atm_pressure_kpa = VALUES(atm_pressure_kpa),
            unload_m3h = VALUES(unload_m3h), bog_mech_kgph = VALUES(bog_mech_kgph),
            bog_residual_kgph = VALUES(bog_residual_kgph), bog_pred_kgph = VALUES(bog_pred_kgph),
            condensation_capacity_kgph = VALUES(condensation_capacity_kgph);";

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@planId", planId);
        cmd.Parameters.AddWithValue("@hour", hour);
        cmd.Parameters.AddWithValue("@temp", data.AmbientTempC);
        cmd.Parameters.AddWithValue("@pressure", data.AtmPressureKpa);
        cmd.Parameters.AddWithValue("@unload", data.UnloadM3h);
        cmd.Parameters.AddWithValue("@bogMech", data.BogMechKgph);
        cmd.Parameters.AddWithValue("@bogResidual", data.BogResidualKgph);
        cmd.Parameters.AddWithValue("@bogPred", data.BogPredKgph);
        cmd.Parameters.AddWithValue("@condensationCapacity", data.CondensationCapacityKgph);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpsertEquipmentOnOffMatrixAsync(
        int planId,
        int hour,
        EquipmentOnOffMatrix data,
        CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO equipment_onoff_matrix
            (plan_id, hour, equipment_id, device_code, device_group, on_off, flow_m3h, power_kw)
            VALUES
            (@planId, @hour, @equipmentId, @deviceCode, @deviceGroup, @onOff, @flow, @power)
            ON DUPLICATE KEY UPDATE
            on_off = VALUES(on_off), flow_m3h = VALUES(flow_m3h), power_kw = VALUES(power_kw);";

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@planId", planId);
        cmd.Parameters.AddWithValue("@hour", hour);
        cmd.Parameters.AddWithValue("@equipmentId", data.EquipmentId.HasValue ? data.EquipmentId.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@deviceCode", data.DeviceCode);
        cmd.Parameters.AddWithValue("@deviceGroup", data.DeviceGroup);
        cmd.Parameters.AddWithValue("@onOff", data.OnOff);
        cmd.Parameters.AddWithValue("@flow", data.FlowM3h);
        cmd.Parameters.AddWithValue("@power", data.PowerKw);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpsertSchedulePlanDetailAsync(
        int planId,
        SchedulePlanDetail data,
        CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO schedule_plan_detail
            (plan_id, hour, equipment_id, equipment_code, device_group, action, sequence_order, delay_time_sec,
             target_flow_m3h, target_load_pct, target_pressure_mpa)
            VALUES
            (@planId, @hour, @equipmentId, @equipmentCode, @deviceGroup, @action, @sequenceOrder, @delayTimeSec,
             @targetFlow, @targetLoadPct, @targetPressure)
            ON DUPLICATE KEY UPDATE
            equipment_code = VALUES(equipment_code),
            device_group = VALUES(device_group),
            action = VALUES(action),
            delay_time_sec = VALUES(delay_time_sec),
            target_flow_m3h = VALUES(target_flow_m3h),
            target_load_pct = VALUES(target_load_pct),
            target_pressure_mpa = VALUES(target_pressure_mpa);";

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@planId", planId);
        cmd.Parameters.AddWithValue("@hour", data.Hour);
        cmd.Parameters.AddWithValue("@equipmentId", data.EquipmentId.HasValue ? data.EquipmentId.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@equipmentCode", data.EquipmentCode);
        cmd.Parameters.AddWithValue("@deviceGroup", data.DeviceGroup);
        cmd.Parameters.AddWithValue("@action", data.Action);
        cmd.Parameters.AddWithValue("@sequenceOrder", data.SequenceOrder);
        cmd.Parameters.AddWithValue("@delayTimeSec", data.DelayTimeSec);
        cmd.Parameters.AddWithValue("@targetFlow", data.TargetFlowM3h);
        cmd.Parameters.AddWithValue("@targetLoadPct", data.TargetLoadPct);
        cmd.Parameters.AddWithValue("@targetPressure", data.TargetPressureMpa);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpsertCompressorScheduleAsync(
        int planId,
        int hour,
        CompressorSchedule data,
        CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO compressor_schedule
            (plan_id, hour, comp_id, equipment_id, level_ratio, power_kw)
            VALUES
            (@planId, @hour, @compId, @equipmentId, @levelRatio, @powerKw)
            ON DUPLICATE KEY UPDATE
            equipment_id = VALUES(equipment_id),
            level_ratio = VALUES(level_ratio),
            power_kw = VALUES(power_kw);";

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@planId", planId);
        cmd.Parameters.AddWithValue("@hour", hour);
        cmd.Parameters.AddWithValue("@compId", data.CompId);
        cmd.Parameters.AddWithValue("@equipmentId", data.EquipmentId.HasValue ? data.EquipmentId.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@levelRatio", data.LevelRatio);
        cmd.Parameters.AddWithValue("@powerKw", data.PowerKw.HasValue ? data.PowerKw.Value : (object)DBNull.Value);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpsertReportDatasetAsync(
        int planId,
        string reportCode,
        string displayName,
        string columnsJson,
        string rowsJson,
        CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO schedule_report_dataset
            (plan_id, report_code, display_name, columns_json, rows_json)
            VALUES
            (@planId, @reportCode, @displayName, @columnsJson, @rowsJson)
            ON DUPLICATE KEY UPDATE
            display_name = VALUES(display_name),
            columns_json = VALUES(columns_json),
            rows_json = VALUES(rows_json),
            updated_at = CURRENT_TIMESTAMP;";

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@planId", planId);
        cmd.Parameters.AddWithValue("@reportCode", reportCode);
        cmd.Parameters.AddWithValue("@displayName", displayName);
        cmd.Parameters.AddWithValue("@columnsJson", columnsJson);
        cmd.Parameters.AddWithValue("@rowsJson", rowsJson);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
