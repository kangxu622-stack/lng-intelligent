namespace backend.Dtos;

public sealed class EquipmentCounts
{
    public List<EquipmentTypeCounts> EquipmentTypes { get; set; } = new();

    public int TotalCount { get; set; }
}

public sealed class EquipmentTypeCounts
{
    public int TypeId { get; set; }

    public string TypeCode { get; set; } = string.Empty;

    public string TypeName { get; set; } = string.Empty;

    public int ExpectedCount { get; set; }

    public int ActualCount { get; set; }

    public int OnlineCount { get; set; }

    public int OfflineCount { get; set; }
}
