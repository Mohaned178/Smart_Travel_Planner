using SmartTravelPlanner.Domain.ValueObjects;

namespace SmartTravelPlanner.Domain.Interfaces;

/// <summary>
/// Client for geocoding city names to coordinates (Nominatim).
/// </summary>
public interface IGeocodingClient
{
    Task<GeocodingResult?> GeocodeAsync(string cityName, CancellationToken ct = default);
}

/// <summary>Result of a geocoding operation.</summary>
public record GeocodingResult(Coordinates Coordinates, string DisplayName, string? CountryName, string? Timezone);
