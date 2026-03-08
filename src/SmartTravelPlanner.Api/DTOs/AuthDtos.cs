namespace SmartTravelPlanner.Api.DTOs;

public record RegisterRequest(string Email, string Password, string? DisplayName);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string AccessToken, DateTime ExpiresAt, string TokenType = "Bearer");

public record UserResponse(Guid UserId, string Email, string? DisplayName);
