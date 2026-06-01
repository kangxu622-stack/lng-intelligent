namespace backend.Entities;

public sealed class HourlyEnergyCost
{
    public double LpPower { get; set; }

    public double HpPower { get; set; }

    public double SwPower { get; set; }

    public double CompPower { get; set; }

    public double RcPower { get; set; }

    public double TotalPower { get; set; }

    public double HourlyCost { get; set; }
}
