namespace SmartTravelPlanner.Application.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(Guid userId, string email, string? displayName);
}
