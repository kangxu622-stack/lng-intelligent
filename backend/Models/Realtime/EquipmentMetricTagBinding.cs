namespace backend.Models.Realtime;

public sealed class EquipmentMetricTagBinding
{
    public string? FlowRateTagName { get; set; }
    public string? PressureTagName { get; set; }
    public string? CurrentPowerTagName { get; set; }
    public string? TemperatureTagName { get; set; }
    public string? LiquidLevelTagName { get; set; }
    public string? StatusTagName { get; set; }
}

public sealed class EquipmentTagBindingOptions
{
    public Dictionary<string, EquipmentMetricTagBinding> Overrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
