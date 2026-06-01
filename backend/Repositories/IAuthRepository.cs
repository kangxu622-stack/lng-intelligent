using backend.Entities;

namespace backend.Repositories;

public interface IAuthRepository
{
    Task<(SysUser User, SysRole? Role)?> GetUserWithRoleByUsernameAsync(string username, CancellationToken cancellationToken);

    Task<(SysUser User, SysRole? Role)?> GetUserWithRoleAsync(int? userId, string? username, CancellationToken cancellationToken);

    Task<int> CountUsersByUsernameAsync(string username, CancellationToken cancellationToken);

    Task<(int RoleId, string RoleName)?> GetActiveRoleByCodeAsync(string roleCode, CancellationToken cancellationToken);

    Task<int> InsertUserAsync(SysUser user, CancellationToken cancellationToken);
}
