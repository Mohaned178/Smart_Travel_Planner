using System.Linq;
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
        
        SetupSuccessfulMocks(3);

        var request = CreateRequest(durationDays: 3);

        
        var result = await _sut.GenerateAsync(request);

        
        result.Success.Should().BeTrue();
        result.Itinerary.DayPlans.Should().HaveCount(3);
        result.Itinerary.CityName.Should().Be("Paris");
        result.Itinerary.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task GenerateAsync_ActivitiesMatchInterests_CostWithinBudget()
    {
        
        SetupSuccessfulMocks(3);

        _costService.Setup(c => c.CalculateCostBreakdown(It.IsAny<Itinerary>(), It.IsAny<decimal>()))
            .Returns(new CostBreakdown
            {
                TotalActivitiesCost = 100, TotalDiningCost = 50, TotalTransportCost = 20,
                GrandTotal = 170, RemainingBudget = 330, CurrencyCode = "USD"
            });

        var request = CreateRequest(durationDays: 3, budget: 500);

        
        var result = await _sut.GenerateAsync(request);

        
        result.Success.Should().BeTrue();
        result.Itinerary.CostBreakdown!.GrandTotal.Should().BeLessThanOrEqualTo(500);
    }

    [Fact]
    public async Task GenerateAsync_UnrecognizedCity_ReturnsError()
    {
        
        _geocodingClient.Setup(g => g.GeocodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GeocodingResult?)null);

        var request = CreateRequest(cityName: "NonexistentCity123");

        
        var result = await _sut.GenerateAsync(request);

        
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("could not be found");
    }

    [Fact]
    public async Task GenerateAsync_NoPlacesFound_ReturnsError()
    {
        
        _geocodingClient.Setup(g => g.GeocodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeocodingResult(new Coordinates(48.8566m, 2.3522m), "Paris", "France", "Europe/Paris"));
        _placesClient.Setup(p => p.SearchPlacesAsync(It.IsAny<Coordinates>(), It.IsAny<int>(),
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Place>());

        var request = CreateRequest();

        
        var result = await _sut.GenerateAsync(request);

        
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("No places");
    }

    [Fact]
    public async Task GenerateAsync_WeatherUnavailable_AddsNotice()
    {
        
        SetupSuccessfulMocks(3);
        _weatherClient.Setup(w => w.GetForecastAsync(It.IsAny<Coordinates>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WeatherForecast>());

        var request = CreateRequest(durationDays: 3);

        
        var result = await _sut.GenerateAsync(request);

        
        result.Success.Should().BeTrue();
        result.Notices.Should().Contain(n => n.Contains("Weather forecast is unavailable"));
    }

    [Fact]
    public async Task GenerateAsync_IncludeAccommodations_AddsNotice()
    {
        
        SetupSuccessfulMocks(2);
        var request = CreateRequest(durationDays: 2, includeAccommodations: true);

        
        var result = await _sut.GenerateAsync(request);

        
        result.Notices.Should().Contain(n => n.Contains("Accommodation suggestions are not yet available"));
    }

    [Fact]
    public async Task GenerateAsync_PersistsAsDraft()
    {
        
        SetupSuccessfulMocks(2);
        var request = CreateRequest(durationDays: 2);

        
        await _sut.GenerateAsync(request);

        
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
            new WeatherForecast(800, 20m, 10m, 0m)).ToList();
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

    [Fact]
    public async Task GenerateAsync_ExcludesForbiddenCategories()
    {
        
        SetupSuccessfulMocks(1);
        
        var forbiddenPlace = new Place
        {
            ExternalId = "forbidden_1", Name = "Bad Bar", Latitude = 48.85m, Longitude = 2.35m, Category = "bar", AllTypes = ["bar"],
            EstimatedCost = 10m, TypicalVisitMinutes = 60, Rating = 4.0m, CachedAt = DateTime.UtcNow
        };
        var validPlace = new Place
        {
            ExternalId = "valid_1", Name = "Good Museum", Latitude = 48.86m, Longitude = 2.36m, Category = "museum", AllTypes = ["museum"],
            EstimatedCost = 10m, TypicalVisitMinutes = 60, Rating = 4.0m, CachedAt = DateTime.UtcNow
        };

        _placesClient.Setup(p => p.SearchPlacesAsync(It.IsAny<Coordinates>(), It.IsAny<int>(),
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Place> { forbiddenPlace, validPlace });

        var request = CreateRequest(durationDays: 1);

        
        var result = await _sut.GenerateAsync(request);

        
        result.Success.Should().BeTrue();
        result.Itinerary.DayPlans.First().Activities.Should().ContainSingle()
            .Which.PlaceName.Should().Be("Good Museum");
    }

    [Fact]
    public async Task GenerateAsync_DedupsRestaurantsFromActivities_AndRecalculatesSchedule()
    {
        
        SetupSuccessfulMocks(1);
        
        var placeOverlap = new Place
        {
            ExternalId = "overlap_1", Name = "Eiffel Restaurant", Latitude = 48.85m, Longitude = 2.35m, Category = "tourist_attraction",
            EstimatedCost = 20m, TypicalVisitMinutes = 60, Rating = 4.5m, CachedAt = DateTime.UtcNow
        };
        var uniqueActivity1 = new Place
        {
            ExternalId = "unique_1", Name = "Museum", Latitude = 48.86m, Longitude = 2.36m, Category = "museum",
            EstimatedCost = 30m, TypicalVisitMinutes = 60, Rating = 4.8m, CachedAt = DateTime.UtcNow
        };
        var uniqueActivity2 = new Place
        {
            ExternalId = "unique_2", Name = "Park", Latitude = 48.86m, Longitude = 2.36m, Category = "park",
            EstimatedCost = 0m, TypicalVisitMinutes = 60, Rating = 4.9m, CachedAt = DateTime.UtcNow
        };

        _placesClient.Setup(p => p.SearchPlacesAsync(It.IsAny<Coordinates>(), It.IsAny<int>(),
            It.Is<IEnumerable<string>>(types => types.Contains("museum") || types.Contains("park") || types.Contains("tourist_attraction")), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Place> { uniqueActivity1, placeOverlap, uniqueActivity2 });

        _placesClient.Setup(p => p.SearchPlacesAsync(It.IsAny<Coordinates>(), It.IsAny<int>(),
            It.Is<IEnumerable<string>>(types => types.Contains("restaurant") || types.Contains("cafe")), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Place> { placeOverlap });

        var request = CreateRequest(durationDays: 1, includeRestaurants: true);

        
        var result = await _sut.GenerateAsync(request);

        
        result.Success.Should().BeTrue();
        var dayPlan = result.Itinerary.DayPlans.First();
        
        dayPlan.Restaurants.Should().Contain(r => r.ExternalPlaceId == placeOverlap.ExternalId);
        dayPlan.Activities.Should().NotContain(a => a.ExternalPlaceId == placeOverlap.ExternalId);
        
        dayPlan.Activities.Should().HaveCount(2);
        dayPlan.Activities.ElementAt(0).OrderIndex.Should().Be(1);
        dayPlan.Activities.ElementAt(0).PlaceName.Should().Be("Park");
        dayPlan.Activities.ElementAt(0).StartTime.Should().Be(new TimeOnly(9, 0));
        
        dayPlan.Activities.ElementAt(1).OrderIndex.Should().Be(2);
        dayPlan.Activities.ElementAt(1).PlaceName.Should().Be("Museum");
    }

    [Fact]
    public async Task GenerateAsync_BudgetLimitsActivities_AndRecalculatesSchedule()
    {
        
        SetupSuccessfulMocks(1);
        
        var places = Enumerable.Range(1, 4).Select(i => new Place
        {
            ExternalId = $"place_{i}", Name = $"Costly Place {i}", Latitude = 48.85m + i * 0.01m,
            Longitude = 2.35m + i * 0.01m, Category = "museum", IsIndoor = true,
            EstimatedCost = 100m, TypicalVisitMinutes = 60, Rating = 4.0m, CachedAt = DateTime.UtcNow
        }).ToList();

        places[0].Category = "museum";
        places[1].Category = "park";
        places[2].Category = "tourist_attraction";
        places[3].Category = "art_gallery";

        _placesClient.Setup(p => p.SearchPlacesAsync(It.IsAny<Coordinates>(), It.IsAny<int>(),
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(places);

        var request = CreateRequest(durationDays: 1, budget: 250m);

        
        var result = await _sut.GenerateAsync(request);

        
        result.Success.Should().BeTrue();
        var dayPlan = result.Itinerary.DayPlans.First();
        
        dayPlan.Activities.Should().HaveCount(2);
        dayPlan.Activities.ElementAt(0).OrderIndex.Should().Be(1);
        dayPlan.Activities.ElementAt(1).OrderIndex.Should().Be(2);
        
        dayPlan.Activities.ElementAt(0).StartTime.Should().Be(new TimeOnly(9, 0));
        dayPlan.Activities.ElementAt(1).StartTime.Should().BeOnOrAfter(new TimeOnly(11, 30));
    }

    private static ItineraryGenerationRequest CreateRequest(
        string cityName = "Paris", int durationDays = 3, decimal budget = 500,
        bool includeAccommodations = false, bool includeRestaurants = false)
    {
        return new ItineraryGenerationRequest(
            Guid.NewGuid(), cityName, budget, "USD", durationDays,
            DateTime.UtcNow.AddDays(7),
            ["museums", "parks", "food"],
            IncludeRestaurants: includeRestaurants,
            IncludeAccommodations: includeAccommodations);
    }
}
