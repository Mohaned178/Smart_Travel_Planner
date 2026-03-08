namespace SmartTravelPlanner.Domain.Interfaces;

/// <summary>
/// Client for fetching exchange rates (Frankfurter).
/// </summary>
public interface ICurrencyClient
{
    Task<decimal?> GetExchangeRateAsync(string fromCurrency, string toCurrency, CancellationToken ct = default);
}
