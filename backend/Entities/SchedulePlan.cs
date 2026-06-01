namespace backend.Entities;

public class SchedulePlan
{
    public int PlanId { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public int ConditionId { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalOutputM3 { get; set; }
    public decimal TotalPowerKwh { get; set; }
    public decimal TotalCostCny { get; set; }
    public decimal OptimizationScore { get; set; }
    public decimal FitnessValue { get; set; }

    public string PlanName { get; set; } = string.Empty;
    public DateTime? SimulationStartTime { get; set; }
    public decimal TotalPenalty { get; set; }
    public decimal ActualDurationH { get; set; }
    public int MaxLpOnline { get; set; }
    public int MaxHpOnline { get; set; }
    public int TotalStartupCount { get; set; }
    public string ApprovalComment { get; set; } = string.Empty;

}
