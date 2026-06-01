using backend.Dtos.SystemManagement;
using backend.Entities;
using backend.Repositories;

namespace backend.Services;

public sealed class SystemManagementAppService : ISystemManagementAppService
{
    private readonly ISystemManagementRepository _repository;

    public SystemManagementAppService(ISystemManagementRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<RoleListItem>> GetRolesAsync(string? keyword, bool? isActive, CancellationToken cancellationToken)
    {
        return _repository.GetRolesAsync(keyword, isActive, cancellationToken);
    }

    public async Task<RoleListItem> CreateRoleAsync(RoleUpsertInput input, CancellationToken cancellationToken)
    {
        var roleCode = NormalizeRequired(input.RoleCode, "Role code is required.");
        var roleName = NormalizeRequired(input.RoleName, "Role name is required.");

        var exists = await _repository.CountRolesByCodeAsync(roleCode, null, cancellationToken);
        if (exists > 0)
        {
            throw new InvalidOperationException("Role code already exists.");
        }

        var roleId = await _repository.InsertRoleAsync(new SysRole
        {
            RoleCode = roleCode,
            RoleName = roleName,
            IsActive = input.IsActive
        }, cancellationToken);

        return await GetRoleOrThrowAsync(roleId, cancellationToken);
    }

    public async Task<RoleListItem> UpdateRoleAsync(int roleId, RoleUpsertInput input, CancellationToken cancellationToken)
    {
        var roleCode = NormalizeRequired(input.RoleCode, "Role code is required.");
        var roleName = NormalizeRequired(input.RoleName, "Role name is required.");

        var exists = await _repository.CountRolesByCodeAsync(roleCode, roleId, cancellationToken);
        if (exists > 0)
        {
            throw new InvalidOperationException("Role code already exists.");
        }

        var updated = await _repository.UpdateRoleAsync(new SysRole
        {
            RoleId = roleId,
            RoleCode = roleCode,
            RoleName = roleName,
            IsActive = input.IsActive
        }, cancellationToken);

        if (!updated)
        {
            throw new InvalidOperationException("Role not found.");
        }

        return await GetRoleOrThrowAsync(roleId, cancellationToken);
    }

    public async Task DeleteRoleAsync(int roleId, CancellationToken cancellationToken)
    {
        var hasUsers = await _repository.RoleHasUsersAsync(roleId, cancellationToken);
        if (hasUsers)
        {
            throw new InvalidOperationException("This role is still assigned to users and cannot be deleted.");
        }

        var deleted = await _repository.DeleteRoleAsync(roleId, cancellationToken);
        if (!deleted)
        {
            throw new InvalidOperationException("Role not found.");
        }
    }

    public Task<IReadOnlyList<UserListItem>> GetUsersAsync(string? username, string? phone, bool? isActive, CancellationToken cancellationToken)
    {
        return _repository.GetUsersAsync(username, phone, isActive, cancellationToken);
    }

    public async Task<UserListItem> CreateUserAsync(UserUpsertInput input, CancellationToken cancellationToken)
    {
        var username = NormalizeRequired(input.Username, "Username is required.");
        var password = NormalizeRequired(input.Password, "Password is required.");

        var exists = await _repository.CountUsersByUsernameAsync(username, null, cancellationToken);
        if (exists > 0)
        {
            throw new InvalidOperationException("Username already exists.");
        }

        var userId = await _repository.InsertUserAsync(new SysUser
        {
            Username = username,
            PasswordHash = password,
            RoleId = input.RoleId,
            Email = NormalizeOptional(input.Email),
            Phone = NormalizeOptional(input.Phone),
            Department = NormalizeOptional(input.Department),
            IsActive = input.IsActive
        }, cancellationToken);

        return await GetUserOrThrowAsync(userId, cancellationToken);
    }

    public async Task<UserListItem> UpdateUserAsync(int userId, UserUpsertInput input, CancellationToken cancellationToken)
    {
        var username = NormalizeRequired(input.Username, "Username is required.");
        var exists = await _repository.CountUsersByUsernameAsync(username, userId, cancellationToken);
        if (exists > 0)
        {
            throw new InvalidOperationException("Username already exists.");
        }

        var updatePassword = !string.IsNullOrWhiteSpace(input.Password);
        var updated = await _repository.UpdateUserAsync(new SysUser
        {
            UserId = userId,
            Username = username,
            PasswordHash = updatePassword ? input.Password!.Trim() : string.Empty,
            RoleId = input.RoleId,
            Email = NormalizeOptional(input.Email),
            Phone = NormalizeOptional(input.Phone),
            Department = NormalizeOptional(input.Department),
            IsActive = input.IsActive
        }, updatePassword, cancellationToken);

        if (!updated)
        {
            throw new InvalidOperationException("User not found.");
        }

        return await GetUserOrThrowAsync(userId, cancellationToken);
    }

    public async Task DeleteUserAsync(int userId, CancellationToken cancellationToken)
    {
        var deleted = await _repository.DeleteUserAsync(userId, cancellationToken);
        if (!deleted)
        {
            throw new InvalidOperationException("User not found.");
        }
    }

    private async Task<RoleListItem> GetRoleOrThrowAsync(int roleId, CancellationToken cancellationToken)
    {
        var roles = await _repository.GetRolesAsync(null, null, cancellationToken);
        return roles.FirstOrDefault(item => item.RoleId == roleId)
            ?? throw new InvalidOperationException("Role not found.");
    }

    private async Task<UserListItem> GetUserOrThrowAsync(int userId, CancellationToken cancellationToken)
    {
        var users = await _repository.GetUsersAsync(null, null, null, cancellationToken);
        return users.FirstOrDefault(item => item.UserId == userId)
            ?? throw new InvalidOperationException("User not found.");
    }

    private static string NormalizeRequired(string? value, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
