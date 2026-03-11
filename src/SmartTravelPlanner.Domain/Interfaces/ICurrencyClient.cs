namespace SmartTravelPlanner.Domain.Interfaces;

public interface ICurrencyClient
{
    Task<decimal?> GetExchangeRateAsync(string fromCurrency, string toCurrency, CancellationToken ct = default);
}
