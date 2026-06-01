using backend.Entities;
using MySqlConnector;

namespace backend.Repositories;

public sealed class AuthRepository : IAuthRepository
{
    private readonly string _connectionString;

    public AuthRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string is not configured.");
    }

    public async Task<(SysUser User, SysRole? Role)?> GetUserWithRoleByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                u.user_id,
                u.username,
                u.password_hash,
                u.role_id,
                r.role_code,
                r.role_name,
                u.email,
                u.phone,
                u.department,
                u.is_active
            FROM sys_user u
            LEFT JOIN sys_role r ON r.role_id = u.role_id
            WHERE u.username = @username
            LIMIT 1;
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@username", username);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var user = ReadUser(reader);
        var role = ReadRole(reader);
        return (user, role);
    }

    public async Task<(SysUser User, SysRole? Role)?> GetUserWithRoleAsync(int? userId, string? username, CancellationToken cancellationToken)
    {
        if (userId is null && string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                u.user_id,
                u.username,
                u.password_hash,
                u.role_id,
                r.role_code,
                r.role_name,
                u.email,
                u.phone,
                u.department,
                u.is_active
            FROM sys_user u
            LEFT JOIN sys_role r ON r.role_id = u.role_id
            WHERE (@userId IS NOT NULL AND u.user_id = @userId)
               OR (@username IS NOT NULL AND u.username = @username)
            LIMIT 1;
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@userId", userId.HasValue ? userId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@username", string.IsNullOrWhiteSpace(username) ? DBNull.Value : username.Trim());

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var user = ReadUser(reader);
        var role = ReadRole(reader);
        return (user, role);
    }

    public async Task<int> CountUsersByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT COUNT(1)
            FROM sys_user
            WHERE username = @username;
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@username", username);

        var scalar = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(scalar);
    }

    public async Task<(int RoleId, string RoleName)?> GetActiveRoleByCodeAsync(string roleCode, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT role_id, role_name
            FROM sys_role
            WHERE role_code = @roleCode AND is_active = 1
            LIMIT 1;
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@roleCode", roleCode);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return (reader.GetInt32("role_id"), reader.GetString("role_name"));
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

    private static SysUser ReadUser(MySqlDataReader reader)
    {
        var roleIdOrdinal = reader.GetOrdinal("role_id");
        var emailOrdinal = TryGetOrdinal(reader, "email");
        var phoneOrdinal = TryGetOrdinal(reader, "phone");
        var departmentOrdinal = TryGetOrdinal(reader, "department");
        var isActiveOrdinal = reader.GetOrdinal("is_active");

        return new SysUser
        {
            UserId = reader.GetInt32("user_id"),
            Username = reader.GetString("username"),
            PasswordHash = reader.GetString("password_hash"),
            RoleId = reader.IsDBNull(roleIdOrdinal) ? null : reader.GetInt32(roleIdOrdinal),
            Email = emailOrdinal < 0 || reader.IsDBNull(emailOrdinal) ? null : reader.GetString(emailOrdinal),
            Phone = phoneOrdinal < 0 || reader.IsDBNull(phoneOrdinal) ? null : reader.GetString(phoneOrdinal),
            Department = departmentOrdinal < 0 || reader.IsDBNull(departmentOrdinal) ? null : reader.GetString(departmentOrdinal),
            IsActive = !reader.IsDBNull(isActiveOrdinal) && reader.GetBoolean(isActiveOrdinal)
        };
    }

    private static SysRole? ReadRole(MySqlDataReader reader)
    {
        var roleCodeOrdinal = TryGetOrdinal(reader, "role_code");
        var roleNameOrdinal = TryGetOrdinal(reader, "role_name");

        if (roleCodeOrdinal < 0 || reader.IsDBNull(roleCodeOrdinal))
        {
            return null;
        }

        return new SysRole
        {
            RoleId = reader.IsDBNull(reader.GetOrdinal("role_id")) ? 0 : reader.GetInt32("role_id"),
            RoleCode = reader.GetString(roleCodeOrdinal),
            RoleName = roleNameOrdinal < 0 || reader.IsDBNull(roleNameOrdinal) ? string.Empty : reader.GetString(roleNameOrdinal)
        };
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
