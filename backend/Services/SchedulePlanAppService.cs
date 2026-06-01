using backend.Dtos.Simulation;
using backend.Entities;
using backend.Repositories;
using System.Text.Json;

namespace backend.Services;

public sealed class SchedulePlanAppService : ISchedulePlanAppService
{
    private const double CurrentOpcInitialLiquidLevelM = 20.0;

    private readonly ISchedulePlanRepository _schedulePlanRepository;
    private readonly IScheduleQueryRepository _scheduleQueryRepository;
    private readonly ILogger<SchedulePlanAppService> _logger;

    public SchedulePlanAppService(
        ISchedulePlanRepository schedulePlanRepository,
        IScheduleQueryRepository scheduleQueryRepository,
        ILogger<SchedulePlanAppService> logger)
    {
        _schedulePlanRepository = schedulePlanRepository;
        _scheduleQueryRepository = scheduleQueryRepository;
        _logger = logger;
    }

    public async Task<SaveScheduleResult> SaveAsync(SaveScheduleInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.CreatedBy <= 0)
        {
            throw new InvalidOperationException("CreatedBy (user ID) is required.");
        }

        if (string.IsNullOrWhiteSpace(input.PlanName))
        {
            throw new InvalidOperationException("PlanName is required.");
        }

        if (input.CalculateInput == null)
        {
            throw new InvalidOperationException("CalculateInput is required.");
        }

        if (input.SimulationResult == null)
        {
            throw new InvalidOperationException("SimulationResult is required.");
        }

        try
        {
            var condition = new ScheduleInitialCondition
            {
                ConditionName = input.CalculateInput.ConditionName ?? $"Condition_{DateTime.Now:yyyyMMddHHmmss}",
                StartTime = ParseStartTime(input.CalculateInput.StartTime),
                CreatedBy = input.CreatedBy,
                TargetOutputM3 = input.CalculateInput.TargetOutputM3 ?? 60000,
                TargetPressureMpa = input.CalculateInput.LpTargetPressure ?? 1.2,
                LpTargetPressureMpa = input.CalculateInput.LpTargetPressure ?? 1.2,
                HpTargetPressureMpa = input.CalculateInput.HpTargetPressure ?? 12.0,
                InitialLiquidLevelM = input.CalculateInput.InitialLiquidLevel ?? GetCurrentInitialLiquidLevelM(),
                DemandDurationH = 24,
                DeltaTH = 1.0,
                PriorityMode = NormalizePriorityModeInput(input.CalculateInput.CalculationMode),
                ConstraintsJson = input.ConstraintsJson?.ToString(),
                Remark = input.CalculateInput.Remark
            };

            int conditionId = await _schedulePlanRepository.InsertInitialConditionAsync(condition, cancellationToken);

            var planData = ExtractPlanData(input.SimulationResult);
            var plan = new SchedulePlan
            {
                PlanCode = GeneratePlanCode(),
                PlanName = input.PlanName,
                SimulationStartTime = ParseStartTime(input.CalculateInput.StartTime),
                ConditionId = conditionId,
                CreatedBy = input.CreatedBy,
                Status = "草稿",
                TotalOutputM3 = (decimal)planData.TotalOutput,
                TotalPowerKwh = (decimal)planData.TotalPower,
                TotalCostCny = (decimal)planData.TotalCost,
                OptimizationScore = (decimal)planData.OptimizationScore,
                FitnessValue = (decimal)planData.FitnessValue,
                TotalPenalty = (decimal)planData.TotalPenalty,
                ActualDurationH = (decimal)planData.ActualDuration,
                MaxLpOnline = planData.MaxLpOnline,
                MaxHpOnline = planData.MaxHpOnline,
                TotalStartupCount = planData.TotalStartupCount,
                ApprovalComment = input.ApprovalComment ?? string.Empty
            };

            int planId = await _schedulePlanRepository.InsertSchedulePlanAsync(plan, cancellationToken);

            await SaveHourlySchedulesAsync(planId, input.SimulationResult.Hourly, cancellationToken);
            await SaveBogForecastsAsync(planId, input.SimulationResult.Bog, cancellationToken);
            await SaveEquipmentOnOffMatrixAsync(planId, input.SimulationResult.Hourly, input.SimulationResult.Unitized, cancellationToken);
            await SaveCompressorSchedulesAsync(planId, input.SimulationResult.Hourly, input.SimulationResult.Unitized, cancellationToken);
            await SaveScheduleDetailsAsync(planId, input.SimulationResult.Hourly, input.SimulationResult.Unitized, cancellationToken);
            await SaveReportDatasetsAsync(planId, cancellationToken);

            return new SaveScheduleResult
            {
                ConditionId = conditionId,
                PlanId = planId,
                PlanCode = plan.PlanCode,
                Message = "淇濆瓨鎴愬姛"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save simulation result");
            throw new InvalidOperationException($"Failed to save simulation result: {ex.Message}", ex);
        }
    }

