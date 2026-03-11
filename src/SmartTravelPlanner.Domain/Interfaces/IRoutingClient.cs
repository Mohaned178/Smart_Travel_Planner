using SmartTravelPlanner.Domain.ValueObjects;

namespace SmartTravelPlanner.Domain.Interfaces;

public interface IRoutingClient
{
    Task<DistanceMatrixResult> GetDistanceMatrixAsync(IReadOnlyList<Coordinates> locations, CancellationToken ct = default);
}

public record DistanceMatrixResult(decimal[][] DistancesKm, decimal[][] DurationsMinutes);

public record DistanceResult(decimal DistanceKm, decimal DurationMinutes);
