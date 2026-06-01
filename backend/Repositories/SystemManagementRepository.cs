using backend.Dtos.SystemManagement;
using backend.Entities;
using MySqlConnector;

namespace backend.Repositories;

public sealed class SystemManagementRepository : ISystemManagementRepository
{
    private readonly string _connectionString;

    public SystemManagementRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string is not configured.");
    }

    public async Task<IReadOnlyList<RoleListItem>> GetRolesAsync(string? keyword, bool? isActive, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                r.role_id,
                r.role_code,
                r.role_name,
                r.is_active,
                COUNT(u.user_id) AS user_count
            FROM sys_role r
            LEFT JOIN sys_user u ON u.role_id = r.role_id
            WHERE (@keyword IS NULL OR r.role_name LIKE @keyword OR r.role_code LIKE @keyword)
              AND (@isActive IS NULL OR r.is_active = @isActive)
            GROUP BY r.role_id, r.role_code, r.role_name, r.is_active
            ORDER BY r.role_id DESC;
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@keyword", string.IsNullOrWhiteSpace(keyword) ? DBNull.Value : $"%{keyword.Trim()}%");
        cmd.Parameters.AddWithValue("@isActive", isActive.HasValue ? (isActive.Value ? 1 : 0) : DBNull.Value);

        var result = new List<RoleListItem>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new RoleListItem
            {
                RoleId = reader.GetInt32("role_id"),
                RoleCode = reader.GetString("role_code"),
                RoleName = reader.GetString("role_name"),
                IsActive = reader.GetBoolean("is_active"),
                UserCount = reader.GetInt32("user_count")
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<UserListItem>> GetUsersAsync(string? username, string? phone, bool? isActive, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                u.user_id,
                u.username,
                u.role_id,
                u.email,
                u.phone,
                u.department,
                u.is_active,
                r.role_code,
                r.role_name
            FROM sys_user u
            LEFT JOIN sys_role r ON r.role_id = u.role_id
            WHERE (@username IS NULL OR u.username LIKE @username)
              AND (@phone IS NULL OR u.phone LIKE @phone)
              AND (@isActive IS NULL OR u.is_active = @isActive)
            ORDER BY u.user_id DESC;
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@username", string.IsNullOrWhiteSpace(username) ? DBNull.Value : $"%{username.Trim()}%");
        cmd.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(phone) ? DBNull.Value : $"%{phone.Trim()}%");
        cmd.Parameters.AddWithValue("@isActive", isActive.HasValue ? (isActive.Value ? 1 : 0) : DBNull.Value);

        var result = new List<UserListItem>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var roleIdOrdinal = reader.GetOrdinal("role_id");
            var roleCodeOrdinal = TryGetOrdinal(reader, "role_code");
            var roleNameOrdinal = TryGetOrdinal(reader, "role_name");
            var emailOrdinal = TryGetOrdinal(reader, "email");
            var phoneOrdinal = TryGetOrdinal(reader, "phone");
            var departmentOrdinal = TryGetOrdinal(reader, "department");

            result.Add(new UserListItem
            {
                UserId = reader.GetInt32("user_id"),
                Username = reader.GetString("username"),
                RoleId = reader.IsDBNull(roleIdOrdinal) ? null : reader.GetInt32(roleIdOrdinal),
                RoleCode = roleCodeOrdinal < 0 || reader.IsDBNull(roleCodeOrdinal) ? null : reader.GetString(roleCodeOrdinal),
                RoleName = roleNameOrdinal < 0 || reader.IsDBNull(roleNameOrdinal) ? null : reader.GetString(roleNameOrdinal),
                Email = emailOrdinal < 0 || reader.IsDBNull(emailOrdinal) ? null : reader.GetString(emailOrdinal),
                Phone = phoneOrdinal < 0 || reader.IsDBNull(phoneOrdinal) ? null : reader.GetString(phoneOrdinal),
                Department = departmentOrdinal < 0 || reader.IsDBNull(departmentOrdinal) ? null : reader.GetString(departmentOrdinal),
                IsActive = reader.GetBoolean("is_active")
            });
        }

        return result;
    }

    public async Task<int> CountRolesByCodeAsync(string roleCode, int? excludeRoleId, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT COUNT(1)
            FROM sys_role
            WHERE role_code = @roleCode
              AND (@excludeRoleId IS NULL OR role_id <> @excludeRoleId);
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@roleCode", roleCode);
        cmd.Parameters.AddWithValue("@excludeRoleId", excludeRoleId.HasValue ? excludeRoleId.Value : DBNull.Value);

        var scalar = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(scalar);
    }

    public async Task<int> CountUsersByUsernameAsync(string username, int? excludeUserId, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT COUNT(1)
            FROM sys_user
            WHERE username = @username
              AND (@excludeUserId IS NULL OR user_id <> @excludeUserId);
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@username", username);
        cmd.Parameters.AddWithValue("@excludeUserId", excludeUserId.HasValue ? excludeUserId.Value : DBNull.Value);

        var scalar = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(scalar);
    }

    public async Task<bool> RoleHasUsersAsync(int roleId, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT COUNT(1)
            FROM sys_user
            WHERE role_id = @roleId;
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@roleId", roleId);

        var scalar = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(scalar) > 0;
    }

    public async Task<int> InsertRoleAsync(SysRole role, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            INSERT INTO sys_role (role_code, role_name, is_active)
            VALUES (@roleCode, @roleName, @isActive);
            SELECT LAST_INSERT_ID();
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@roleCode", role.RoleCode);
        cmd.Parameters.AddWithValue("@roleName", role.RoleName);
        cmd.Parameters.AddWithValue("@isActive", role.IsActive ? 1 : 0);

        var scalar = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(scalar);
    }

    public async Task<bool> UpdateRoleAsync(SysRole role, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            UPDATE sys_role
            SET role_code = @roleCode,
                role_name = @roleName,
                is_active = @isActive
            WHERE role_id = @roleId;
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@roleId", role.RoleId);
        cmd.Parameters.AddWithValue("@roleCode", role.RoleCode);
        cmd.Parameters.AddWithValue("@roleName", role.RoleName);
        cmd.Parameters.AddWithValue("@isActive", role.IsActive ? 1 : 0);

        var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0;
    }

    public async Task<bool> DeleteRoleAsync(int roleId, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            DELETE FROM sys_role
            WHERE role_id = @roleId;
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@roleId", roleId);

        var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0;
    }

    public async Task<int> InsertUserAsync(SysUser user, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            INSERT INTO sys_user
            (
                username,
                password_hash,
                role_id,
                email,
                phone,
                department,
                is_active
            )
            VALUES
            (
                @username,
                @passwordHash,
                @roleId,
                @email,
                @phone,
                @department,
                @isActive
            );
            SELECT LAST_INSERT_ID();
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@username", user.Username);
        cmd.Parameters.AddWithValue("@passwordHash", user.PasswordHash);
        cmd.Parameters.AddWithValue("@roleId", user.RoleId.HasValue ? user.RoleId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(user.Email) ? DBNull.Value : user.Email.Trim());
        cmd.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(user.Phone) ? DBNull.Value : user.Phone.Trim());
        cmd.Parameters.AddWithValue("@department", string.IsNullOrWhiteSpace(user.Department) ? DBNull.Value : user.Department.Trim());
        cmd.Parameters.AddWithValue("@isActive", user.IsActive ? 1 : 0);

        var scalar = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(scalar);
    }

    public async Task<bool> UpdateUserAsync(SysUser user, bool updatePassword, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = updatePassword
            ? """
                UPDATE sys_user
                SET username = @username,
                    password_hash = @passwordHash,
                    role_id = @roleId,
                    email = @email,
                    phone = @phone,
                    department = @department,
                    is_active = @isActive
                WHERE user_id = @userId;
                """
            : """
                UPDATE sys_user
                SET username = @username,
                    role_id = @roleId,
                    email = @email,
                    phone = @phone,
                    department = @department,
                    is_active = @isActive
                WHERE user_id = @userId;
                """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@userId", user.UserId);
        cmd.Parameters.AddWithValue("@username", user.Username);
        if (updatePassword)
        {
            cmd.Parameters.AddWithValue("@passwordHash", user.PasswordHash);
        }

        cmd.Parameters.AddWithValue("@roleId", user.RoleId.HasValue ? user.RoleId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(user.Email) ? DBNull.Value : user.Email.Trim());
        cmd.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(user.Phone) ? DBNull.Value : user.Phone.Trim());
        cmd.Parameters.AddWithValue("@department", string.IsNullOrWhiteSpace(user.Department) ? DBNull.Value : user.Department.Trim());
        cmd.Parameters.AddWithValue("@isActive", user.IsActive ? 1 : 0);

        var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0;
    }

    public async Task<bool> DeleteUserAsync(int userId, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            DELETE FROM sys_user
            WHERE user_id = @userId;
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@userId", userId);

        var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0;
    }

    private static int TryGetOrdinal(MySqlDataReader reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }
}
