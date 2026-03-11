using SmartTravelPlanner.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace SmartTravelPlanner.Application.Services;

public interface ICurrencyConversionService
{
    Task<(decimal ConvertedAmount, bool WasFallback)> ConvertAsync(decimal amount, string fromCurrency,
        string toCurrency, CancellationToken ct = default);
    Task<decimal?> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken ct = default);
}

public class CurrencyConversionService : ICurrencyConversionService
{
    private readonly ICurrencyClient _currencyClient;
    private readonly ILogger<CurrencyConversionService> _logger;

    public CurrencyConversionService(ICurrencyClient currencyClient, ILogger<CurrencyConversionService> logger)
    {
        _currencyClient = currencyClient;
        _logger = logger;
    }

    public async Task<(decimal ConvertedAmount, bool WasFallback)> ConvertAsync(
        decimal amount, string fromCurrency, string toCurrency, CancellationToken ct = default)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
            return (amount, false);

        var rate = await _currencyClient.GetExchangeRateAsync(fromCurrency, toCurrency, ct);
        if (rate is null)
        {
            _logger.LogWarning("Exchange rate unavailable for {From} → {To}, using source currency",
                fromCurrency, toCurrency);
            return (amount, true);
        }

        return (Math.Round(amount * rate.Value, 2), false);
    }

    public async Task<decimal?> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken ct = default)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
            return 1m;

        return await _currencyClient.GetExchangeRateAsync(fromCurrency, toCurrency, ct);
    }
}
