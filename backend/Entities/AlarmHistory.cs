namespace backend.Entities;

public class AlarmHistory
{
    public long AlarmId { get; set; }
    public int AlarmConfigId { get; set; }
    public int EquipmentId { get; set; }
    public int TagId { get; set; }
    public string AlarmLevel { get; set; } = string.Empty;
    public decimal AlarmValue { get; set; }
    public decimal ThresholdValue { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? DurationSec { get; set; }
    public DateTime? AckTime { get; set; }
    public int? AckBy { get; set; }
}