using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartTravelPlanner.Domain.Interfaces;
using SmartTravelPlanner.Domain.ValueObjects;

namespace SmartTravelPlanner.Infrastructure.ExternalApis.OpenRouteService;

public class OpenRouteServiceRoutingClient : IRoutingClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _profile;
    private readonly ILogger<OpenRouteServiceRoutingClient> _logger;

    public OpenRouteServiceRoutingClient(HttpClient http, IConfiguration config, ILogger<OpenRouteServiceRoutingClient> logger)
    {
        _http = http;
        _logger = logger;
        _apiKey = config["ExternalApis:OpenRouteService:ApiKey"] ?? "";
        _profile = config["ExternalApis:OpenRouteService:Profile"] ?? "driving-car";
        var baseUrl = config["ExternalApis:OpenRouteService:BaseUrl"] ?? "https://api.openrouteservice.org/";
        if (!baseUrl.EndsWith('/')) baseUrl += "/";
        _http.BaseAddress = new Uri(baseUrl);
    }

    public async Task<DistanceMatrixResult> GetDistanceMatrixAsync(IReadOnlyList<Coordinates> locations, CancellationToken ct = default)
    {
        if (locations.Count < 2)
            return new DistanceMatrixResult([], []);

        _logger.LogInformation("Fetching distance matrix for {Count} locations", locations.Count);

        try
        {
            var requestBody = new
            {
                locations = locations.Select(c => new[] { (double)c.Longitude, (double)c.Latitude }).ToArray(),
                metrics = new[] { "distance", "duration" }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("Authorization", _apiKey);

            var response = await _http.PostAsync($"v2/matrix/{_profile}", content, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OrsMatrixResponse>(cancellationToken: ct);
            if (result is null)
            {
                _logger.LogWarning("Distance matrix returned null response");
                return new DistanceMatrixResult([], []);
            }
            var distancesKm = result.Distances?.Select(row =>
                row.Select(d => d / 1000m).ToArray()).ToArray() ?? [];
            var durationsMin = result.Durations?.Select(row =>
                row.Select(d => d / 60m).ToArray()).ToArray() ?? [];

            _logger.LogInformation("Distance matrix computed successfully");
            return new DistanceMatrixResult(distancesKm, durationsMin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch distance matrix");
            return new DistanceMatrixResult([], []);
        }
    }

    private record OrsMatrixResponse
    {
        [JsonPropertyName("distances")] public decimal[][]? Distances { get; init; }
        [JsonPropertyName("durations")] public decimal[][]? Durations { get; init; }
    }
}
