namespace backend.Dtos.Auth;

public sealed class LoginResult
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public int? RoleId { get; set; }

    public string? RoleCode { get; set; }

    public string? RoleName { get; set; }

    public string Message { get; set; } = string.Empty;
}
