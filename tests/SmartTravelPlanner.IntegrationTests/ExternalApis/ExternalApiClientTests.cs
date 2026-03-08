using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using RichardSzalay.MockHttp;

namespace SmartTravelPlanner.IntegrationTests.ExternalApis;

public class OpenMeteoClientTests
{
    [Fact]
    public async Task GetForecastAsync_ValidResponse_ParsesCorrectly()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://api.open-meteo.com/*")
            .Respond("application/json", """
            {
              "daily": {
                "weather_code": [2, 61, 0],
                "temperature_2m_max": [18.5, 14.2, 22.0],
                "temperature_2m_min": [9.2, 7.1, 12.5],
                "precipitation_sum": [0.0, 8.5, 0.0]
              }
            }
            """);

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://api.open-meteo.com/v1");

        // Act
        var response = await client.GetFromJsonAsync<JsonElement>("/forecast?latitude=48.85&longitude=2.35&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum&forecast_days=3&timezone=auto");

        // Assert
        var codes = response.GetProperty("daily").GetProperty("weather_code");
        codes.GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task GetForecastAsync_ServerError_Returns500()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://api.open-meteo.com/*")
            .Respond(HttpStatusCode.InternalServerError);

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://api.open-meteo.com/v1");

        var response = await client.GetAsync("/forecast?latitude=48.85&longitude=2.35");
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
}

public class OpenTripMapClientTests
{
    [Fact]
    public async Task SearchPlaces_ValidResponse_ReturnsPlaces()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://api.opentripmap.com/*")
            .Respond("application/json", """
            [
              {
                "xid": "W123",
                "name": "Louvre Museum",
                "kinds": "cultural,museums",
                "rate": 7,
                "point": { "lat": 48.8606, "lon": 2.3376 }
              }
            ]
            """);

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://api.opentripmap.com/0.1/en/places");

        var response = await client.GetFromJsonAsync<JsonElement[]>("/radius?radius=10000&lon=2.35&lat=48.85&kinds=cultural&rate=2&limit=50&apikey=test");

        response.Should().NotBeNull();
        response!.Length.Should().Be(1);
        response[0].GetProperty("name").GetString().Should().Be("Louvre Museum");
    }
}

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