    private static DateTime? ParseStartTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParse(value.Trim(), out var parsed) ? parsed : null;
    }

    private static double GetCurrentInitialLiquidLevelM()
    {
        return CurrentOpcInitialLiquidLevelM;
    }

    private static (double TotalOutput, double TotalPower, double TotalCost, double OptimizationScore, double FitnessValue,
        double TotalPenalty, double ActualDuration, int MaxLpOnline, int MaxHpOnline, int TotalStartupCount)
        ExtractPlanData(ScheduleCalculationResult result)
    {
        var summary = result.Summary;
        var hourly = result.Hourly;

        double totalOutput = TryGetDouble(summary, "TotalOutputM3", "totalOutputM3", "total_output_m3");
        double totalPower = TryGetDouble(summary, "TotalPowerKwh", "totalPowerKwh", "total_power_kwh");
        double totalCost = TryGetDouble(summary, "TotalCostYuan", "TotalCostCny", "totalCostCny", "estimated_total_electric_cost");
        double fitnessValue = TryGetDouble(summary, "FitnessValue", "fitnessValue", "fitness_value");
        double optimizationScore = TryGetDouble(summary, "OptimizationScore", "optimizationScore", "optimization_score");
        double totalPenalty = TryGetDouble(summary, "TotalPenalty", "totalPenalty", "total_penalty");
        double actualDuration = TryGetDouble(summary, "ActualDurationH", "actualDurationH", "actual_duration_h");
        int maxLpOnline = TryGetInt(summary, "MaxLpOnline", "maxLpOnline", "max_lp_online");
        int maxHpOnline = TryGetInt(summary, "MaxHpOnline", "maxHpOnline", "max_hp_online");
        int totalStartupCount = TryGetInt(summary, "TotalStartupCount", "totalStartupCount", "total_startup_count");

        if (hourly.ValueKind == JsonValueKind.Array)
        {
            double sumOutput = 0;
            double sumPower = 0;
            double sumCost = 0;
            int hourlyCount = 0;
            int hourlyMaxLp = 0;
            int hourlyMaxHp = 0;

            foreach (var hourData in hourly.EnumerateArray())
            {
                if (hourData.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                sumOutput += TryGetDouble(hourData, "Actual_LNG_Output_m3h", "HpFlowTotal");
                sumPower += TryGetDouble(hourData, "Hourly_Power_kW", "TotalPower");
                sumCost += TryGetDouble(hourData, "Hourly_Cost_CNY", "HourlyCost");
                hourlyMaxLp = Math.Max(hourlyMaxLp, TryGetInt(hourData, "LP_Num", "LpNum"));
                hourlyMaxHp = Math.Max(hourlyMaxHp, TryGetInt(hourData, "HP_Num", "HpNum"));
                hourlyCount++;
            }

            if (totalOutput <= 0) totalOutput = sumOutput;
            if (totalPower <= 0) totalPower = sumPower;
            if (totalCost <= 0) totalCost = sumCost;
            if (actualDuration <= 0) actualDuration = hourlyCount;
            if (maxLpOnline <= 0) maxLpOnline = hourlyMaxLp;
            if (maxHpOnline <= 0) maxHpOnline = hourlyMaxHp;
        }

        if (optimizationScore <= 0 && result.RunHistory is { Count: > 0 })
        {
            optimizationScore = result.RunHistory[^1];
        }

        if (fitnessValue <= 0 && optimizationScore > 0)
        {
            fitnessValue = optimizationScore;
        }

        if (totalStartupCount <= 0)
        {
            totalStartupCount = CountStartups(result.Unitized);
        }

        return (
            totalOutput,
            totalPower,
            totalCost,
            optimizationScore,
            fitnessValue,
            totalPenalty,
            actualDuration,
            maxLpOnline,
            maxHpOnline,
            totalStartupCount);
    }

    private async Task SaveHourlySchedulesAsync(int planId, JsonElement hourly, CancellationToken cancellationToken)
    {
        if (hourly.ValueKind != JsonValueKind.Array) return;

        int hour = 0;
        foreach (var hourData in hourly.EnumerateArray())
        {
            if (hourData.ValueKind == JsonValueKind.Object)
            {
                await SaveHourlyPumpScheduleAsync(planId, hour, hourData, cancellationToken);
                await SaveHourlyAuxiliaryScheduleAsync(planId, hour, hourData, cancellationToken);
                await SaveHourlyEnergyCostAsync(planId, hour, hourData, cancellationToken);
                await SaveHourlyTankStatusAsync(planId, hour, hourData, cancellationToken);
            }

            hour++;
        }
    }

    private async Task SaveHourlyPumpScheduleAsync(int planId, int hour, JsonElement data, CancellationToken cancellationToken)
    {
        var schedule = new HourlyPumpSchedule
        {
            LpNum = GetIntProperty(data, "LP_Num", "LpNum"),
            LpFlowPerPump = GetDoubleProperty(data, "LP_Flow_perPump_m3h", "LpFlowPerPump"),
            LpFlowTotal = GetDoubleProperty(data, "LpFlowTotal"),
            LpPower = GetDoubleProperty(data, "LpPower"),
            IdealLpPressure = GetDoubleProperty(data, "Ideal_LP_Pressure_MPa", "IdealLpPressure"),
            HpNum = GetIntProperty(data, "HP_Num", "HpNum"),
            HpFlowPerPump = GetDoubleProperty(data, "HP_Flow_perPump_m3h", "HpFlowPerPump"),
            HpFlowTotal = GetDoubleProperty(data, "Actual_LNG_Output_m3h", "HpFlowTotal"),
            HpPower = GetDoubleProperty(data, "HpPower"),
            IdealHpPressure = GetDoubleProperty(data, "Ideal_HP_Pressure_MPa", "IdealHpPressure")
        };

        await _schedulePlanRepository.UpsertHourlyPumpScheduleAsync(planId, hour, schedule, cancellationToken);
    }

    private async Task SaveHourlyAuxiliaryScheduleAsync(int planId, int hour, JsonElement data, CancellationToken cancellationToken)
    {
        var schedule = new HourlyAuxiliarySchedule
        {
            SwBigCount = GetIntProperty(data, "SW_Big", "SwBigCount"),
            SwSmallCount = GetIntProperty(data, "SW_Small", "SwSmallCount"),
            SwFlowTotal = GetDoubleProperty(data, "SwFlowTotal"),
            SwPower = GetDoubleProperty(data, "SwPower"),
            OrvCount = GetIntProperty(data, "ORV_Count", "OrvCount"),
            OrvHeatDuty = GetDoubleProperty(data, "OrvHeatDuty")
        };

        await _schedulePlanRepository.UpsertHourlyAuxiliaryScheduleAsync(planId, hour, schedule, cancellationToken);
    }

    private async Task SaveHourlyEnergyCostAsync(int planId, int hour, JsonElement data, CancellationToken cancellationToken)
    {
        var cost = new HourlyEnergyCost
        {
            LpPower = GetDoubleProperty(data, "LpPower"),
            HpPower = GetDoubleProperty(data, "HpPower"),
            SwPower = GetDoubleProperty(data, "SwPower"),
            CompPower = GetDoubleProperty(data, "CompPower"),
            RcPower = GetDoubleProperty(data, "RcPower"),
            TotalPower = GetDoubleProperty(data, "Hourly_Power_kW", "TotalPower"),
            HourlyCost = GetDoubleProperty(data, "Hourly_Cost_CNY", "HourlyCost")
        };

        await _schedulePlanRepository.UpsertHourlyEnergyCostAsync(planId, hour, cost, cancellationToken);
    }

    private async Task SaveHourlyTankStatusAsync(int planId, int hour, JsonElement data, CancellationToken cancellationToken)
    {
        var status = new HourlyTankStatus
        {
            TankLevel = GetDoubleProperty(data, "Tank_Level_m", "TankLevel"),
            Inflow = GetDoubleProperty(data, "Inflow"),
            Outflow = GetDoubleProperty(data, "Outflow"),
            DeltaLevel = GetDoubleProperty(data, "DeltaLevel"),
            LevelStatus = NormalizeLevelStatus(GetStringProperty(data, "LevelStatus"))
        };

        await _schedulePlanRepository.UpsertHourlyTankStatusAsync(planId, hour, status, cancellationToken);
    }

    private async Task SaveBogForecastsAsync(int planId, JsonElement bog, CancellationToken cancellationToken)
    {
        if (bog.ValueKind != JsonValueKind.Object) return;

        var mech = GetNumberArray(bog, "bog_mech_kgph", "BogMechKgph");
        var pred = GetNumberArray(bog, "bog_pred_kgph", "BogPredKgph");
        var hours = GetNumberArray(bog, "hours", "Hours");
        int count = Math.Max(mech.Count, Math.Max(pred.Count, hours.Count));

        for (int i = 0; i < count; i++)
        {
            int hour = hours.Count > i ? (int)hours[i] : i + 1;
            var forecast = new BogForecast
            {
                AmbientTempC = 0,
                AtmPressureKpa = 0,
                UnloadM3h = 0,
                BogMechKgph = mech.Count > i ? mech[i] : 0,
                BogResidualKgph = 0,
                BogPredKgph = pred.Count > i ? pred[i] : 0,
                CondensationCapacityKgph = 0
            };

            await _schedulePlanRepository.UpsertBogForecastAsync(planId, Math.Max(hour - 1, 0), forecast, cancellationToken);
        }
    }

    private async Task SaveEquipmentOnOffMatrixAsync(
        int planId,
        JsonElement hourly,
        JsonElement unitized,
        CancellationToken cancellationToken)
    {
        if (unitized.ValueKind != JsonValueKind.Object) return;

        foreach (var group in unitized.EnumerateObject())
        {
            if (group.NameEquals("Comp")) continue;
            if (group.Value.ValueKind != JsonValueKind.Array) continue;

            string prefix = GetDevicePrefix(group.Name);
            string deviceGroup = GetDeviceGroupByUnitizedName(group.Name);

            int hour = 0;
            foreach (var row in group.Value.EnumerateArray())
            {
                var values = ReadNumericRow(row);
                var hourData = GetHourData(hourly, hour);
                double perFlow = GetPerDeviceFlow(group.Name, hourData);
                double perPower = GetPerDevicePower(group.Name, hourData, values);

                for (int i = 0; i < values.Count; i++)
                {
                    var level = values[i];
                    var matrix = new EquipmentOnOffMatrix
                    {
                        EquipmentId = null,
                        DeviceCode = $"{prefix}{i + 1}",
                        DeviceGroup = deviceGroup,
                        OnOff = level > 0 ? 1 : 0,
                        FlowM3h = level > 0 ? perFlow : 0,
                        PowerKw = level > 0 ? perPower : 0
                    };

                    await _schedulePlanRepository.UpsertEquipmentOnOffMatrixAsync(planId, hour, matrix, cancellationToken);
                }

                hour++;
            }
        }
    }

    private async Task SaveCompressorSchedulesAsync(
        int planId,
        JsonElement hourly,
        JsonElement unitized,
        CancellationToken cancellationToken)
    {
        if (unitized.ValueKind != JsonValueKind.Object || !unitized.TryGetProperty("Comp", out var compRows) || compRows.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        int hour = 0;
        foreach (var row in compRows.EnumerateArray())
        {
            var values = ReadNumericRow(row);
            var hourData = GetHourData(hourly, hour);
            double totalCompPower = GetDoubleProperty(hourData, "CompPower");
            int activeCount = values.Count(v => v > 0);
            double perPower = activeCount > 0 ? totalCompPower / activeCount : 0;

            for (int i = 0; i < values.Count; i++)
            {
                var level = values[i];
                var schedule = new CompressorSchedule
                {
                    CompId = $"Comp{i + 1}",
                    EquipmentId = null,
                    LevelRatio = level,
                    PowerKw = level > 0 ? perPower : 0
                };

                await _schedulePlanRepository.UpsertCompressorScheduleAsync(planId, hour, schedule, cancellationToken);
            }

            hour++;
        }
    }

    private async Task SaveScheduleDetailsAsync(
        int planId,
        JsonElement hourly,
        JsonElement unitized,
        CancellationToken cancellationToken)
    {
        if (unitized.ValueKind != JsonValueKind.Object) return;

        int totalHours = GetMaxHourCount(unitized);
        for (int hour = 0; hour < totalHours; hour++)
        {
            var hourData = GetHourData(hourly, hour);
            int sequence = 1;

            sequence = await SaveGroupDetailsAsync(
                planId, hour, sequence, "LP", "LP#", "LP_PUMP",
                GetUnitizedRow(unitized, "LP", hour),
                GetDoubleProperty(hourData, "LP_Flow_perPump_m3h", "LpFlowPerPump"),
                GetDoubleProperty(hourData, "Ideal_LP_Pressure_MPa", "IdealLpPressure"),
                cancellationToken);

            sequence = await SaveGroupDetailsAsync(
                planId, hour, sequence, "HP", "HP#", "HP_PUMP",
                GetUnitizedRow(unitized, "HP", hour),
                GetDoubleProperty(hourData, "HP_Flow_perPump_m3h", "HpFlowPerPump"),
                GetDoubleProperty(hourData, "Ideal_HP_Pressure_MPa", "IdealHpPressure"),
                cancellationToken);

            sequence = await SaveGroupDetailsAsync(
                planId, hour, sequence, "SW_Big", "SWBig#", "SW_BIG",
                GetUnitizedRow(unitized, "SW_Big", hour),
                0,
                0,
                cancellationToken);

            sequence = await SaveGroupDetailsAsync(
                planId, hour, sequence, "SW_Small", "SWSmall#", "SW_SMALL",
                GetUnitizedRow(unitized, "SW_Small", hour),
                0,
                0,
                cancellationToken);

            sequence = await SaveGroupDetailsAsync(
                planId, hour, sequence, "ORV", "ORV#", "ORV",
                GetUnitizedRow(unitized, "ORV", hour),
                0,
                0,
                cancellationToken);

            sequence = await SaveGroupDetailsAsync(
                planId, hour, sequence, "Comp", "Comp", "COMPRESSOR",
                GetUnitizedRow(unitized, "Comp", hour),
                0,
                0,
                cancellationToken,
                isCompressor: true);
        }
    }

    private async Task<int> SaveGroupDetailsAsync(
        int planId,
        int hour,
        int startSequence,
        string groupName,
        string codePrefix,
        string deviceGroup,
        IReadOnlyList<double> values,
        double targetFlow,
        double targetPressure,
        CancellationToken cancellationToken,
        bool isCompressor = false)
    {
        int sequence = startSequence;

        for (int i = 0; i < values.Count; i++)
        {
            double value = values[i];
            string equipmentCode = isCompressor ? $"{codePrefix}{i + 1}" : $"{codePrefix}{i + 1}";
            var detail = new SchedulePlanDetail
            {
                PlanId = planId,
                Hour = hour,
                EquipmentId = null,
                EquipmentCode = equipmentCode,
                DeviceGroup = deviceGroup,
                Action = value > 0 ? "启动" : "停止",
                SequenceOrder = sequence++,
                DelayTimeSec = 0,
                TargetFlowM3h = (decimal)(value > 0 ? targetFlow : 0),
                TargetLoadPct = (decimal)(isCompressor ? value * 100 : (value > 0 ? 100 : 0)),
                TargetPressureMpa = (decimal)(value > 0 ? targetPressure : 0)
            };

            await _schedulePlanRepository.UpsertSchedulePlanDetailAsync(planId, detail, cancellationToken);
        }

        return sequence;
    }

    private static int CountStartups(JsonElement unitized)
    {
        if (unitized.ValueKind != JsonValueKind.Object) return 0;

        int startupCount = 0;
        foreach (var group in unitized.EnumerateObject())
        {
            if (group.Value.ValueKind != JsonValueKind.Array) continue;

            var previous = Array.Empty<double>();
            bool initialized = false;

            foreach (var row in group.Value.EnumerateArray())
            {
                var current = ReadNumericRow(row).ToArray();
                if (initialized)
                {
                    int length = Math.Max(previous.Length, current.Length);
                    for (int i = 0; i < length; i++)
                    {
                        double prev = i < previous.Length ? previous[i] : 0;
                        double now = i < current.Length ? current[i] : 0;
                        if (prev <= 0 && now > 0)
                        {
                            startupCount++;
                        }
                    }
                }

                previous = current;
                initialized = true;
            }
        }

        return startupCount;
    }

    private static int GetMaxHourCount(JsonElement unitized)
    {
        int max = 0;
        foreach (var property in unitized.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                max = Math.Max(max, property.Value.GetArrayLength());
            }
        }

        return max;
    }

    private static IReadOnlyList<double> GetUnitizedRow(JsonElement unitized, string propertyName, int hour)
    {
        if (!unitized.TryGetProperty(propertyName, out var matrix) || matrix.ValueKind != JsonValueKind.Array || matrix.GetArrayLength() <= hour)
        {
            return Array.Empty<double>();
        }

        return TryGetArrayElement(matrix, hour, out var row) ? ReadNumericRow(row) : Array.Empty<double>();
    }

    private static JsonElement GetHourData(JsonElement hourly, int hour)
    {
        if (hourly.ValueKind == JsonValueKind.Array && TryGetArrayElement(hourly, hour, out var row))
        {
            return row;
        }

        return default;
    }

    private static bool TryGetArrayElement(JsonElement array, int index, out JsonElement element)
    {
        if (array.ValueKind == JsonValueKind.Array)
        {
            int currentIndex = 0;
            foreach (var item in array.EnumerateArray())
            {
                if (currentIndex == index)
                {
                    element = item;
                    return true;
                }

                currentIndex++;
            }
        }

        element = default;
        return false;
    }

    private static List<double> ReadNumericRow(JsonElement row)
    {
        var values = new List<double>();
        if (row.ValueKind != JsonValueKind.Array) return values;

        foreach (var item in row.EnumerateArray())
        {
            values.Add(item.ValueKind == JsonValueKind.Number ? item.GetDouble() : 0);
        }

        return values;
    }

    private static List<double> GetNumberArray(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            return ReadNumericRow(property);
        }

        return new List<double>();
    }

    private static double GetPerDeviceFlow(string groupName, JsonElement hourData)
    {
        return groupName switch
        {
            "LP" => GetDoubleProperty(hourData, "LP_Flow_perPump_m3h", "LpFlowPerPump"),
            "HP" => GetDoubleProperty(hourData, "HP_Flow_perPump_m3h", "HpFlowPerPump"),
            _ => 0
        };
    }

    private static double GetPerDevicePower(string groupName, JsonElement hourData, IReadOnlyList<double> values)
    {
        int activeCount = values.Count(v => v > 0);
        if (activeCount <= 0) return 0;

        return groupName switch
        {
            "LP" => GetDoubleProperty(hourData, "LpPower") / activeCount,
            "HP" => GetDoubleProperty(hourData, "HpPower") / activeCount,
            "SW_Big" or "SW_Small" => GetDoubleProperty(hourData, "SwPower") / activeCount,
            _ => 0
        };
    }

    private static string GetDevicePrefix(string groupName) => groupName switch
    {
        "LP" => "LP#",
        "HP" => "HP#",
        "SW_Big" => "SWBig#",
        "SW_Small" => "SWSmall#",
        "ORV" => "ORV#",
        _ => $"{groupName}#"
    };

    private static string GetDeviceGroupByUnitizedName(string groupName) => groupName switch
    {
        "LP" => "LP_PUMP",
        "HP" => "HP_PUMP",
        "SW_Big" => "SW_BIG",
        "SW_Small" => "SW_SMALL",
        "ORV" => "ORV",
        "Comp" => "COMPRESSOR",
        _ => "UNKNOWN"
    };

    private static int GetIntProperty(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(propertyName, out var prop) &&
                prop.ValueKind == JsonValueKind.Number)
            {
                return prop.GetInt32();
            }
        }

        return 0;
    }

    private static double GetDoubleProperty(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(propertyName, out var prop) &&
                prop.ValueKind == JsonValueKind.Number)
            {
                return prop.GetDouble();
            }
        }

        return 0;
    }

    private static string? GetStringProperty(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(propertyName, out var prop) &&
                prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString();
            }
        }

        return null;
    }

    private static double TryGetDouble(JsonElement element, params string[] propertyNames) => GetDoubleProperty(element, propertyNames);

    private static int TryGetInt(JsonElement element, params string[] propertyNames) => GetIntProperty(element, propertyNames);

    private async Task SaveReportDatasetsAsync(int planId, CancellationToken cancellationToken)
    {
        var reportSheets = await _scheduleQueryRepository.GetReportSheetsByPlanIdAsync(planId, cancellationToken);
        foreach (var sheet in reportSheets)
        {
            var columnsJson = JsonSerializer.Serialize(sheet.Columns);
            var rowsJson = JsonSerializer.Serialize(sheet.Rows);

            await _schedulePlanRepository.UpsertReportDatasetAsync(
                planId,
                sheet.SheetName,
                string.IsNullOrWhiteSpace(sheet.DisplayName) ? sheet.SheetName : sheet.DisplayName,
                columnsJson,
                rowsJson,
                cancellationToken);
        }
    }

    private static string GeneratePlanCode() => $"PLAN-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

    private static string NormalizePriorityModeInput(string? calculationMode)
    {
        var normalized = calculationMode?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "middle" : normalized;
    }

    private static string NormalizeLevelStatus(string? levelStatus)
    {
        return levelStatus?.Trim().ToLowerInvariant() switch
        {
            "normal" => "正常",
            "正常" => "正常",
            "high_warning" => "高预警",
            "highwarning" => "高预警",
            "高预警" => "高预警",
            "low_warning" => "低预警",
            "lowwarning" => "低预警",
            "低预警" => "低预警",
            "high_high" => "高限位",
            "highhigh" => "高限位",
            "高限位" => "高限位",
            "low_low" => "低限位",
            "lowlow" => "低限位",
            "低限位" => "低限位",
            _ => "正常"
        };
    }
}
