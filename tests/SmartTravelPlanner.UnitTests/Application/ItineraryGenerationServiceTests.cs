using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SmartTravelPlanner.Application.Services;
using SmartTravelPlanner.Domain.Entities;
using SmartTravelPlanner.Domain.Interfaces;
using SmartTravelPlanner.Domain.ValueObjects;

namespace SmartTravelPlanner.UnitTests.Application;

public class ItineraryGenerationServiceTests
{
    private readonly Mock<IGeocodingClient> _geocodingClient = new();
    private readonly Mock<IPlacesClient> _placesClient = new();
    private readonly Mock<IWeatherClient> _weatherClient = new();
    private readonly Mock<IRoutingClient> _routingClient = new();
    private readonly Mock<ICurrencyConversionService> _currencyService = new();
    private readonly Mock<ICostCalculationService> _costService = new();
    private readonly Mock<IItineraryRepository> _itineraryRepo = new();
    private readonly IConfiguration _config;
    private readonly ItineraryGenerationService _sut;

    public ItineraryGenerationServiceTests()
    {
        var inMemory = new Dictionary<string, string?>
        {
            ["TransportRates:PublicTransportPerKm"] = "0.50",
            ["ExternalApis:OpenTripMap:SearchRadiusMeters"] = "10000"
        };
        _config = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();

        _sut = new ItineraryGenerationService(
            _geocodingClient.Object, _placesClient.Object, _weatherClient.Object,
            _routingClient.Object, _currencyService.Object, _costService.Object,
            _itineraryRepo.Object, _config, Mock.Of<ILogger<ItineraryGenerationService>>());
    }

    [Fact]
    public async Task GenerateAsync_ValidRequest_Returns3DayPlan()
    {
        // Arrange
        SetupSuccessfulMocks(3);

        var request = CreateRequest(durationDays: 3);

        // Act
        var result = await _sut.GenerateAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Itinerary.DayPlans.Should().HaveCount(3);
        result.Itinerary.CityName.Should().Be("Paris");
        result.Itinerary.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task GenerateAsync_ActivitiesMatchInterests_CostWithinBudget()
    {
        // Arrange
        SetupSuccessfulMocks(3);

        _costService.Setup(c => c.CalculateCostBreakdown(It.IsAny<Itinerary>(), It.IsAny<decimal>()))
            .Returns(new CostBreakdown
            {
                TotalActivitiesCost = 100, TotalDiningCost = 50, TotalTransportCost = 20,
                GrandTotal = 170, RemainingBudget = 330, CurrencyCode = "USD"
            });

        var request = CreateRequest(durationDays: 3, budget: 500);

        // Act
        var result = await _sut.GenerateAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Itinerary.CostBreakdown!.GrandTotal.Should().BeLessThanOrEqualTo(500);
    }

    [Fact]
    public async Task GenerateAsync_UnrecognizedCity_ReturnsError()
    {
        // Arrange
        _geocodingClient.Setup(g => g.GeocodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GeocodingResult?)null);

        var request = CreateRequest(cityName: "NonexistentCity123");

        // Act
        var result = await _sut.GenerateAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("could not be found");
    }

    [Fact]
    public async Task GenerateAsync_NoPlacesFound_ReturnsError()
    {
        // Arrange
        _geocodingClient.Setup(g => g.GeocodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeocodingResult(new Coordinates(48.8566m, 2.3522m), "Paris", "France", "Europe/Paris"));
        _placesClient.Setup(p => p.SearchPlacesAsync(It.IsAny<Coordinates>(), It.IsAny<int>(),
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Place>());

        var request = CreateRequest();

        // Act
        var result = await _sut.GenerateAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("No places");
    }

    [Fact]
    public async Task GenerateAsync_WeatherUnavailable_AddsNotice()
    {
        // Arrange
        SetupSuccessfulMocks(3);
        _weatherClient.Setup(w => w.GetForecastAsync(It.IsAny<Coordinates>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WeatherForecast>());

        var request = CreateRequest(durationDays: 3);

        // Act
        var result = await _sut.GenerateAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Notices.Should().Contain(n => n.Contains("Weather forecast is unavailable"));
    }

    [Fact]
    public async Task GenerateAsync_IncludeAccommodations_AddsNotice()
    {
        // Arrange
        SetupSuccessfulMocks(2);
        var request = CreateRequest(durationDays: 2, includeAccommodations: true);

        // Act
        var result = await _sut.GenerateAsync(request);

        // Assert
        result.Notices.Should().Contain(n => n.Contains("Accommodation suggestions are not yet available"));
    }

    [Fact]
    public async Task GenerateAsync_PersistsAsDraft()
    {
        // Arrange
        SetupSuccessfulMocks(2);
        var request = CreateRequest(durationDays: 2);

        // Act
        await _sut.GenerateAsync(request);

        // Assert
        _itineraryRepo.Verify(r => r.AddAsync(It.Is<Itinerary>(i => i.Status == "Draft"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupSuccessfulMocks(int days)
    {
        _geocodingClient.Setup(g => g.GeocodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeocodingResult(new Coordinates(48.8566m, 2.3522m), "Paris, France", "France", "Europe/Paris"));

        var places = Enumerable.Range(1, days * 4).Select(i => new Place
        {
            ExternalId = $"place_{i}", Name = $"Place {i}", Latitude = 48.8566m + i * 0.01m,
            Longitude = 2.3522m + i * 0.01m, Category = "cultural", IsIndoor = i % 2 == 0,
            EstimatedCost = 10m, TypicalVisitMinutes = 60, Rating = 4.0m, CachedAt = DateTime.UtcNow
        }).ToList();

        _placesClient.Setup(p => p.SearchPlacesAsync(It.IsAny<Coordinates>(), It.IsAny<int>(),
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(places);

        var forecasts = Enumerable.Range(0, days).Select(_ =>
            new WeatherForecast(2, 20m, 10m, 0m)).ToList();
        _weatherClient.Setup(w => w.GetForecastAsync(It.IsAny<Coordinates>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(forecasts);

        _currencyService.Setup(c => c.ConvertAsync(It.IsAny<decimal>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((1m, false));
        _currencyService.Setup(c => c.GetRateAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(1m);

        _routingClient.Setup(r => r.GetDistanceMatrixAsync(It.IsAny<IReadOnlyList<Coordinates>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DistanceMatrixResult([], []));

        _costService.Setup(c => c.CalculateCostBreakdown(It.IsAny<Itinerary>(), It.IsAny<decimal>()))
            .Returns(new CostBreakdown
            {
                TotalActivitiesCost = 100, TotalDiningCost = 0, TotalTransportCost = 10,
                GrandTotal = 110, RemainingBudget = 390, CurrencyCode = "USD"
            });
    }

    private static ItineraryGenerationRequest CreateRequest(
        string cityName = "Paris", int durationDays = 3, decimal budget = 500,
        bool includeAccommodations = false)
    {
        return new ItineraryGenerationRequest(
            Guid.NewGuid(), cityName, budget, "USD", durationDays,
            DateTime.UtcNow.AddDays(7),
            ["museums", "parks", "food"],
            IncludeAccommodations: includeAccommodations);
    }
}
