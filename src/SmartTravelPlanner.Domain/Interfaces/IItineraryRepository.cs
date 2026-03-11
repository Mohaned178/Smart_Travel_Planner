using SmartTravelPlanner.Domain.Entities;

namespace SmartTravelPlanner.Domain.Interfaces;

public interface IItineraryRepository
{
    Task<Itinerary?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Itinerary?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Itinerary>> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<int> GetCountByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Itinerary itinerary, CancellationToken ct = default);
    Task UpdateAsync(Itinerary itinerary, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task DeleteStaleDraftsAsync(int expirationHours, CancellationToken ct = default);
}
