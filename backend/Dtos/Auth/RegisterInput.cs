namespace backend.Dtos.Auth;

public sealed class RegisterInput
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Department { get; set; }

    public string? RoleCode { get; set; }
}
