using System.Text.Json;

namespace backend.Dtos.Simulation;

public sealed class SaveScheduleInput
{
    public int CreatedBy { get; set; }

    public string? PlanName { get; set; }

    public JsonElement? ConstraintsJson { get; set; }

    public string? ApprovalComment { get; set; }

    public ScheduleCalculationInput? CalculateInput { get; set; }

    public ScheduleCalculationResult? SimulationResult { get; set; }
}
