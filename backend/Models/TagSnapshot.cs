namespace backend.Models.Realtime;

public class TagSnapshot
{
    public string TagName { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public int Quality { get; set; } = 192; // OPC 标准：Good
    public string Unit { get; set; } = string.Empty;
}