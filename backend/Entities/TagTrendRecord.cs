namespace backend.Entities;

public class TagTrendRecord
{
    public long Id { get; set; }
    public string TagName { get; set; } = string.Empty;
    public double Value { get; set; }
    public int Quality { get; set; } = 192;
    public string Unit { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}