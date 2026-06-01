namespace backend.Entities;

public sealed class HourlyTankStatus
{
    public double TankLevel { get; set; }

    public double Inflow { get; set; }

    public double Outflow { get; set; }

    public double DeltaLevel { get; set; }

    public string LevelStatus { get; set; } = "正常";
}
