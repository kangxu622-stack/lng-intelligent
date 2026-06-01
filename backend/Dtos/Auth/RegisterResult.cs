namespace backend.Dtos.Auth;

public sealed class RegisterResult
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public string RoleCode { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
