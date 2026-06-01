namespace backend.Entities;

public sealed class EquipmentOnOffMatrix
{
    public int? EquipmentId { get; set; }

    public string DeviceCode { get; set; } = string.Empty;

    public string DeviceGroup { get; set; } = string.Empty;

    public int OnOff { get; set; }

    public double FlowM3h { get; set; }

    public double PowerKw { get; set; }
}
