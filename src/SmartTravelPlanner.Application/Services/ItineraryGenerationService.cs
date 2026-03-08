using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartTravelPlanner.Domain.Entities;
using SmartTravelPlanner.Domain.Interfaces;
using SmartTravelPlanner.Domain.ValueObjects;

namespace SmartTravelPlanner.Application.Services;

public interface IItineraryGenerationService
{
    Task<ItineraryGenerationResult> GenerateAsync(ItineraryGenerationRequest request, CancellationToken ct = default);
}

/// <summary>
/// Input model for itinerary generation, decoupled from API DTOs.
/// </summary>
public record ItineraryGenerationRequest(
    Guid UserId,
    string CityName,
    decimal TotalBudget,
    string CurrencyCode,
    int DurationDays,
    DateTime TripStartDate,
    List<string> Interests,
    bool IncludeRestaurants = false,
    bool IncludeAccommodations = false,
    List<string>? CuisinePreferences = null);

/// <summary>
/// Output result from itinerary generation.
/// </summary>
public record ItineraryGenerationResult(
    Itinerary Itinerary,
    List<string> Notices,
    bool Success,
    string? Error = null);

/// <summary>
/// Orchestrates the full itinerary generation pipeline:
///   geocode → fetch places → allocate to days → calculate costs → assemble Itinerary.
/// Per FR-002, FR-003, FR-004, FR-005.
/// </summary>
public class ItineraryGenerationService : IItineraryGenerationService
{
    private readonly IGeocodingClient _geocodingClient;
    private readonly IPlacesClient _placesClient;
    private readonly IWeatherClient _weatherClient;
    private readonly IRoutingClient _routingClient;
    private readonly ICurrencyConversionService _currencyService;
    private readonly ICostCalculationService _costService;
    private readonly IItineraryRepository _itineraryRepo;
    private readonly ILogger<ItineraryGenerationService> _logger;
    private readonly decimal _transportRatePerKm;
    private readonly int _searchRadiusMeters;

    // Interest name → OpenTripMap category mapping
    private static readonly Dictionary<string, string> InterestToCategory = new(StringComparer.OrdinalIgnoreCase)
    {
        ["museums"] = "cultural", ["parks"] = "natural", ["food"] = "food",
        ["nightlife"] = "amusements", ["shopping"] = "shops", ["history"] = "cultural",
        ["landmarks"] = "cultural", ["adventure"] = "sport", ["beaches"] = "natural",
        ["art"] = "cultural"
    };

    public ItineraryGenerationService(
        IGeocodingClient geocodingClient,
        IPlacesClient placesClient,
        IWeatherClient weatherClient,
        IRoutingClient routingClient,
        ICurrencyConversionService currencyService,
        ICostCalculationService costService,
        IItineraryRepository itineraryRepo,
        IConfiguration config,
        ILogger<ItineraryGenerationService> logger)
    {
        _geocodingClient = geocodingClient;
        _placesClient = placesClient;
        _weatherClient = weatherClient;
        _routingClient = routingClient;
        _currencyService = currencyService;
        _costService = costService;
        _itineraryRepo = itineraryRepo;
        _logger = logger;
        _transportRatePerKm = decimal.Parse(config["TransportRates:PublicTransportPerKm"] ?? "0.50");
        _searchRadiusMeters = int.Parse(config["ExternalApis:OpenTripMap:SearchRadiusMeters"] ?? "10000");
    }

