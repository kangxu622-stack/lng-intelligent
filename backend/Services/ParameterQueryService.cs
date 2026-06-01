using System.Text.Json;
using backend.Dtos.Simulation;
using backend.Repositories;

namespace backend.Services;

public sealed class ParameterQueryService : IParameterQueryService
{
    private const double CurrentOpcInitialLiquidLevelM = 20.0;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly string[] RequiredAlgorithmParameterKeys =
    [
        "total_output_target_m3_day",
        "tank_pressure_max",
        "tank_pressure_min",
        "tank_level_max",
        "tank_level_min",
        "num_tanks",
        "tank_volume",
        "s_tan",
        "initial_liquid",
        "rho_lng",
        "rho_sw",
        "cp_sw",
        "delta_t_sw",
        "latent_heat_kjkg",
        "num_lp_pumps",
        "num_hp_pumps",
        "num_sw_big",
        "num_sw_small",
        "num_orv",
        "num_compressors",
        "lp_pq_coeffs",
        "lp_flow_min",
        "lp_flow_max",
        "lp_power_max",
        "hp_pq_coeffs",
        "hp_flow_min",
        "hp_flow_max",
        "hp_power_max",
        "sw_big_flow",
        "sw_big_power",
        "sw_small_flow",
        "sw_small_power",
        "comp_levels",
        "comp_power_levels",
        "comp_capacity_kgph",
        "orv_unit_capacity_m3h",
        "lp_target_pressure",
        "hp_target_pressure",
        "start_time",
        "peak_elec_price",
        "flat_elec_price",
        "valley_elec_price",
        "t_lng",
        "pipe_heat_u",
        "pump_heat_fraction",
        "flash_fraction",
        "bog_density_kgm3",
        "k_atm",
        "k_dpatm",
        "demand_charge_price",
        "line_alarm_kw",
        "tank_daily_bor",
        "pipe_heat_area_m2",
        "recondenser_factor",
        "pipe_bog_share",
        "atm_ref_kpa",
        "unload_comp_min_level",
        "daily_demand_curve",
        "tank_master_data",
        "device_master_data"
    ];

    private readonly IParameterRepository _parameterRepository;

    public ParameterQueryService(IParameterRepository parameterRepository)
    {
        _parameterRepository = parameterRepository;
    }

    public async Task<Dictionary<string, object?>> BuildAlgorithmParamsAsync(
        ScheduleCalculationInput request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);

        await ApplyTankParametersAsync(parameters, request, cancellationToken);
        await ApplyPumpParametersAsync(parameters, request, cancellationToken);
        await ApplySeawaterPumpParametersAsync(parameters, cancellationToken);
        await ApplyOrvParametersAsync(parameters, cancellationToken);
        await ApplyBogCompressorParametersAsync(parameters, cancellationToken);
        await ApplyProcessMediumPropertiesAsync(parameters, cancellationToken);
        await ApplyTankMasterDataAsync(parameters, cancellationToken);
        await ApplyDeviceMasterDataAsync(parameters, cancellationToken);

        if (request.TargetOutputM3.HasValue)
        {
            parameters["total_output_target_m3_day"] = request.TargetOutputM3.Value;
        }

        if (request.LpTargetPressure.HasValue)
        {
            parameters["lp_target_pressure"] = request.LpTargetPressure.Value;
        }

