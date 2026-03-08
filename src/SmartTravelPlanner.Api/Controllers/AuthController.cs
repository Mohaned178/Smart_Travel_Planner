using Microsoft.AspNetCore.Mvc;
using SmartTravelPlanner.Api.DTOs;
using SmartTravelPlanner.Application.Interfaces;

namespace SmartTravelPlanner.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>Register a new user account.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(request.Email, request.Password, request.DisplayName, ct);

        if (!result.Success)
        {
            if (result.Error?.Contains("already registered") == true)
                return Conflict(new ProblemDetails { Title = "Conflict", Detail = result.Error, Status = 409 });
            return BadRequest(new ProblemDetails { Title = "Bad Request", Detail = result.Error, Status = 400 });
        }

        var response = new UserResponse(result.UserId!.Value, result.Email!, result.DisplayName);
        return CreatedAtAction(nameof(Register), response);
    }

    /// <summary>Login and receive a JWT access token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password, ct);

        if (!result.Success)
            return Unauthorized(new ProblemDetails { Title = "Unauthorized", Detail = "Invalid credentials", Status = 401 });

        return Ok(new AuthResponse(result.Token!, result.ExpiresAt!.Value));
    }
}
