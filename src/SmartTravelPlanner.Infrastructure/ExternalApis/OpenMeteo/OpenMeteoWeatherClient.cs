using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartTravelPlanner.Domain.Interfaces;
using SmartTravelPlanner.Domain.ValueObjects;

namespace SmartTravelPlanner.Infrastructure.ExternalApis.OpenMeteo;

public class OpenMeteoWeatherClient : IWeatherClient
{
    private readonly HttpClient _http;
    private readonly ILogger<OpenMeteoWeatherClient> _logger;

    public OpenMeteoWeatherClient(HttpClient http, IConfiguration config, ILogger<OpenMeteoWeatherClient> logger)
    {
        _http = http;
        _logger = logger;
        _http.BaseAddress = new Uri(config["ExternalApis:OpenMeteo:BaseUrl"] ?? "https://api.open-meteo.com/v1");
    }

    public async Task<IReadOnlyList<WeatherForecast>> GetForecastAsync(Coordinates coordinates, int days, CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching weather forecast for {Lat},{Lon} - {Days} days",
            coordinates.Latitude, coordinates.Longitude, days);

        try
        {
            var url = $"/forecast?latitude={coordinates.Latitude}&longitude={coordinates.Longitude}" +
                      $"&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum" +
                      $"&forecast_days={Math.Min(days, 16)}&timezone=auto";

            var response = await _http.GetFromJsonAsync<OpenMeteoResponse>(url, ct);
            if (response?.Daily is null)
            {
                _logger.LogWarning("Weather API returned no daily data");
                return [];
            }

            var forecasts = new List<WeatherForecast>();
            var count = Math.Min(days, response.Daily.WeatherCode?.Length ?? 0);

            for (int i = 0; i < count; i++)
            {
                forecasts.Add(new WeatherForecast(
                    Code: response.Daily.WeatherCode?[i] ?? 0,
                    MaxTemp: response.Daily.TemperatureMax?[i] ?? 0m,
                    MinTemp: response.Daily.TemperatureMin?[i] ?? 0m,
                    Precipitation: response.Daily.PrecipitationSum?[i] ?? 0m
                ));
            }

            _logger.LogInformation("Retrieved {Count} day forecasts", forecasts.Count);
            return forecasts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch weather forecast");
            return [];
        }
    }

    private record OpenMeteoResponse
    {
        [JsonPropertyName("daily")] public DailyData? Daily { get; init; }
    }

    private record DailyData
    {
        [JsonPropertyName("weather_code")] public int[]? WeatherCode { get; init; }
        [JsonPropertyName("temperature_2m_max")] public decimal[]? TemperatureMax { get; init; }
        [JsonPropertyName("temperature_2m_min")] public decimal[]? TemperatureMin { get; init; }
        [JsonPropertyName("precipitation_sum")] public decimal[]? PrecipitationSum { get; init; }
    }
}
