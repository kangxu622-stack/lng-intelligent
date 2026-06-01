namespace backend.Entities;

public class SchedulePlanDetail
{
    public int DetailId { get; set; }
    public int PlanId { get; set; }
    public int Hour { get; set; }
    public int? EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string DeviceGroup { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
    public int DelayTimeSec { get; set; }
    public decimal TargetFlowM3h { get; set; }
    public decimal TargetLoadPct { get; set; }
    public decimal TargetPressureMpa { get; set; }
}
