namespace backend.Dtos.SystemManagement;

public sealed class RoleListItem
{
    public int RoleId { get; set; }

    public string RoleCode { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int UserCount { get; set; }
}
