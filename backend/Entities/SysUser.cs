namespace backend.Entities;

public sealed class SysUser
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? RealName { get; set; }

    public int? RoleId { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Department { get; set; }

    public bool IsActive { get; set; }
}
