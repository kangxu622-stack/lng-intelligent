namespace backend.Dtos.Simulation;

public sealed class InitialConditionDefaultsDto
{
    public double TargetOutputM3 { get; set; }

    public double LpTargetPressure { get; set; }

    public double HpTargetPressure { get; set; }

    public double InitialLiquidLevel { get; set; }
}
