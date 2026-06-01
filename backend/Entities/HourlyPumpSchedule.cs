namespace backend.Entities;

public sealed class HourlyPumpSchedule
{
    public int LpNum { get; set; }

    public double LpFlowPerPump { get; set; }

    public double LpFlowTotal { get; set; }

    public double LpPower { get; set; }

    public double IdealLpPressure { get; set; }

    public int HpNum { get; set; }

    public double HpFlowPerPump { get; set; }

    public double HpFlowTotal { get; set; }

    public double HpPower { get; set; }

    public double IdealHpPressure { get; set; }
}
