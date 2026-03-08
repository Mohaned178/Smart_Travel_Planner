using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartTravelPlanner.Domain.Entities;
using SmartTravelPlanner.Domain.Interfaces;
using SmartTravelPlanner.Domain.ValueObjects;

namespace SmartTravelPlanner.Infrastructure.ExternalApis.OpenTripMap;

public class OpenTripMapPlacesClient : IPlacesClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly ILogger<OpenTripMapPlacesClient> _logger;

    private static readonly Dictionary<string, string> CategoryMapping = new()
    {
        ["cultural"] = "cultural",
        ["natural"] = "natural",
        ["food"] = "foods",
        ["amusements"] = "amusements",
        ["shops"] = "shops",
        ["sport"] = "sport"
    };

    private static readonly HashSet<string> IndoorCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "museums", "theatres_and_entertainments", "shops", "religion",
        "foods", "banks", "other"
    };

    public OpenTripMapPlacesClient(HttpClient http, IConfiguration config, ILogger<OpenTripMapPlacesClient> logger)
    {
        _http = http;
        _logger = logger;
        _apiKey = config["ExternalApis:OpenTripMap:ApiKey"] ?? "";
        _http.BaseAddress = new Uri(config["ExternalApis:OpenTripMap:BaseUrl"] ?? "https://api.opentripmap.com/0.1/en/places");
    }

    public async Task<IReadOnlyList<Place>> SearchPlacesAsync(Coordinates coordinates, int radiusMeters,
        IEnumerable<string> categories, CancellationToken ct = default)
    {
        var mappedCategories = categories
            .Select(c => CategoryMapping.GetValueOrDefault(c.ToLowerInvariant(), c))
            .Distinct();

        var kinds = string.Join(",", mappedCategories);

        _logger.LogInformation("Searching places near {Lat},{Lon} radius={Radius}m kinds={Kinds}",
            coordinates.Latitude, coordinates.Longitude, radiusMeters, kinds);

        try
        {
            var url = string.Create(CultureInfo.InvariantCulture,
                $"/radius?radius={radiusMeters}&lon={coordinates.Longitude}&lat={coordinates.Latitude}" +
                $"&kinds={kinds}&limit=50&apikey={_apiKey}");

            var results = await _http.GetFromJsonAsync<OpenTripMapFeature[]>(url, ct);
            if (results is null || results.Length == 0)
            {
                _logger.LogWarning("No places found");
                return [];
            }

            var places = new List<Place>();
            foreach (var feature in results.Where(f => !string.IsNullOrEmpty(f.Name)))
            {
                var detail = await GetPlaceDetailAsync(feature.Xid, ct);

                places.Add(new Place
                {
                    ExternalId = feature.Xid,
                    Name = feature.Name,
                    Address = detail?.Address ?? "",
                    Latitude = feature.Point?.Lat ?? 0,
                    Longitude = feature.Point?.Lon ?? 0,
                    Category = feature.Kinds?.Split(',').FirstOrDefault() ?? "other",
                    IsIndoor = IsIndoorPlace(feature.Kinds),
                    EstimatedCost = 0,
                    TypicalVisitMinutes = 60,
                    Rating = feature.Rate.HasValue ? (decimal)feature.Rate.Value : null,
                    Description = detail?.Description,
                    ImageUrl = detail?.Image,
                    CachedAt = DateTime.UtcNow
                });
            }

            _logger.LogInformation("Found {Count} places", places.Count);
            return places;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search places");
            return [];
        }
    }

    private async Task<PlaceDetail?> GetPlaceDetailAsync(string xid, CancellationToken ct)
    {
        try
        {
            return await _http.GetFromJsonAsync<PlaceDetail>($"/xid/{xid}?apikey={_apiKey}", ct);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsIndoorPlace(string? kinds)
    {
        if (string.IsNullOrEmpty(kinds)) return false;
        return kinds.Split(',').Any(k => IndoorCategories.Contains(k.Trim()));
    }

    private record OpenTripMapFeature
    {
        [JsonPropertyName("xid")] public string Xid { get; init; } = "";
        [JsonPropertyName("name")] public string Name { get; init; } = "";
        [JsonPropertyName("kinds")] public string? Kinds { get; init; }
        [JsonPropertyName("rate")] public double? Rate { get; init; }
        [JsonPropertyName("point")] public PointData? Point { get; init; }
    }

    private record PointData
    {
        [JsonPropertyName("lat")] public decimal Lat { get; init; }
        [JsonPropertyName("lon")] public decimal Lon { get; init; }
    }

    private record PlaceDetail
    {
        [JsonPropertyName("address")] public string? Address { get; init; }
        [JsonPropertyName("wikipedia_extracts")] public WikipediaExtracts? Wikipedia { get; init; }
        [JsonPropertyName("image")] public string? Image { get; init; }
        public string? Description => Wikipedia?.Text;
    }

    private record WikipediaExtracts
    {
        [JsonPropertyName("text")] public string? Text { get; init; }
    }
}
