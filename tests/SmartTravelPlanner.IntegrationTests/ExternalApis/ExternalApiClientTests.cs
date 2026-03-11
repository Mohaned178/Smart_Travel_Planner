using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using RichardSzalay.MockHttp;

namespace SmartTravelPlanner.IntegrationTests.ExternalApis;



public class OpenRouteServiceClientTests
{
    [Fact]
    public async Task GetDistanceMatrix_ValidResponse_ParsesCorrectly()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://api.openrouteservice.org/*")
            .Respond("application/json", """
            {
              "distances": [[0, 1500, 3000], [1500, 0, 2000], [3000, 2000, 0]],
              "durations": [[0, 300, 600], [300, 0, 400], [600, 400, 0]]
            }
            """);

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://api.openrouteservice.org");

        var response = await client.GetFromJsonAsync<JsonElement>("/v2/matrix/driving-car?test=true");

        response.GetProperty("distances").GetArrayLength().Should().Be(3);
        response.GetProperty("durations").GetArrayLength().Should().Be(3);
    }
}

public class NominatimClientTests
{
    [Fact]
    public async Task Geocode_ValidResponse_ReturnsCoordinates()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://nominatim.openstreetmap.org/*")
            .Respond("application/json", """
            [
              {
                "lat": "48.8534",
                "lon": "2.3488",
                "display_name": "Paris, Ile-de-France, France"
              }
            ]
            """);

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://nominatim.openstreetmap.org");
        client.DefaultRequestHeaders.Add("User-Agent", "SmartTravelPlanner/1.0");

        var response = await client.GetFromJsonAsync<JsonElement[]>("/search?q=Paris&format=json&limit=1");

        response.Should().NotBeNull();
        response![0].GetProperty("lat").GetString().Should().Be("48.8534");
    }
}

public class FrankfurterClientTests
{
    [Fact]
    public async Task GetExchangeRate_ValidResponse_ReturnsRate()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://api.frankfurter.app/*")
            .Respond("application/json", """
            {
              "amount": 1.0,
              "base": "EUR",
              "date": "2023-10-01",
              "rates": {
                "USD": 1.05
              }
            }
            """);

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://api.frankfurter.app");

        var response = await client.GetFromJsonAsync<JsonElement>("/latest?from=EUR&to=USD");

        response.GetProperty("rates").GetProperty("USD").GetDecimal().Should().Be(1.05m);
    }
}
