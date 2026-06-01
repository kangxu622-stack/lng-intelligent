using backend.Dtos.Auth;

namespace backend.Services;

public interface IAuthAppService
{
    Task<LoginResult?> LoginAsync(string username, string password, CancellationToken cancellationToken);

    Task<RegisterResult> RegisterAsync(RegisterInput input, CancellationToken cancellationToken);

    Task<LogoutResult> LogoutAsync(LogoutInput input, CancellationToken cancellationToken);

    Task<UserInfo?> GetUserInfoAsync(int? userId, string? username, CancellationToken cancellationToken);
}
