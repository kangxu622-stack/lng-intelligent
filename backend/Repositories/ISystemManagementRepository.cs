using backend.Dtos.SystemManagement;
using backend.Entities;

namespace backend.Repositories;

public interface ISystemManagementRepository
{
    Task<IReadOnlyList<RoleListItem>> GetRolesAsync(string? keyword, bool? isActive, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserListItem>> GetUsersAsync(string? username, string? phone, bool? isActive, CancellationToken cancellationToken);

    Task<int> CountRolesByCodeAsync(string roleCode, int? excludeRoleId, CancellationToken cancellationToken);

    Task<int> CountUsersByUsernameAsync(string username, int? excludeUserId, CancellationToken cancellationToken);

    Task<bool> RoleHasUsersAsync(int roleId, CancellationToken cancellationToken);

    Task<int> InsertRoleAsync(SysRole role, CancellationToken cancellationToken);

    Task<bool> UpdateRoleAsync(SysRole role, CancellationToken cancellationToken);

    Task<bool> DeleteRoleAsync(int roleId, CancellationToken cancellationToken);

    Task<int> InsertUserAsync(SysUser user, CancellationToken cancellationToken);

    Task<bool> UpdateUserAsync(SysUser user, bool updatePassword, CancellationToken cancellationToken);

    Task<bool> DeleteUserAsync(int userId, CancellationToken cancellationToken);
}
