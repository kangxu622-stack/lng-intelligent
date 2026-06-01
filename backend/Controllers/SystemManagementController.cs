using backend.Dtos;
using backend.Dtos.SystemManagement;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace backend.Controllers;

[ApiController]
[Route("api/system-management")]
public sealed class SystemManagementController : ControllerBase
{
    private readonly ISystemManagementAppService _service;

    public SystemManagementController(ISystemManagementAppService service)
    {
        _service = service;
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles([FromQuery] string? keyword, [FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetRolesAsync(keyword, isActive, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<RoleListItem>>.Success(result, "Get roles success."));
        }
        catch (MySqlException ex)
        {
            return Ok(ApiResponse<IReadOnlyList<RoleListItem>>.Error(500, $"Database error: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<IReadOnlyList<RoleListItem>>.Error(500, ex.Message));
        }
    }

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] RoleUpsertInput input, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.CreateRoleAsync(input, cancellationToken);
            return Ok(ApiResponse<RoleListItem>.Success(result, "Create role success."));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<RoleListItem>.Error(400, ex.Message));
        }
        catch (MySqlException ex)
        {
            return Ok(ApiResponse<RoleListItem>.Error(500, $"Database error: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<RoleListItem>.Error(500, ex.Message));
        }
    }

    [HttpPut("roles/{roleId:int}")]
    public async Task<IActionResult> UpdateRole(int roleId, [FromBody] RoleUpsertInput input, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.UpdateRoleAsync(roleId, input, cancellationToken);
            return Ok(ApiResponse<RoleListItem>.Success(result, "Update role success."));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<RoleListItem>.Error(400, ex.Message));
        }
        catch (MySqlException ex)
        {
            return Ok(ApiResponse<RoleListItem>.Error(500, $"Database error: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<RoleListItem>.Error(500, ex.Message));
        }
    }

    [HttpDelete("roles/{roleId:int}")]
    public async Task<IActionResult> DeleteRole(int roleId, CancellationToken cancellationToken)
    {
        try
        {
            await _service.DeleteRoleAsync(roleId, cancellationToken);
            return Ok(ApiResponse.Success(message: "Delete role success."));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse.Error(400, ex.Message));
        }
        catch (MySqlException ex)
        {
            return Ok(ApiResponse.Error(500, $"Database error: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? username, [FromQuery] string? phone, [FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetUsersAsync(username, phone, isActive, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<UserListItem>>.Success(result, "Get users success."));
        }
        catch (MySqlException ex)
        {
            return Ok(ApiResponse<IReadOnlyList<UserListItem>>.Error(500, $"Database error: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<IReadOnlyList<UserListItem>>.Error(500, ex.Message));
        }
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] UserUpsertInput input, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.CreateUserAsync(input, cancellationToken);
            return Ok(ApiResponse<UserListItem>.Success(result, "Create user success."));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<UserListItem>.Error(400, ex.Message));
        }
        catch (MySqlException ex)
        {
            return Ok(ApiResponse<UserListItem>.Error(500, $"Database error: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<UserListItem>.Error(500, ex.Message));
        }
    }

    [HttpPut("users/{userId:int}")]
    public async Task<IActionResult> UpdateUser(int userId, [FromBody] UserUpsertInput input, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.UpdateUserAsync(userId, input, cancellationToken);
            return Ok(ApiResponse<UserListItem>.Success(result, "Update user success."));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<UserListItem>.Error(400, ex.Message));
        }
        catch (MySqlException ex)
        {
            return Ok(ApiResponse<UserListItem>.Error(500, $"Database error: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<UserListItem>.Error(500, ex.Message));
        }
    }

    [HttpDelete("users/{userId:int}")]
    public async Task<IActionResult> DeleteUser(int userId, CancellationToken cancellationToken)
    {
        try
        {
            await _service.DeleteUserAsync(userId, cancellationToken);
            return Ok(ApiResponse.Success(message: "Delete user success."));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse.Error(400, ex.Message));
        }
        catch (MySqlException ex)
        {
            return Ok(ApiResponse.Error(500, $"Database error: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }
}
