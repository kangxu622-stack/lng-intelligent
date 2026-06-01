namespace backend.Dtos.Auth;

public sealed class LogoutResult
{
    public bool Success { get; set; }

    public int? UserId { get; set; }

    public string? Username { get; set; }

    public string Message { get; set; } = string.Empty;
}
