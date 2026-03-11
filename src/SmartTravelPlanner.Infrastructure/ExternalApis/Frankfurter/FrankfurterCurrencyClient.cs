using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartTravelPlanner.Domain.Interfaces;

namespace SmartTravelPlanner.Infrastructure.ExternalApis.Frankfurter;

public class FrankfurterCurrencyClient : ICurrencyClient
{
    private readonly HttpClient _http;
    private readonly ILogger<FrankfurterCurrencyClient> _logger;

    public FrankfurterCurrencyClient(HttpClient http, IConfiguration config, ILogger<FrankfurterCurrencyClient> logger)
    {
        _http = http;
        _logger = logger;
        var baseUrl = config["ExternalApis:Frankfurter:BaseUrl"] ?? "https://api.frankfurter.app/";
        if (!baseUrl.EndsWith('/')) baseUrl += "/";
        _http.BaseAddress = new Uri(baseUrl);
    }

    public async Task<decimal?> GetExchangeRateAsync(string fromCurrency, string toCurrency, CancellationToken ct = default)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
            return 1m;

        _logger.LogInformation("Fetching exchange rate {From} → {To}", fromCurrency, toCurrency);

        try
        {
            var response = await _http.GetFromJsonAsync<FrankfurterResponse>(
                $"latest?from={fromCurrency.ToUpperInvariant()}&to={toCurrency.ToUpperInvariant()}", ct);

            if (response?.Rates is null || !response.Rates.TryGetValue(toCurrency.ToUpperInvariant(), out var rate))
            {
                _logger.LogWarning("Exchange rate not found for {From} → {To}", fromCurrency, toCurrency);
                return null;
            }

            _logger.LogInformation("Exchange rate {From} → {To}: {Rate}", fromCurrency, toCurrency, rate);
            return rate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch exchange rate");
            return null;
        }
    }

    private record FrankfurterResponse
    {
        [JsonPropertyName("rates")] public Dictionary<string, decimal>? Rates { get; init; }
    }
}