    public async Task<ItineraryGenerationResult> GenerateAsync(ItineraryGenerationRequest request, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var notices = new List<string>();

        _logger.LogInformation("Starting itinerary generation for {City}, {Days} days, budget {Budget} {Currency}",
            request.CityName, request.DurationDays, request.TotalBudget, request.CurrencyCode);

        // Step 1: Geocode city
        var geocodeResult = await _geocodingClient.GeocodeAsync(request.CityName, ct);
        if (geocodeResult is null)
        {
            _logger.LogWarning("City not found: {City}", request.CityName);
            return new ItineraryGenerationResult(null!, notices, false, $"City '{request.CityName}' could not be found.");
        }

        _logger.LogInformation("Geocoded {City} → {Lat},{Lon}",
            request.CityName, geocodeResult.Coordinates.Latitude, geocodeResult.Coordinates.Longitude);

        // Step 2: Fetch places based on interests
        var categories = request.Interests
            .Select(i => InterestToCategory.GetValueOrDefault(i, i))
            .Distinct()
            .ToList();

        var places = await _placesClient.SearchPlacesAsync(
            geocodeResult.Coordinates, _searchRadiusMeters, categories, ct);

        if (places.Count == 0)
        {
            _logger.LogWarning("No places found for categories: {Categories}", string.Join(", ", categories));
            return new ItineraryGenerationResult(null!, notices, false,
                "No places of interest found for the given interests in this city.");
        }

        _logger.LogInformation("Found {Count} places of interest", places.Count);

        // Step 3: Fetch weather forecast
        var forecasts = await _weatherClient.GetForecastAsync(
            geocodeResult.Coordinates, request.DurationDays, ct);

        if (forecasts.Count == 0)
            notices.Add("Weather forecast is unavailable. Itinerary generated without weather adjustments.");

        // Step 4: Get exchange rate for cost conversion
        var (_, currencyFallback) = await _currencyService.ConvertAsync(1m, "EUR", request.CurrencyCode, ct);
        if (currencyFallback)
            notices.Add($"Exchange rate for EUR → {request.CurrencyCode} unavailable. Costs shown in EUR.");

        var exchangeRate = await _currencyService.GetRateAsync("EUR", request.CurrencyCode, ct) ?? 1m;

        // Step 5: Build itinerary — allocate places to days
        var itinerary = BuildItinerary(request, geocodeResult, places, forecasts, exchangeRate, notices);

        // Step 6: Route optimization — order activities using distance matrix
        await OptimizeRoutes(itinerary, ct, notices);

        // Step 7: Calculate cost breakdown
        itinerary.CostBreakdown = _costService.CalculateCostBreakdown(itinerary, _transportRatePerKm);

        // Check budget utilization
        if (itinerary.CostBreakdown.RemainingBudget < request.TotalBudget * 0.1m && itinerary.CostBreakdown.RemainingBudget > 0)
            notices.Add("Budget is nearly fully utilized (less than 10% remaining).");

        // Step 8: Handle accommodation request
        if (request.IncludeAccommodations)
            notices.Add("Accommodation suggestions are not yet available.");

        // Persist as Draft
        await _itineraryRepo.AddAsync(itinerary, ct);

        stopwatch.Stop();
        _logger.LogInformation(
            "Itinerary generated in {Elapsed}ms — {DayCount} days, {ActivityCount} activities, budget utilization {Utilization:P1}",
            stopwatch.ElapsedMilliseconds,
            itinerary.DayPlans.Count,
            itinerary.DayPlans.Sum(dp => dp.Activities.Count),
            itinerary.CostBreakdown.GrandTotal / request.TotalBudget);

        return new ItineraryGenerationResult(itinerary, notices, true);
    }

