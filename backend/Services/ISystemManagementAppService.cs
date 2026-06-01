using backend.Dtos.SystemManagement;

namespace backend.Services;

public interface ISystemManagementAppService
{
    Task<IReadOnlyList<RoleListItem>> GetRolesAsync(string? keyword, bool? isActive, CancellationToken cancellationToken);

    Task<RoleListItem> CreateRoleAsync(RoleUpsertInput input, CancellationToken cancellationToken);

    Task<RoleListItem> UpdateRoleAsync(int roleId, RoleUpsertInput input, CancellationToken cancellationToken);

    Task DeleteRoleAsync(int roleId, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserListItem>> GetUsersAsync(string? username, string? phone, bool? isActive, CancellationToken cancellationToken);

    Task<UserListItem> CreateUserAsync(UserUpsertInput input, CancellationToken cancellationToken);

    Task<UserListItem> UpdateUserAsync(int userId, UserUpsertInput input, CancellationToken cancellationToken);

    Task DeleteUserAsync(int userId, CancellationToken cancellationToken);
}
