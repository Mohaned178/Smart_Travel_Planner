using SmartTravelPlanner.Domain.Entities;
using SmartTravelPlanner.Domain.Enums;
using SmartTravelPlanner.Domain.ValueObjects;

namespace SmartTravelPlanner.Domain.Interfaces;

/// <summary>
/// Client for searching places of interest (OpenTripMap).
/// </summary>
public interface IPlacesClient
{
    Task<IReadOnlyList<Place>> SearchPlacesAsync(Coordinates coordinates, int radiusMeters, IEnumerable<string> categories, CancellationToken ct = default);
}
