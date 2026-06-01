namespace backend.Dtos.SystemManagement;

public sealed class RoleUpsertInput
{
    public string RoleCode { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
