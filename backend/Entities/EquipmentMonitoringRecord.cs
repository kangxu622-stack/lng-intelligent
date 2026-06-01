namespace backend.Entities;

public class EquipmentMonitoringRecord
{
    public int EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string ProcessArea { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsControllable { get; set; }
    public decimal? FlowRate { get; set; }
    public decimal? Pressure { get; set; }
    public decimal? CurrentPower { get; set; }
    public decimal? Temperature { get; set; }
    public decimal? LiquidLevel { get; set; }
    public DateTime? UpdateTime { get; set; }
    public string Remark { get; set; } = string.Empty;
}

public class EquipmentMonitoringTrendPoint
{
    public DateTime Timestamp { get; set; }
    public decimal? FlowRate { get; set; }
    public decimal? Pressure { get; set; }
    public decimal? CurrentPower { get; set; }
    public decimal? Temperature { get; set; }
    public decimal? LiquidLevel { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class EquipmentMonitoringTrendData
{
    public int EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public List<EquipmentMonitoringTrendPoint> Items { get; set; } = new();
}