    private Itinerary BuildItinerary(
        ItineraryGenerationRequest request,
        GeocodingResult geo,
        IReadOnlyList<Place> places,
        IReadOnlyList<WeatherForecast> forecasts,
        decimal exchangeRate,
        List<string> notices)
    {
        var itinerary = new Itinerary
        {
            UserId = request.UserId,
            CityName = request.CityName,
            CountryName = geo.CountryName,
            Latitude = geo.Coordinates.Latitude,
            Longitude = geo.Coordinates.Longitude,
            Timezone = geo.Timezone,
            TotalBudget = request.TotalBudget,
            CurrencyCode = request.CurrencyCode,
            DurationDays = request.DurationDays,
            TripStartDate = request.TripStartDate,
            Status = "Draft"
        };

        // Sort places by rating (best first), then distribute across days
        var sortedPlaces = places
            .OrderByDescending(p => p.Rating ?? 0)
            .ToList();

        var budgetPerDay = request.TotalBudget / request.DurationDays;
        var placesPerDay = Math.Clamp(sortedPlaces.Count / request.DurationDays, 3, 6);
        var placeIndex = 0;

        for (int day = 0; day < request.DurationDays; day++)
        {
            var dayPlan = new DayPlan
            {
                ItineraryId = itinerary.Id,
                DayNumber = day + 1,
                Date = request.TripStartDate.AddDays(day)
            };

            // Apply weather data
            if (day < forecasts.Count)
            {
                var forecast = forecasts[day];
                dayPlan.WeatherCode = forecast.Code;
                dayPlan.MaxTemperatureC = forecast.MaxTemp;
                dayPlan.MinTemperatureC = forecast.MinTemp;
                dayPlan.PrecipitationMm = forecast.Precipitation;
                dayPlan.WeatherSummary = ClassifyWeather(forecast.Code);
            }

            // Allocate activities for the day
            var dayBudgetRemaining = budgetPerDay;
            var startTime = new TimeOnly(9, 0);

            for (int slot = 0; slot < placesPerDay && placeIndex < sortedPlaces.Count; slot++)
            {
                var place = sortedPlaces[placeIndex];
                var costInUserCurrency = Math.Round(place.EstimatedCost * exchangeRate, 2);

                // Skip if it would exceed daily budget
                if (costInUserCurrency > dayBudgetRemaining && costInUserCurrency > 0)
                {
                    placeIndex++;
                    slot--; // retry with next place
                    continue;
                }

                var visitMinutes = place.TypicalVisitMinutes > 0 ? place.TypicalVisitMinutes : 60;
                var endTime = startTime.AddMinutes(visitMinutes);

                var activity = new ActivitySlot
                {
                    DayPlanId = dayPlan.Id,
                    OrderIndex = slot + 1,
                    StartTime = startTime,
                    EndTime = endTime,
                    PlaceName = place.Name,
                    PlaceAddress = place.Address,
                    PlaceLatitude = place.Latitude,
                    PlaceLongitude = place.Longitude,
                    Category = place.Category,
                    IsIndoor = place.IsIndoor,
                    EstimatedCostLocal = place.EstimatedCost,
                    EstimatedCostUser = costInUserCurrency,
                    VisitDurationMinutes = visitMinutes,
                    ExternalPlaceId = place.ExternalId
                };

                dayPlan.Activities.Add(activity);
                dayBudgetRemaining -= costInUserCurrency;
                startTime = endTime.AddMinutes(30); // 30-min gap between activities
                placeIndex++;
            }

            // Weather-aware adjustment: prioritize indoor on bad weather days
            if (dayPlan.WeatherCode.HasValue && IsRainyWeather(dayPlan.WeatherCode.Value))
            {
                var activities = dayPlan.Activities.ToList();
                var reordered = activities
                    .OrderByDescending(a => a.IsIndoor)
                    .ThenBy(a => a.OrderIndex)
                    .ToList();

                for (int i = 0; i < reordered.Count; i++)
                    reordered[i].OrderIndex = i + 1;

                dayPlan.Activities = reordered;
            }

            // Restaurant suggestions for the day
            if (request.IncludeRestaurants && dayPlan.Activities.Count > 0)
            {
                AddRestaurantSuggestions(dayPlan, sortedPlaces, exchangeRate, request.CuisinePreferences);
            }

            itinerary.DayPlans.Add(dayPlan);
        }

        return itinerary;
    }

