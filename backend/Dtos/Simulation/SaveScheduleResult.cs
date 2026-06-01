namespace backend.Dtos.Simulation;

public sealed class SaveScheduleResult
{
    public int ConditionId { get; set; }

    public int PlanId { get; set; }

    public string PlanCode { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
