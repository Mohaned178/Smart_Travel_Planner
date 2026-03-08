using SmartTravelPlanner.Domain.ValueObjects;

namespace SmartTravelPlanner.Domain.Interfaces;

/// <summary>
/// Client for computing distance/travel time matrices (OpenRouteService).
/// </summary>
public interface IRoutingClient
{
    Task<DistanceMatrixResult> GetDistanceMatrixAsync(IReadOnlyList<Coordinates> locations, CancellationToken ct = default);
}

/// <summary>Result of a distance matrix operation.</summary>
public record DistanceMatrixResult(decimal[][] DistancesKm, decimal[][] DurationsMinutes);

/// <summary>Travel distance/time between two points.</summary>
public record DistanceResult(decimal DistanceKm, decimal DurationMinutes);
