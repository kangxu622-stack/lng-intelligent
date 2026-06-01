namespace backend.Entities;

public class Equipment
{
    public int EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public int TypeId { get; set; }
    public int? ParentEquipmentId { get; set; }
    public string SystemCode { get; set; } = string.Empty;
    public string ProcessArea { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsControllable { get; set; }
    public DateTime? InstallDate { get; set; }
    public DateTime? CommissionedDate { get; set; }
    public string Remark { get; set; } = string.Empty;
}