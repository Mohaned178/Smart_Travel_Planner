using SmartTravelPlanner.Domain.ValueObjects;

namespace SmartTravelPlanner.Domain.Interfaces;

public interface IWeatherClient
{
    Task<IReadOnlyList<WeatherForecast>> GetForecastAsync(Coordinates coordinates, int days, CancellationToken ct = default);
}

public record WeatherForecast(int Code, decimal MaxTemp, decimal MinTemp, decimal Precipitation);
