using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using SmartTravelPlanner.Domain.ValueObjects;
using SmartTravelPlanner.Infrastructure.ExternalApis.OpenWeather;

namespace SmartTravelPlanner.IntegrationTests.ExternalApis;

public class OpenWeatherClientTests
{
    [Fact]
    public async Task GetForecastAsync_ValidResponse_ParsesCorrectly()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://api.openweathermap.org/data/2.5/forecast*")
            .Respond("application/json", """
            {
              "list": [
                {
                  "dt": 1600000000,
                  "main": {
                    "temp_max": 22.5,
                    "temp_min": 15.0
                  },
                  "weather": [
                    { "id": 800 }
                  ],
                  "rain": { "3h": 0.0 }
                },
                {
                  "dt": 1600086400,
                  "main": {
                    "temp_max": 18.2,
                    "temp_min": 10.1
                  },
                  "weather": [
                    { "id": 500 }
                  ],
                  "rain": { "3h": 2.5 }
                }
              ]
            }
            """);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api.openweathermap.org/data/2.5/");
        
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["ExternalApis:OpenWeather:ApiKey"]).Returns("test-api-key");
        
        var mockLogger = new Mock<ILogger<OpenWeatherClient>>();

        var client = new OpenWeatherClient(httpClient, mockConfig.Object, mockLogger.Object);
        var coords = new Coordinates(48.8584m, 2.2945m);

        var result = await client.GetForecastAsync(coords, 2);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        
        result[0].MaxTemp.Should().Be(22.5m);
        result[0].MinTemp.Should().Be(15.0m);
        result[0].Code.Should().Be(800);
        result[0].Precipitation.Should().Be(0.0m);

        result[1].MaxTemp.Should().Be(18.2m);
        result[1].MinTemp.Should().Be(10.1m);
        result[1].Code.Should().Be(500);
        result[1].Precipitation.Should().Be(2.5m);
    }
}
