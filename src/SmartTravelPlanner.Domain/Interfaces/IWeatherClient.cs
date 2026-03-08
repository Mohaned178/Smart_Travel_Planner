using SmartTravelPlanner.Domain.ValueObjects;

namespace SmartTravelPlanner.Domain.Interfaces;

/// <summary>
/// Client for fetching weather forecasts (Open-Meteo).
/// </summary>
public interface IWeatherClient
{
    Task<IReadOnlyList<WeatherForecast>> GetForecastAsync(Coordinates coordinates, int days, CancellationToken ct = default);
}

/// <summary>Daily weather data from Open-Meteo.</summary>
public record WeatherForecast(int Code, decimal MaxTemp, decimal MinTemp, decimal Precipitation);
