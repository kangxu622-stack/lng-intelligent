namespace backend.Entities;

public class AlarmConfig
{
    public int AlarmConfigId { get; set; }
    public string AlarmCode { get; set; } = string.Empty;
    public string AlarmName { get; set; } = string.Empty;
    public string AlarmType { get; set; } = string.Empty;
    public int EquipmentId { get; set; }
    public int TagId { get; set; }
    public decimal ThresholdValue { get; set; }
    public decimal Hysteresis { get; set; }
    public string Priority { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string Description { get; set; } = string.Empty;
}