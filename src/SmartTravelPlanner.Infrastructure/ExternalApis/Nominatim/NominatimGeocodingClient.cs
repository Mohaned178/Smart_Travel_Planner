using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartTravelPlanner.Domain.Interfaces;
using SmartTravelPlanner.Domain.ValueObjects;

namespace SmartTravelPlanner.Infrastructure.ExternalApis.Nominatim;

public class NominatimGeocodingClient : IGeocodingClient
{
    private readonly HttpClient _http;
    private readonly ILogger<NominatimGeocodingClient> _logger;
    private static readonly SemaphoreSlim RateLimiter = new(1, 1);
    private static DateTime _lastRequest = DateTime.MinValue;

    public NominatimGeocodingClient(HttpClient http, IConfiguration config, ILogger<NominatimGeocodingClient> logger)
    {
        _http = http;
        _logger = logger;
        var baseUrl = config["ExternalApis:Nominatim:BaseUrl"] ?? "https://nominatim.openstreetmap.org/";
        if (!baseUrl.EndsWith('/')) baseUrl += "/";
        _http.BaseAddress = new Uri(baseUrl);
        _http.DefaultRequestHeaders.Add("User-Agent", config["ExternalApis:Nominatim:UserAgent"] ?? "SmartTravelPlanner/1.0");
    }

    public async Task<GeocodingResult?> GeocodeAsync(string cityName, CancellationToken ct = default)
    {
        await EnforceRateLimitAsync(ct);

        _logger.LogInformation("Geocoding city: {CityName}", cityName);

        var response = await _http.GetFromJsonAsync<NominatimResult[]>(
            $"search?q={Uri.EscapeDataString(cityName)}&format=json&limit=1&addressdetails=1", ct);

        var result = response?.FirstOrDefault();
        if (result is null)
        {
            _logger.LogWarning("Geocoding returned no results for city: {CityName}", cityName);
            return null;
        }

        var coordinates = new Coordinates(
            decimal.Parse(result.Lat, CultureInfo.InvariantCulture),
            decimal.Parse(result.Lon, CultureInfo.InvariantCulture));
        var country = result.Address?.Country;

        _logger.LogInformation("Geocoded {CityName} to {Lat},{Lon}", cityName, result.Lat, result.Lon);
        return new GeocodingResult(coordinates, result.DisplayName, country, null);
    }

    private static async Task EnforceRateLimitAsync(CancellationToken ct)
    {
        await RateLimiter.WaitAsync(ct);
        try
        {
            var elapsed = DateTime.UtcNow - _lastRequest;
            if (elapsed < TimeSpan.FromSeconds(1))
                await Task.Delay(TimeSpan.FromSeconds(1) - elapsed, ct);
            _lastRequest = DateTime.UtcNow;
        }
        finally
        {
            RateLimiter.Release();
        }
    }

    private record NominatimResult
    {
        [JsonPropertyName("lat")] public string Lat { get; init; } = "";
        [JsonPropertyName("lon")] public string Lon { get; init; } = "";
        [JsonPropertyName("display_name")] public string DisplayName { get; init; } = "";
        [JsonPropertyName("address")] public NominatimAddress? Address { get; init; }
    }

    private record NominatimAddress
    {
        [JsonPropertyName("country")] public string? Country { get; init; }
    }
}
