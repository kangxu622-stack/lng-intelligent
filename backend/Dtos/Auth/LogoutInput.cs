namespace backend.Dtos.Auth;

public sealed class LogoutInput
{
    public int? UserId { get; set; }

    public string? Username { get; set; }
}