    private async Task OptimizeRoutes(Itinerary itinerary, CancellationToken ct, List<string> notices)
    {
        foreach (var dayPlan in itinerary.DayPlans)
        {
            var activities = dayPlan.Activities.OrderBy(a => a.OrderIndex).ToList();
            if (activities.Count < 2) continue;

            var locations = activities
                .Select(a => new Coordinates(a.PlaceLatitude, a.PlaceLongitude))
                .ToList();

            try
            {
                var matrix = await _routingClient.GetDistanceMatrixAsync(locations, ct);
                if (matrix.DistancesKm.Length > 0)
                {
                    var optimizedOrder = NearestNeighborOrder(matrix.DistancesKm);
                    var reordered = new List<ActivitySlot>();

                    for (int i = 0; i < optimizedOrder.Count; i++)
                    {
                        var activity = activities[optimizedOrder[i]];
                        activity.OrderIndex = i + 1;

                        if (i > 0)
                        {
                            var prevIdx = optimizedOrder[i - 1];
                            var currIdx = optimizedOrder[i];
                            activity.TravelDistanceFromPrevKm = matrix.DistancesKm[prevIdx][currIdx];
                            activity.TravelTimeFromPrevMinutes = matrix.DurationsMinutes[prevIdx][currIdx];
                        }

                        reordered.Add(activity);
                    }

                    dayPlan.Activities = reordered;
                    ReassignTimeSlots(dayPlan);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Route optimization failed, using Haversine fallback");
                ApplyHaversineFallback(activities);
                if (!notices.Contains("Route optimization unavailable. Travel estimates based on straight-line distance."))
                    notices.Add("Route optimization unavailable. Travel estimates based on straight-line distance.");
            }
        }
    }

    private static List<int> NearestNeighborOrder(decimal[][] distances)
    {
        var n = distances.Length;
        var visited = new bool[n];
        var order = new List<int> { 0 };
        visited[0] = true;

        for (int step = 1; step < n; step++)
        {
            var last = order[^1];
            var nearest = -1;
            var nearestDist = decimal.MaxValue;

            for (int j = 0; j < n; j++)
            {
                if (!visited[j] && distances[last][j] < nearestDist)
                {
                    nearest = j;
                    nearestDist = distances[last][j];
                }
            }

            if (nearest >= 0)
            {
                order.Add(nearest);
                visited[nearest] = true;
            }
        }

        return order;
    }

    private static void ReassignTimeSlots(DayPlan dayPlan)
    {
        var startTime = new TimeOnly(9, 0);
        foreach (var activity in dayPlan.Activities.OrderBy(a => a.OrderIndex))
        {
            var travelMinutes = (int)(activity.TravelTimeFromPrevMinutes ?? 0);
            if (activity.OrderIndex > 1)
                startTime = startTime.AddMinutes(travelMinutes);

            activity.StartTime = startTime;
            activity.EndTime = startTime.AddMinutes(activity.VisitDurationMinutes);
            startTime = activity.EndTime.AddMinutes(15); // 15-min buffer
        }
    }

    private static void ApplyHaversineFallback(List<ActivitySlot> activities)
    {
        for (int i = 1; i < activities.Count; i++)
        {
            var prev = new Coordinates(activities[i - 1].PlaceLatitude, activities[i - 1].PlaceLongitude);
            var curr = new Coordinates(activities[i].PlaceLatitude, activities[i].PlaceLongitude);
            var distKm = prev.DistanceToKm(curr);
            activities[i].TravelDistanceFromPrevKm = distKm;
            activities[i].TravelTimeFromPrevMinutes = distKm * 3m; // Rough: 3 min/km walking
        }
    }

    private static void AddRestaurantSuggestions(DayPlan dayPlan, List<Place> allPlaces,
        decimal exchangeRate, List<string>? cuisinePreferences)
    {
        var foodPlaces = allPlaces.Where(p =>
            p.Category.Contains("food", StringComparison.OrdinalIgnoreCase) ||
            p.Category.Contains("restaurants", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (foodPlaces.Count == 0) return;

        var mealSlots = new[] { "lunch", "dinner" };
        var activityCenter = dayPlan.Activities.FirstOrDefault();
        if (activityCenter is null) return;

        foreach (var meal in mealSlots)
        {
            var restaurant = foodPlaces.FirstOrDefault();
            if (restaurant is null) break;

            var distKm = new Coordinates(activityCenter.PlaceLatitude, activityCenter.PlaceLongitude)
                .DistanceToKm(new Coordinates(restaurant.Latitude, restaurant.Longitude));

            dayPlan.Restaurants.Add(new RestaurantSuggestion
            {
                DayPlanId = dayPlan.Id,
                MealSlot = meal,
                Name = restaurant.Name,
                CuisineType = cuisinePreferences?.FirstOrDefault(),
                Latitude = restaurant.Latitude,
                Longitude = restaurant.Longitude,
                DistanceFromActivityKm = distKm,
                EstimatedMealCost = Math.Round(15m * exchangeRate, 2), // Estimated meal cost
                ExternalPlaceId = restaurant.ExternalId
            });

            foodPlaces.Remove(restaurant);
        }
    }

    private static string ClassifyWeather(int code) => code switch
    {
        0 => "Clear sky",
        1 or 2 or 3 => "Partly cloudy",
        45 or 48 => "Foggy",
        51 or 53 or 55 or 61 or 63 or 65 or 80 or 81 or 82 => "Rainy",
        56 or 57 or 66 or 67 => "Freezing rain",
        71 or 73 or 75 or 77 or 85 or 86 => "Snowy",
        95 or 96 or 99 => "Thunderstorm",
        _ => "Unknown"
    };

    private static bool IsRainyWeather(int code)
        => code is >= 51 and <= 67 or >= 80 and <= 82 or >= 95;
}
