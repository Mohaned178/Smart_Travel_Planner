using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;

namespace SmartTravelPlanner.Infrastructure.Extensions;

/// <summary>
/// Polly resilience policies for external API HttpClients.
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Adds standard resilience policies: retry with exponential backoff,
    /// circuit breaker, and timeout.
    /// </summary>
    public static IHttpClientBuilder AddResiliencePolicies(this IHttpClientBuilder builder)
    {
        return builder
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy())
            .AddPolicyHandler(Policy.TimeoutAsync<System.Net.Http.HttpResponseMessage>(TimeSpan.FromSeconds(10)));
    }

    private static IAsyncPolicy<System.Net.Http.HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static IAsyncPolicy<System.Net.Http.HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }
}
