namespace backend.Entities;

public sealed class EquipmentType
{
    public int TypeId { get; set; }

    public string TypeCode { get; set; } = string.Empty;

    public string TypeName { get; set; } = string.Empty;

    public int ExpectedCount { get; set; }
}
