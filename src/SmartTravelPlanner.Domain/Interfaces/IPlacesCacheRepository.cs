using SmartTravelPlanner.Domain.Entities;

namespace SmartTravelPlanner.Domain.Interfaces;

/// <summary>
/// Repository for Place cache operations with staleness checks.
/// </summary>
public interface IPlacesCacheRepository
{
    Task<Place?> GetByExternalIdAsync(string externalId, CancellationToken ct = default);
    Task UpsertAsync(Place place, CancellationToken ct = default);
    Task<IReadOnlyList<Place>> GetByExternalIdsAsync(IEnumerable<string> externalIds, CancellationToken ct = default);
}
