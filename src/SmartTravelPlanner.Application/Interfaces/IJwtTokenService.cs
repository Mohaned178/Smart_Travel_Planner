namespace SmartTravelPlanner.Application.Interfaces;

/// <summary>
/// Generates JWT tokens for authenticated users.
/// </summary>
public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(Guid userId, string email, string? displayName);
}
