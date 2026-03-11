using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartTravelPlanner.Domain.Constants;
using SmartTravelPlanner.Domain.Entities;
using SmartTravelPlanner.Domain.Interfaces;
using SmartTravelPlanner.Domain.ValueObjects;

namespace SmartTravelPlanner.Infrastructure.ExternalApis.GooglePlaces;

public class GooglePlacesClient : IPlacesClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<GooglePlacesClient> _logger;

    public GooglePlacesClient(HttpClient httpClient, IConfiguration configuration, ILogger<GooglePlacesClient> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["ExternalApis:GooglePlaces:ApiKey"] ?? string.Empty;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Place>> SearchPlacesAsync(
        Coordinates coordinates, int radiusMeters, IEnumerable<string> categories, CancellationToken ct = default)
    {
        var categoryList = categories.ToList();
        if (categoryList.Count == 0) categoryList.Add("tourist_attraction");

        var allPlaces = new Dictionary<string, Place>(StringComparer.OrdinalIgnoreCase);

        foreach (var category in categoryList)
        {
            var url = $"nearbysearch/json"
                    + $"?location={Math.Round(coordinates.Latitude, 6)},{Math.Round(coordinates.Longitude, 6)}"
                    + $"&radius={radiusMeters}"
                    + $"&type={category}"
                    + $"&key={_apiKey}";

            await FetchPlacesFromUrl(url, category, allPlaces, ct);
        }

        return allPlaces.Values.ToList();
    }

    private async Task FetchPlacesFromUrl(
        string url, string searchedCategory, Dictionary<string, Place> allPlaces, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept", "application/json");

        try
        {
            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google Places returned {Status} for category {Category}", response.StatusCode, searchedCategory);
                return;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

            if (!json.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
                return;

            foreach (var item in results.EnumerateArray())
            {
                var externalId = item.TryGetProperty("place_id", out var idEl)
                    ? idEl.GetString() ?? Guid.NewGuid().ToString()
                    : Guid.NewGuid().ToString();

                if (allPlaces.ContainsKey(externalId)) continue;

                var place = new Place { ExternalId = externalId };

                place.Name = item.TryGetProperty("name", out var nameEl)
                    ? nameEl.GetString() ?? "Unknown"
                    : "Unknown";

                if (item.TryGetProperty("geometry", out var geometry) &&
                    geometry.TryGetProperty("location", out var location))
                {
                    place.Latitude  = location.TryGetProperty("lat", out var lat) ? lat.GetDecimal() : 0;
                    place.Longitude = location.TryGetProperty("lng", out var lon) ? lon.GetDecimal() : 0;
                }

                if (item.TryGetProperty("vicinity", out var addr))
                    place.Address = addr.GetString();

                if (item.TryGetProperty("types", out var types) && types.ValueKind == JsonValueKind.Array)
                {
                    var typeList = types.EnumerateArray()
                        .Select(t => t.GetString())
                        .Where(t => t != null)
                        .Cast<string>()
                        .ToList();

                    place.AllTypes = typeList;

                    place.Category = typeList.Contains(searchedCategory, StringComparer.OrdinalIgnoreCase)
                        ? searchedCategory
                        : typeList.FirstOrDefault(t => PlaceTypeConstants.KnownActivityTypes.Contains(t))
                          ?? typeList.FirstOrDefault()
                          ?? "unknown";
                }
                else
                {
                    place.AllTypes = [searchedCategory];
                    place.Category = searchedCategory;
                }

                if (item.TryGetProperty("rating", out var ratingEl) && ratingEl.ValueKind == JsonValueKind.Number)
                    place.Rating = ratingEl.GetDecimal();

                if (item.TryGetProperty("price_level", out var priceEl) && priceEl.ValueKind == JsonValueKind.Number)
                    place.EstimatedCost = priceEl.GetInt32() * 15m;
                else
                    place.EstimatedCost = EstimateCostByCategory(place.Category);

                place.IsIndoor = PlaceTypeConstants.IsIndoor(place.Category);

                place.CachedAt = DateTime.UtcNow;
                allPlaces[externalId] = place;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch places for category {Category}", searchedCategory);
        }
    }

    private static decimal EstimateCostByCategory(string category) =>
        category.ToLowerInvariant() switch
        {
            "museum"                                   => 12m,
            "art_gallery"                              => 10m,
            "tourist_attraction"                       => 8m,
            "amusement_park" or "aquarium" or "zoo"   => 25m,
            "park" or "beach" or "church" or "cemetery"
            or "mosque" or "hindu_temple" or "synagogue" => 0m,
            "restaurant" or "meal_delivery"
            or "meal_takeaway"                         => 15m,
            "cafe" or "bakery"                         => 8m,
            "shopping_mall" or "store" or "market"     => 0m,
            _                                          => 5m
        };
}