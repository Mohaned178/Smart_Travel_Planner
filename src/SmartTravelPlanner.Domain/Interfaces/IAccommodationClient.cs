using SmartTravelPlanner.Domain.ValueObjects;

namespace SmartTravelPlanner.Domain.Interfaces;

public interface IAccommodationClient
{
    Task<IReadOnlyList<AccommodationResult>> SearchAccommodationsAsync(
        Coordinates coordinates, int radiusMeters, decimal maxPricePerNight,
        CancellationToken ct = default);
}

public record AccommodationResult(
    string Name, string Address, Coordinates Coordinates,
    decimal PricePerNight, string? Type, decimal? Rating);
