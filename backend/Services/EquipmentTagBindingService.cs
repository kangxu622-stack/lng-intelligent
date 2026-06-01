using backend.Models.Realtime;

namespace backend.Services;

public sealed class EquipmentTagBindingService : IEquipmentTagBindingService
{
    private readonly EquipmentTagBindingOptions _options;

    public EquipmentTagBindingService(IConfiguration configuration)
    {
        _options = configuration.GetSection("EquipmentTagBindings").Get<EquipmentTagBindingOptions>() ?? new EquipmentTagBindingOptions();
    }

    public EquipmentMetricTagBinding Resolve(string equipmentCode, string typeCode)
    {
        if (_options.Overrides.TryGetValue(equipmentCode, out var binding))
        {
            return MergeWithConvention(binding, equipmentCode, typeCode);
        }

        return BuildConventionBinding(equipmentCode, typeCode);
    }

    private static EquipmentMetricTagBinding MergeWithConvention(
        EquipmentMetricTagBinding binding,
        string equipmentCode,
        string typeCode)
    {
        var convention = BuildConventionBinding(equipmentCode, typeCode);

        return new EquipmentMetricTagBinding
        {
            FlowRateTagName = string.IsNullOrWhiteSpace(binding.FlowRateTagName) ? convention.FlowRateTagName : binding.FlowRateTagName,
            PressureTagName = string.IsNullOrWhiteSpace(binding.PressureTagName) ? convention.PressureTagName : binding.PressureTagName,
            CurrentPowerTagName = string.IsNullOrWhiteSpace(binding.CurrentPowerTagName) ? convention.CurrentPowerTagName : binding.CurrentPowerTagName,
            TemperatureTagName = string.IsNullOrWhiteSpace(binding.TemperatureTagName) ? convention.TemperatureTagName : binding.TemperatureTagName,
            LiquidLevelTagName = string.IsNullOrWhiteSpace(binding.LiquidLevelTagName) ? convention.LiquidLevelTagName : binding.LiquidLevelTagName,
            StatusTagName = string.IsNullOrWhiteSpace(binding.StatusTagName) ? convention.StatusTagName : binding.StatusTagName
        };
    }

    private static EquipmentMetricTagBinding BuildConventionBinding(string equipmentCode, string typeCode)
    {
        var normalizedCode = equipmentCode.Trim().ToUpperInvariant();
        var baseKey = normalizedCode.Replace(" ", string.Empty);

        return typeCode switch
        {
            "LP_PUMP" or "HP_PUMP" or "SW_PUMP_BIG" or "SW_PUMP_SMALL" => new EquipmentMetricTagBinding
            {
                FlowRateTagName = $"FI.{baseKey}",
                PressureTagName = $"PI.{baseKey}",
                CurrentPowerTagName = $"PWR.{baseKey}",
                TemperatureTagName = $"TI.{baseKey}",
                LiquidLevelTagName = $"LI.{baseKey}",
                StatusTagName = $"ST.{baseKey}"
            },
            "ORV" => new EquipmentMetricTagBinding
            {
                FlowRateTagName = $"FI.{baseKey}",
                PressureTagName = $"PI.{baseKey}",
                TemperatureTagName = $"TI.{baseKey}",
                StatusTagName = $"ST.{baseKey}"
            },
            "BOG_COMPRESSOR" => new EquipmentMetricTagBinding
            {
                FlowRateTagName = $"FI.{baseKey}",
                PressureTagName = $"PI.{baseKey}",
                CurrentPowerTagName = $"PWR.{baseKey}",
                TemperatureTagName = $"TI.{baseKey}",
                StatusTagName = $"ST.{baseKey}"
            },
            "RECONDENSER" => new EquipmentMetricTagBinding
            {
                FlowRateTagName = $"FI.{baseKey}",
                PressureTagName = $"PI.{baseKey}",
                TemperatureTagName = $"TI.{baseKey}",
                LiquidLevelTagName = $"LI.{baseKey}",
                StatusTagName = $"ST.{baseKey}"
            },
            _ => new EquipmentMetricTagBinding
            {
                FlowRateTagName = $"FI.{baseKey}",
                PressureTagName = $"PI.{baseKey}",
                CurrentPowerTagName = $"PWR.{baseKey}",
                TemperatureTagName = $"TI.{baseKey}",
                LiquidLevelTagName = $"LI.{baseKey}",
                StatusTagName = $"ST.{baseKey}"
            }
        };
    }
}
