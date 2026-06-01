using backend.Dtos.Auth;
using backend.Entities;
using backend.Repositories;

namespace backend.Services;

public sealed class AuthAppService : IAuthAppService
{
    private readonly IAuthRepository _authRepository;

    public AuthAppService(IAuthRepository authRepository)
    {
        _authRepository = authRepository;
    }

    public async Task<LoginResult?> LoginAsync(string username, string password, CancellationToken cancellationToken)
    {
        var record = await _authRepository.GetUserWithRoleByUsernameAsync(username, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var (user, role) = record.Value;
        if (!user.IsActive)
        {
            return null;
        }

        // 当前数据库初始化数据使用明文测试密码，后续切换为哈希时可在这里替换校验逻辑。
        if (!string.Equals(user.PasswordHash, password, StringComparison.Ordinal))
        {
            return null;
        }

        return new LoginResult
        {
            UserId = user.UserId,
            Username = user.Username,
            RoleId = user.RoleId,
            RoleCode = role?.RoleCode,
            RoleName = role?.RoleName,
            Message = "Login successful."
        };
    }

    public async Task<RegisterResult> RegisterAsync(RegisterInput input, CancellationToken cancellationToken)
    {
        var username = input.Username.Trim();
        var roleCode = string.IsNullOrWhiteSpace(input.RoleCode) ? "VISITOR" : input.RoleCode.Trim().ToUpperInvariant();

        var existingCount = await _authRepository.CountUsersByUsernameAsync(username, cancellationToken);
        if (existingCount > 0)
        {
            throw new InvalidOperationException("Username already exists.");
        }

        var role = await _authRepository.GetActiveRoleByCodeAsync(roleCode, cancellationToken);
        if (role is null)
        {
            throw new InvalidOperationException("Role does not exist or is inactive.");
        }

        var (roleId, roleName) = role.Value;

        var user = new SysUser
        {
            Username = username,
            PasswordHash = input.Password,
            RoleId = roleId,
            Email = input.Email,
            Phone = input.Phone,
            Department = input.Department,
            IsActive = true
        };

        var userId = await _authRepository.InsertUserAsync(user, cancellationToken);

        return new RegisterResult
        {
            UserId = userId,
            Username = username,
            RoleId = roleId,
            RoleCode = roleCode,
            RoleName = roleName,
            Message = "Register successful."
        };
    }

    public async Task<LogoutResult> LogoutAsync(LogoutInput input, CancellationToken cancellationToken)
    {
        if (input.UserId is null && string.IsNullOrWhiteSpace(input.Username))
        {
            return new LogoutResult
            {
                Success = true,
                Message = "Logout successful."
            };
        }

        var record = await _authRepository.GetUserWithRoleAsync(input.UserId, input.Username, cancellationToken);
        if (record is null)
        {
            return new LogoutResult
            {
                Success = true,
                Message = "Logout successful."
            };
        }

        return new LogoutResult
        {
            Success = true,
            UserId = record.Value.User.UserId,
            Username = record.Value.User.Username,
            Message = "Logout successful."
        };
    }

    public async Task<UserInfo?> GetUserInfoAsync(int? userId, string? username, CancellationToken cancellationToken)
    {
        var record = await _authRepository.GetUserWithRoleAsync(userId, username, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var (user, role) = record.Value;

        return new UserInfo
        {
            UserId = user.UserId,
            Username = user.Username,
            RoleId = user.RoleId,
            RoleCode = role?.RoleCode,
            RoleName = role?.RoleName,
            Email = user.Email,
            Phone = user.Phone,
            Department = user.Department,
            IsActive = user.IsActive
        };
    }
}
