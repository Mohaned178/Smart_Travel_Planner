using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartTravelPlanner.Domain.Interfaces;
using SmartTravelPlanner.Domain.ValueObjects;
using System.Globalization;

namespace SmartTravelPlanner.Infrastructure.ExternalApis.OpenWeather;

public class OpenWeatherClient : IWeatherClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<OpenWeatherClient> _logger;

    public OpenWeatherClient(HttpClient httpClient, IConfiguration configuration, ILogger<OpenWeatherClient> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["ExternalApis:OpenWeather:ApiKey"] ?? string.Empty;
        _logger = logger;
    }

    public async Task<IReadOnlyList<WeatherForecast>> GetForecastAsync(Coordinates coordinates, int days, CancellationToken ct = default)
    {
        var url = $"forecast?lat={coordinates.Latitude.ToString(CultureInfo.InvariantCulture)}&lon={coordinates.Longitude.ToString(CultureInfo.InvariantCulture)}&appid={_apiKey}&units=metric";
        
        try
        {
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            var forecasts = new List<WeatherForecast>();

            if (json.TryGetProperty("list", out var listElement) && listElement.ValueKind == JsonValueKind.Array)
            {
                var dailyData = new Dictionary<string, (int code, decimal maxT, decimal minT, decimal rain)>();
                
                foreach (var item in listElement.EnumerateArray())
                {
                    if (item.TryGetProperty("dt", out var dtElement))
                    {
                        var unixTime = dtElement.GetInt64();
                        var date = DateTimeOffset.FromUnixTimeSeconds(unixTime).Date.ToString("yyyy-MM-dd");
                        
                        var maxTemp = 0m;
                        var minTemp = 0m;
                        if (item.TryGetProperty("main", out var main))
                        {
                            maxTemp = main.TryGetProperty("temp_max", out var tMax) ? tMax.GetDecimal() : 0m;
                            minTemp = main.TryGetProperty("temp_min", out var tMin) ? tMin.GetDecimal() : 0m;
                        }

                        var code = 0;
                        if (item.TryGetProperty("weather", out var weather) && weather.ValueKind == JsonValueKind.Array && weather.GetArrayLength() > 0)
                        {
                            code = weather[0].TryGetProperty("id", out var wId) ? wId.GetInt32() : 0;
                        }

                        var rain = 0m;
                        if (item.TryGetProperty("rain", out var rainElement) && rainElement.TryGetProperty("3h", out var rain3h))
                        {
                            rain = rain3h.GetDecimal();
                        }

                        if (!dailyData.ContainsKey(date))
                        {
                            dailyData[date] = (code, maxTemp, minTemp, rain);
                        }
                        else
                        {
                            var existing = dailyData[date];
                            dailyData[date] = (
                                existing.code, 
                                Math.Max(existing.maxT, maxTemp), 
                                Math.Min(existing.minT, minTemp), 
                                existing.rain + rain
                            );
                        }
                    }
                }

                foreach (var kvp in dailyData.Values.Take(days))
                {
                    forecasts.Add(new WeatherForecast(kvp.code, kvp.maxT, kvp.minT, kvp.rain));
                }
            }

            while (forecasts.Count < days && forecasts.Count > 0)
            {
                forecasts.Add(forecasts.Last());
            }

            return forecasts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch weather from OpenWeather for {Latitude}, {Longitude}", coordinates.Latitude, coordinates.Longitude);
            return Array.Empty<WeatherForecast>();
        }
    }
}
