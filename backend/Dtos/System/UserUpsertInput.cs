namespace backend.Dtos.SystemManagement;

public sealed class UserUpsertInput
{
    public string Username { get; set; } = string.Empty;

    public string? Password { get; set; }

    public int? RoleId { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Department { get; set; }

    public bool IsActive { get; set; } = true;
}
