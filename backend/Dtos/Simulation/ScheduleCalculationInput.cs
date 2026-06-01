namespace backend.Dtos.Simulation;

public sealed class ScheduleCalculationInput
{
    public string? PlanName { get; set; }

    public string? ConditionName { get; set; }

    public string? Remark { get; set; }

    public double? TargetOutputM3 { get; set; }

    public double? LpTargetPressure { get; set; }

    public double? HpTargetPressure { get; set; }

    public double? InitialLiquidLevel { get; set; }

    public string? StartTime { get; set; }

    public List<double>? DailyDemandCurve { get; set; }

    public string? CalculationMode { get; set; }
}
