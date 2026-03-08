using SmartTravelPlanner.Domain.Entities;

namespace SmartTravelPlanner.Domain.Interfaces;

/// <summary>
/// Repository for user lookup operations.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
}
