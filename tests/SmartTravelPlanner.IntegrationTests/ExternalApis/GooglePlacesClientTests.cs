using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using SmartTravelPlanner.Domain.ValueObjects;
using SmartTravelPlanner.Infrastructure.ExternalApis.GooglePlaces;

namespace SmartTravelPlanner.IntegrationTests.ExternalApis;

public class GooglePlacesClientTests
{
    [Fact]
    public async Task SearchPlacesAsync_ValidResponse_ReturnsListOfPlaces()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://maps.googleapis.com/maps/api/place/nearbysearch/json*")
            .Respond("application/json", """
            {
              "results": [
                {
                  "place_id": "ChIJLU7jZClu5kcR4PcOOO6p3I0",
                  "name": "Eiffel Tower",
                  "vicinity": "Champ de Mars, 5 Avenue Anatole France, Paris",
                  "geometry": {
                    "location": { "lat": 48.8584, "lng": 2.2945 }
                  },
                  "types": [
                    "tourist_attraction",
                    "point_of_interest"
                  ],
                  "rating": 4.6,
                  "price_level": 2
                }
              ]
            }
            """);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://maps.googleapis.com/maps/api/place/");
        
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["ExternalApis:GooglePlaces:ApiKey"]).Returns("test-api-key");
        
        var mockLogger = new Mock<ILogger<GooglePlacesClient>>();

        var client = new GooglePlacesClient(httpClient, mockConfig.Object, mockLogger.Object);
        var coords = new Coordinates(48.8584m, 2.2945m);

        var result = await client.SearchPlacesAsync(coords, 1000, new[] { "tourist_attraction" });

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Eiffel Tower");
        result[0].ExternalId.Should().Be("ChIJLU7jZClu5kcR4PcOOO6p3I0");
        result[0].Latitude.Should().Be(48.8584m);
        result[0].Longitude.Should().Be(2.2945m);
        result[0].Category.Should().Be("tourist_attraction");
        result[0].Rating.Should().Be(4.6m);
        result[0].Address.Should().Be("Champ de Mars, 5 Avenue Anatole France, Paris");
        result[0].EstimatedCost.Should().Be(30m);
    }
}
