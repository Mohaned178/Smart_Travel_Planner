using SmartTravelPlanner.Domain.ValueObjects;

namespace SmartTravelPlanner.Domain.Interfaces;

public interface IGeocodingClient
{
    Task<GeocodingResult?> GeocodeAsync(string cityName, CancellationToken ct = default);
}

public record GeocodingResult(Coordinates Coordinates, string DisplayName, string? CountryName, string? Timezone);
