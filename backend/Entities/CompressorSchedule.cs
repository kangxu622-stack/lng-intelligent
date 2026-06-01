namespace backend.Entities;

public sealed class CompressorSchedule
{
    public string CompId { get; set; } = string.Empty;

    public int? EquipmentId { get; set; }

    public double LevelRatio { get; set; }

    public double? PowerKw { get; set; }
}
