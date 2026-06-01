namespace backend.Entities;

public sealed class ScheduleInitialCondition
{
    public string? ConditionName { get; set; }

    public DateTime? StartTime { get; set; }

    public int CreatedBy { get; set; }

    public double TargetOutputM3 { get; set; }

    public double TargetPressureMpa { get; set; }

    public double LpTargetPressureMpa { get; set; }

    public double HpTargetPressureMpa { get; set; }

    public double InitialLiquidLevelM { get; set; }

    public int DemandDurationH { get; set; }

    public double DeltaTH { get; set; }

    public string PriorityMode { get; set; } = "节能";

    public string? ConstraintsJson { get; set; }

    public string? Remark { get; set; }
}
