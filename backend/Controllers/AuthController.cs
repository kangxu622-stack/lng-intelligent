using backend.Dtos;
using backend.Dtos.Auth;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthAppService _authService;

    public AuthController(IAuthAppService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginInput request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return Ok(ApiResponse<LoginResult>.Error(400, "Login request body must be provided."));
        }

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Ok(ApiResponse<LoginResult>.Error(400, "Username and password are required."));
        }

        try
        {
            var result = await _authService.LoginAsync(request.Username.Trim(), request.Password, cancellationToken);
            if (result == null)
            {
                return Ok(ApiResponse<LoginResult>.Error(401, "Invalid username or password."));
            }

            return Ok(ApiResponse<LoginResult>.Success(result, "登录成功"));
        }
        catch (MySqlException ex)
        {
            return Ok(ApiResponse<LoginResult>.Error(500, $"Database error: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<LoginResult>.Error(500, ex.Message));
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterInput request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return Ok(ApiResponse<RegisterResult>.Error(400, "Register request body must be provided."));
        }

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Ok(ApiResponse<RegisterResult>.Error(400, "Username and password are required."));
        }

        try
        {
            var result = await _authService.RegisterAsync(request, cancellationToken);
            return Ok(ApiResponse<RegisterResult>.Success(result, "注册成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<RegisterResult>.Error(400, ex.Message));
        }
        catch (MySqlException ex)
        {
            return Ok(ApiResponse<RegisterResult>.Error(500, $"Database error: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<RegisterResult>.Error(500, ex.Message));
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutInput? request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.LogoutAsync(request ?? new LogoutInput(), cancellationToken);
            return Ok(ApiResponse<LogoutResult>.Success(result, "登出成功"));
        }
        catch (MySqlException ex)
        {
            return Ok(ApiResponse<LogoutResult>.Error(500, $"Database error: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<LogoutResult>.Error(500, ex.Message));
        }
    }

    [HttpGet("user-info")]
    public async Task<IActionResult> GetUserInfo([FromQuery] int? userId, [FromQuery] string? username, CancellationToken cancellationToken)
    {
        if (userId is null && string.IsNullOrWhiteSpace(username))
        {
            return Ok(ApiResponse<UserInfo>.Error(400, "userId or username must be provided."));
        }

        try
        {
            var result = await _authService.GetUserInfoAsync(userId, username, cancellationToken);
            if (result == null)
            {
                return Ok(ApiResponse<UserInfo>.Error(404, "User not found."));
            }

            return Ok(ApiResponse<UserInfo>.Success(result, "获取用户信息成功"));
        }
        catch (MySqlException ex)
        {
            return Ok(ApiResponse<UserInfo>.Error(500, $"Database error: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<UserInfo>.Error(500, ex.Message));
        }
    }
}
