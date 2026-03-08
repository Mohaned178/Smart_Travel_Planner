using SmartTravelPlanner.Domain.Entities;

namespace SmartTravelPlanner.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string password, string? displayName, CancellationToken ct = default);
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default);
}

public record AuthResult(bool Success, string? Token = null, DateTime? ExpiresAt = null,
    Guid? UserId = null, string? Email = null, string? DisplayName = null, string? Error = null);