        if (request.HpTargetPressure.HasValue)
        {
            parameters["hp_target_pressure"] = request.HpTargetPressure.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.StartTime))
        {
            parameters["start_time"] = request.StartTime.Trim();
        }

        if (request.DailyDemandCurve is { Count: > 0 })
        {
            parameters["daily_demand_curve"] = request.DailyDemandCurve.ToArray();
        }

        parameters["plan_name"] = request.PlanName;
        parameters["condition_name"] = request.ConditionName;
        parameters["remark"] = request.Remark;

        ValidateRequiredParameters(parameters);
        return parameters;
    }

    private static void ValidateRequiredParameters(IDictionary<string, object?> parameters)
    {
        var missingKeys = RequiredAlgorithmParameterKeys
            .Where(key => !parameters.TryGetValue(key, out var value) || value is null || IsEmptyArray(value))
            .ToArray();

        if (missingKeys.Length == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Missing required algorithm parameters from backend assembly: {string.Join(", ", missingKeys)}");
    }

    private static bool IsEmptyArray(object value)
    {
        return value switch
        {
            Array array => array.Length == 0,
            _ => false
        };
    }

    private async Task ApplyTankParametersAsync(
        IDictionary<string, object?> parameters,
        ScheduleCalculationInput request,
        CancellationToken cancellationToken)
    {
        var summary = await _parameterRepository.GetTankSummaryAsync(cancellationToken);
        if (summary is null)
        {
            return;
        }

        parameters["num_tanks"] = summary.Value.TankCount;
        parameters["tank_volume"] = summary.Value.TankCapacityM3;
        parameters["s_tan"] = summary.Value.SectionAreaM2;
        parameters["initial_liquid"] = GetInitialLiquidLevelM(request);
        parameters["tank_level_max"] = summary.Value.TankLevelMax;
        parameters["tank_level_min"] = summary.Value.TankLevelMin;
        parameters["tank_pressure_max"] = summary.Value.TankPressureMax;
        parameters["tank_pressure_min"] = summary.Value.TankPressureMin;
    }

    private async Task ApplyPumpParametersAsync(
        IDictionary<string, object?> parameters,
        ScheduleCalculationInput request,
        CancellationToken cancellationToken)
    {
        var lp = await _parameterRepository.GetLowPressurePumpSummaryAsync(cancellationToken);
        if (lp is not null)
        {
            parameters["num_lp_pumps"] = lp.Value.PumpCount;
            parameters["lp_flow_min"] = lp.Value.MinFlowM3h;
            parameters["lp_flow_max"] = lp.Value.MaxFlowM3h;
            parameters["lp_power_max"] = lp.Value.RatedPowerKw;
            parameters["lp_target_pressure"] = request.LpTargetPressure ?? lp.Value.TargetPressureMpa;
            parameters["lp_pq_coeffs"] = new[] { lp.Value.PqA, lp.Value.PqB, lp.Value.PqC };
        }

        var hp = await _parameterRepository.GetHighPressurePumpSummaryAsync(cancellationToken);
        if (hp is not null)
        {
            parameters["num_hp_pumps"] = hp.Value.PumpCount;
            parameters["hp_flow_min"] = hp.Value.MinFlowM3h;
            parameters["hp_flow_max"] = hp.Value.MaxFlowM3h;
            parameters["hp_power_max"] = hp.Value.RatedPowerKw;
            parameters["hp_target_pressure"] = request.HpTargetPressure ?? hp.Value.TargetPressureMpa;
            parameters["hp_pq_coeffs"] = new[] { hp.Value.PqA, hp.Value.PqB, hp.Value.PqC };
        }
    }

    private async Task ApplySeawaterPumpParametersAsync(
        IDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var seawaterGroups = await _parameterRepository.GetSeawaterPumpGroupsAsync(cancellationToken);
        var groups = seawaterGroups
            .Select(item => (Count: item.Count, Flow: item.RatedFlowM3h, Power: item.RatedPowerKw))
            .ToList();
        if (groups.Count == 0)
        {
            return;
        }

        var biggestPumpGroup = groups.OrderByDescending(item => item.Flow).First();
        parameters["num_sw_big"] = biggestPumpGroup.Count;
        parameters["sw_big_flow"] = biggestPumpGroup.Flow;
        parameters["sw_big_power"] = biggestPumpGroup.Power;

        var smallestPumpGroup = groups.OrderBy(item => item.Flow).First();
        parameters["num_sw_small"] = smallestPumpGroup.Count;
        parameters["sw_small_flow"] = smallestPumpGroup.Flow;
        parameters["sw_small_power"] = smallestPumpGroup.Power;
    }

    private async Task ApplyOrvParametersAsync(
        IDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var orvSummary = await _parameterRepository.GetOrvSummaryAsync(cancellationToken);
        if (orvSummary is null)
        {
            return;
        }

        parameters["num_orv"] = orvSummary.Value.OrvCount;
        parameters["orv_unit_capacity_m3h"] = orvSummary.Value.MaxFlowM3h;
    }

    private async Task ApplyBogCompressorParametersAsync(
        IDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var compressor = await _parameterRepository.GetBogCompressorSummaryAsync(cancellationToken);
        if (compressor is null)
        {
            return;
        }

        parameters["num_compressors"] = compressor.Value.CompressorCount;
        parameters["comp_capacity_kgph"] = compressor.Value.RatedCapacityKgph;
        parameters["comp_power_levels"] = DeserializeDoubleArray(compressor.Value.PowerLevelsJson);
        parameters["comp_levels"] = DeserializeDoubleArray(compressor.Value.LoadLevelsJson);
    }

    private static double[] DeserializeDoubleArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<double[]>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task ApplyProcessMediumPropertiesAsync(
        IDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var properties = await _parameterRepository.GetProcessMediumPropertiesAsync(cancellationToken);
        var electricityPriceCandidates = new List<double>();
        double? line1AlarmKw = null;
        double? line2AlarmKw = null;

        foreach (var property in properties)
        {
            var normalizedName = NormalizeText(property.PropertyName);
            var normalizedUnit = NormalizeText(property.Unit);
            var normalizedMedium = NormalizeText(property.MediumType);

            switch (normalizedName)
            {
                case "t_lng":
                    parameters["t_lng"] = property.PropertyValue;
                    continue;
                case "u_insulation":
                    parameters["pipe_heat_u"] = property.PropertyValue;
                    continue;
                case "pump_heat_fraction":
                    parameters["pump_heat_fraction"] = property.PropertyValue;
                    continue;
                case "flash_fraction":
                    parameters["flash_fraction"] = property.PropertyValue;
                    continue;
                case "rho_gas":
                    parameters["bog_density_kgm3"] = property.PropertyValue;
                    continue;
                case "k_atm_p":
                    parameters["k_atm"] = property.PropertyValue;
                    continue;
                case "k_dp_dt":
                    parameters["k_dpatm"] = property.PropertyValue;
                    continue;
                case "demand_charge_price":
                    parameters["demand_charge_price"] = property.PropertyValue;
                    continue;
                case "line1_alarm_kw":
                    line1AlarmKw = property.PropertyValue;
                    continue;
                case "line2_alarm_kw":
                    line2AlarmKw = property.PropertyValue;
                    continue;
                case "tank_daily_bor":
                    parameters["tank_daily_bor"] = property.PropertyValue;
                    continue;
                case "pipe_heat_area_m2":
                    parameters["pipe_heat_area_m2"] = property.PropertyValue;
                    continue;
                case "recondenser_factor":
                    parameters["recondenser_factor"] = property.PropertyValue;
                    continue;
                case "pipe_bog_share":
                    parameters["pipe_bog_share"] = property.PropertyValue;
                    continue;
                case "atm_ref_kpa":
                    parameters["atm_ref_kpa"] = property.PropertyValue;
                    continue;
                case "unload_comp_min_level":
                    parameters["unload_comp_min_level"] = property.PropertyValue;
                    continue;
            }

            if (TryMapProperty(parameters, property.PropertyValue, normalizedUnit, normalizedMedium))
            {
                continue;
            }

            TryCollectElectricityPriceCandidate(
                electricityPriceCandidates,
                property.PropertyValue,
                normalizedName,
                normalizedUnit);
        }

        if (electricityPriceCandidates.Count >= 3)
        {
            var ordered = electricityPriceCandidates.Distinct().OrderBy(value => value).ToArray();
            if (ordered.Length >= 3)
            {
                parameters["flat_elec_price"] = ordered[0];
                parameters["valley_elec_price"] = ordered[1];
                parameters["peak_elec_price"] = ordered[^1];
            }
        }

        if (line1AlarmKw.HasValue && line2AlarmKw.HasValue)
        {
            parameters["line_alarm_kw"] = new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["L1"] = line1AlarmKw.Value,
                ["L2"] = line2AlarmKw.Value
            };
        }
    }

    private async Task ApplyTankMasterDataAsync(
        IDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var tanks = await _parameterRepository.GetTankMasterDataAsync(cancellationToken);
        parameters["tank_master_data"] = tanks
            .Select(item => new Dictionary<string, object?>
            {
                ["name"] = item.Name,
                ["phase"] = item.Phase,
                ["area_m2"] = item.AreaM2,
                ["level_min_m"] = item.LevelMinM,
                ["level_pump_start_m"] = item.PumpStartLevelM,
                ["level_max_m"] = item.LevelMaxM,
                ["level_init_m"] = item.LevelInitM
            })
            .ToList();
    }

    private async Task ApplyDeviceMasterDataAsync(
        IDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var devices = await _parameterRepository.GetDeviceMasterDataAsync(cancellationToken);
        parameters["device_master_data"] = devices
            .Select(item => new Dictionary<string, object?>
            {
                ["name"] = item.Name,
                ["category"] = item.Category,
                ["line"] = item.Line,
                ["rated_power_kw"] = item.RatedPowerKw,
                ["reactive_kvar"] = item.ReactiveKvar,
                ["min_flow"] = item.MinFlowM3h,
                ["max_flow"] = item.MaxFlowM3h,
                ["tank"] = item.TankName,
                ["capacity_kgph"] = item.CapacityKgph,
                ["phase"] = item.Phase
            })
            .ToList();
    }

    private static double GetInitialLiquidLevelM(ScheduleCalculationInput request)
    {
        if (request.InitialLiquidLevel.HasValue)
        {
            return request.InitialLiquidLevel.Value;
        }

        return CurrentOpcInitialLiquidLevelM;
    }

    private static bool TryMapProperty(
        IDictionary<string, object?> parameters,
        double value,
        string normalizedUnit,
        string normalizedMedium)
    {
        if (normalizedMedium.Contains("lng") && normalizedUnit.Contains("kg") && !normalizedUnit.Contains("kj"))
        {
            parameters["rho_lng"] = value;
            return true;
        }

        if (normalizedMedium.Contains("lng") && normalizedUnit.Contains("kj") && normalizedUnit.Contains("kg"))
        {
            parameters["latent_heat_kjkg"] = value;
            return true;
        }

        if (normalizedMedium.Contains("seawater") && normalizedUnit.Contains("kg") && !normalizedUnit.Contains("kj"))
        {
            parameters["rho_sw"] = value;
            return true;
        }

        if (normalizedMedium.Contains("seawater") && normalizedUnit.Contains("kj") && normalizedUnit.Contains("kg"))
        {
            parameters["cp_sw"] = value;
            return true;
        }

        if (normalizedMedium.Contains("seawater") && normalizedUnit.Contains("k"))
        {
            parameters["delta_t_sw"] = value;
            return true;
        }

        if (normalizedMedium.Contains("common") && (normalizedUnit.Contains("day") || normalizedUnit.Contains("/d")))
        {
            parameters["total_output_target_m3_day"] = value;
            return true;
        }

        if (normalizedMedium.Contains("common") && normalizedUnit == "mpa" && value > 0 && value < 5)
        {
            parameters["lp_target_pressure"] = value;
            return true;
        }

        if (normalizedMedium.Contains("common") && normalizedUnit == "mpa" && value >= 5)
        {
            parameters["hp_target_pressure"] = value;
            return true;
        }

        return false;
    }

    private static bool TryCollectElectricityPriceCandidate(
        ICollection<double> electricityPriceCandidates,
        double value,
        string normalizedName,
        string normalizedUnit)
    {
        if (!normalizedName.Contains("price") && !normalizedUnit.Contains("kwh"))
        {
            return false;
        }

        electricityPriceCandidates.Add(value);
        return true;
    }

    private static string NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}
