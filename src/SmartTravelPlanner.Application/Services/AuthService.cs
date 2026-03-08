using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SmartTravelPlanner.Application.Interfaces;
using SmartTravelPlanner.Domain.Entities;

namespace SmartTravelPlanner.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(UserManager<User> userManager, IJwtTokenService tokenService, ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string? displayName, CancellationToken ct = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            _logger.LogWarning("Registration attempted with existing email");
            return new AuthResult(false, Error: "Email already registered");
        }

        var user = new User
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("User registration failed: {Errors}", errors);
            return new AuthResult(false, Error: errors);
        }

        _logger.LogInformation("User registered successfully");
        return new AuthResult(true, UserId: user.Id, Email: user.Email, DisplayName: user.DisplayName);
    }

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, password))
        {
            _logger.LogWarning("Login failed for email");
            return new AuthResult(false, Error: "Invalid credentials");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var (token, expiresAt) = _tokenService.GenerateToken(user.Id, user.Email!, user.DisplayName);

        _logger.LogInformation("User logged in successfully");
        return new AuthResult(true, Token: token, ExpiresAt: expiresAt,
            UserId: user.Id, Email: user.Email, DisplayName: user.DisplayName);
    }
}
