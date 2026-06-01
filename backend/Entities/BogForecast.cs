namespace backend.Entities;

public sealed class BogForecast
{
    public double AmbientTempC { get; set; }

    public double AtmPressureKpa { get; set; }

    public double UnloadM3h { get; set; }

    public double BogMechKgph { get; set; }

    public double BogResidualKgph { get; set; }

    public double BogPredKgph { get; set; }

    public double CondensationCapacityKgph { get; set; }
}
