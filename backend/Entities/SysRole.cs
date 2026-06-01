namespace backend.Entities;

public sealed class SysRole
{
    public int RoleId { get; set; }

    public string RoleCode { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
