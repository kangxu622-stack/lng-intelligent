namespace backend.Dtos;

public class EquipmentMonitoringQueryDto
{
    public string? TypeCode { get; set; }
}

public class EquipmentMonitoringGroupDto
{
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string ProcessArea { get; set; } = string.Empty;
    public List<EquipmentMonitoringItemDto> Items { get; set; } = new();
}

public class EquipmentMonitoringItemDto
{
    public int EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string ProcessArea { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public decimal? FlowRate { get; set; }
    public decimal? Pressure { get; set; }
    public decimal? CurrentPower { get; set; }
    public decimal? Temperature { get; set; }
    public decimal? LiquidLevel { get; set; }
    public DateTime? UpdateTime { get; set; }
    public string Remark { get; set; } = string.Empty;
    public EquipmentMetricTagBindingDto TagBindings { get; set; } = new();
}

public class EquipmentMonitoringResponse
{
    public List<EquipmentMonitoringGroupDto> Groups { get; set; } = new();
}

public class EquipmentMetricTagBindingDto
{
    public string? FlowRateTagName { get; set; }
    public string? PressureTagName { get; set; }
    public string? CurrentPowerTagName { get; set; }
    public string? TemperatureTagName { get; set; }
    public string? LiquidLevelTagName { get; set; }
    public string? StatusTagName { get; set; }
}

public class EquipmentMonitoringTrendPointDto
{
    public DateTime Timestamp { get; set; }
    public decimal? FlowRate { get; set; }
    public decimal? Pressure { get; set; }
    public decimal? CurrentPower { get; set; }
    public decimal? Temperature { get; set; }
    public decimal? LiquidLevel { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class EquipmentMonitoringTrendResponse
{
    public int EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public List<EquipmentMonitoringTrendPointDto> Items { get; set; } = new();
}
