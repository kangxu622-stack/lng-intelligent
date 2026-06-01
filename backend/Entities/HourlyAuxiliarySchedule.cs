namespace backend.Entities;

public sealed class HourlyAuxiliarySchedule
{
    public int SwBigCount { get; set; }

    public int SwSmallCount { get; set; }

    public double SwFlowTotal { get; set; }

    public double SwPower { get; set; }

    public int OrvCount { get; set; }

    public double OrvHeatDuty { get; set; }
}
