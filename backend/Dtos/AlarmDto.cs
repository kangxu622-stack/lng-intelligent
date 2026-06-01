namespace backend.Dtos;

public class AlarmConfigDto
{
    public int AlarmConfigId { get; set; }
    public string AlarmCode { get; set; } = string.Empty;
    public string AlarmName { get; set; } = string.Empty;
    public string AlarmType { get; set; } = string.Empty;
    public int EquipmentId { get; set; }
    public int TagId { get; set; }
    public decimal ThresholdValue { get; set; }
    public decimal Hysteresis { get; set; }
    public string Priority { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class AlarmHistoryDto
{
    public long AlarmId { get; set; }
    public int AlarmConfigId { get; set; }
    public int EquipmentId { get; set; }
    public int TagId { get; set; }
    public string AlarmLevel { get; set; } = string.Empty;
    public decimal AlarmValue { get; set; }
    public decimal ThresholdValue { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? DurationSec { get; set; }
    public DateTime? AckTime { get; set; }
    public int? AckBy { get; set; }
}

public class AlarmQueryDto
{
    public int? EquipmentId { get; set; }
    public int? TagId { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AlarmConfigListResponse
{
    public List<AlarmConfigDto> Items { get; set; } = new();
}

public class AlarmHistoryListResponse
{
    public List<AlarmHistoryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
}