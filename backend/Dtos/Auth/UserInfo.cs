namespace backend.Dtos.Auth;

public sealed class UserInfo
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public int? RoleId { get; set; }

    public string? RoleCode { get; set; }

    public string? RoleName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Department { get; set; }

    public bool IsActive { get; set; }
}
