using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartTravelPlanner.Application.Helpers;
using SmartTravelPlanner.Domain.Constants;
using SmartTravelPlanner.Domain.Entities;
using SmartTravelPlanner.Domain.Interfaces;
using SmartTravelPlanner.Domain.ValueObjects;

namespace SmartTravelPlanner.Application.Services;

public interface IItineraryGenerationService
{
    Task<ItineraryGenerationResult> GenerateAsync(ItineraryGenerationRequest request, CancellationToken ct = default);
}

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

public record ItineraryGenerationResult(
    Itinerary Itinerary,
    List<string> Notices,
    bool Success,
    string? Error = null);

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

    private const int NearbySearchRadiusMeters = 3000;
    private const decimal MaxDistanceFromCityKm = 5m;
    private const int MaxActivitiesPerDay = 4;
    private const int MaxSameCategoryPerDay = 2;
    private const int BreakBetweenActivitiesMinutes = 20;
    private const int MinActivitiesPerDay = 3;

    private static readonly Dictionary<string, int> CategoryDurationMinutes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["museum"]             = 150,
            ["art_gallery"]        = 120,
            ["park"]               = 75,
            ["beach"]              = 75,
            ["tourist_attraction"] = 90,
            ["amusement_park"]     = 120,
            ["shopping_mall"]      = 90,
            ["restaurant"]         = 60,
            ["church"]             = 45,
            ["mosque"]             = 45,
            ["synagogue"]          = 45,
            ["hindu_temple"]       = 45,
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
        _geocodingClient    = geocodingClient;
        _placesClient       = placesClient;
        _weatherClient      = weatherClient;
        _routingClient      = routingClient;
        _currencyService    = currencyService;
        _costService        = costService;
        _itineraryRepo      = itineraryRepo;
        _logger             = logger;
        _transportRatePerKm = decimal.Parse(
            config["TransportRates:PublicTransportPerKm"] ?? "0.50");
    }

    public async Task<ItineraryGenerationResult> GenerateAsync(
        ItineraryGenerationRequest request, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var notices   = new List<string>();

        _logger.LogInformation(
            "Starting itinerary generation for {City}, {Days} days, budget {Budget} {Currency}",
            request.CityName, request.DurationDays, request.TotalBudget, request.CurrencyCode);

        var geo = await _geocodingClient.GeocodeAsync(request.CityName, ct);
        if (geo is null)
        {
            _logger.LogWarning("City not found: {City}", request.CityName);
            return new ItineraryGenerationResult(null!, notices, false,
                $"City '{request.CityName}' could not be found.");
        }

        _logger.LogInformation("Geocoded {City} → {Lat},{Lon}",
            request.CityName, geo.Coordinates.Latitude, geo.Coordinates.Longitude);

        var activityTypes = ResolveActivityTypes(request.Interests);

        var rawPlaces = await _placesClient.SearchPlacesAsync(
            geo.Coordinates, NearbySearchRadiusMeters, activityTypes, ct);

        var placesInCity = PlaceFilters.FilterByDistance(rawPlaces, geo.Coordinates, MaxDistanceFromCityKm);

        var places = PlaceFilters.FilterForbidden(placesInCity)
            .Where(p => !PlaceTypeConstants.FoodRelatedTypes.Contains(p.Category))
            .ToList();

        if (places.Count == 0)
        {
            _logger.LogWarning("No activity places found in {City} for types: {Types}",
                request.CityName, string.Join(", ", activityTypes));
            return new ItineraryGenerationResult(null!, notices, false,
                "No places of interest found for the given interests in this city.");
        }

        _logger.LogInformation("Found {Count} activity places after filtering", places.Count);

        var forecasts = await _weatherClient.GetForecastAsync(geo.Coordinates, request.DurationDays, ct);
        if (forecasts.Count == 0)
            notices.Add("Weather forecast is unavailable. Itinerary generated without weather adjustments.");

        var (_, currencyFallback) = await _currencyService.ConvertAsync(1m, "EUR", request.CurrencyCode, ct);
        if (currencyFallback)
            notices.Add($"Exchange rate for EUR → {request.CurrencyCode} unavailable. Costs shown in EUR.");

        var exchangeRate = await _currencyService.GetRateAsync("EUR", request.CurrencyCode, ct) ?? 1m;

        IReadOnlyList<Place> restaurantPlaces = [];
        if (request.IncludeRestaurants)
        {
            var rawRestaurants = await _placesClient.SearchPlacesAsync(
                geo.Coordinates, NearbySearchRadiusMeters, ["restaurant", "cafe"], ct);

            restaurantPlaces = PlaceFilters.FilterForbidden(
                PlaceFilters.FilterByDistance(rawRestaurants, geo.Coordinates, MaxDistanceFromCityKm));
        }

        var itinerary = await BuildItineraryAsync(
            request, geo, places, restaurantPlaces, forecasts, exchangeRate, notices, ct);

        await OptimizeRoutesAsync(itinerary, ct, notices);

        EnforceBudget(itinerary, request.TotalBudget, notices);

        itinerary.CostBreakdown = _costService.CalculateCostBreakdown(itinerary, _transportRatePerKm);

        if (itinerary.CostBreakdown.RemainingBudget < request.TotalBudget * 0.1m &&
            itinerary.CostBreakdown.RemainingBudget > 0)
            notices.Add("Budget is nearly fully utilized (less than 10% remaining).");

        if (request.IncludeAccommodations)
            notices.Add("Accommodation suggestions are not yet available.");

        await _itineraryRepo.AddAsync(itinerary, ct);

        stopwatch.Stop();
        _logger.LogInformation(
            "Itinerary generated in {Elapsed}ms — {Days} days, {Activities} activities, utilization {Util:P1}",
            stopwatch.ElapsedMilliseconds,
            itinerary.DayPlans.Count,
            itinerary.DayPlans.Sum(dp => dp.Activities.Count),
            itinerary.CostBreakdown.GrandTotal / request.TotalBudget);

        return new ItineraryGenerationResult(itinerary, notices, true);
    }

    private static IReadOnlyList<string> ResolveActivityTypes(IEnumerable<string> interests)
    {
        var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var interest in interests)
        {
            if (string.Equals(interest, "food", StringComparison.OrdinalIgnoreCase))
                continue;

            if (PlaceTypeConstants.InterestToPlaceTypes.TryGetValue(interest, out var placeTypes))
            {
                foreach (var t in placeTypes)
                {
                    if (!PlaceTypeConstants.FoodRelatedTypes.Contains(t) &&
                        !PlaceTypeConstants.ForbiddenCategories.Contains(t))
                        types.Add(t);
                }
            }
            else
            {
                types.Add(interest);
            }
        }

        if (types.Count == 0) types.Add("tourist_attraction");
        return types.ToList();
    }

    private async Task<Itinerary> BuildItineraryAsync(
        ItineraryGenerationRequest request,
        GeocodingResult geo,
        IReadOnlyList<Place> places,
        IReadOnlyList<Place> restaurantPlaces,
        IReadOnlyList<WeatherForecast> forecasts,
        decimal exchangeRate,
        List<string> notices,
        CancellationToken ct)
    {
        var itinerary = new Itinerary
        {
            UserId        = request.UserId,
            CityName      = request.CityName,
            CountryName   = geo.CountryName,
            Latitude      = geo.Coordinates.Latitude,
            Longitude     = geo.Coordinates.Longitude,
            Timezone      = geo.Timezone,
            TotalBudget   = request.TotalBudget,
            CurrencyCode  = request.CurrencyCode,
            DurationDays  = request.DurationDays,
            TripStartDate = request.TripStartDate,
            Status        = "Draft"
        };

        var enrichedPlaces = EnrichDurations(places);
        var clusteredDays  = ClusterPlacesByProximity(enrichedPlaces, request.DurationDays);

        var budgetPerDay         = request.TotalBudget / request.DurationDays;
        var usedRestaurantIds    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedActivityPlaceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rng                  = new Random(request.TripStartDate.DayOfYear + request.CityName.Length);

        for (int day = 0; day < request.DurationDays; day++)
        {
            var dayPlan = new DayPlan
            {
                ItineraryId = itinerary.Id,
                DayNumber   = day + 1,
                Date        = request.TripStartDate.AddDays(day)
            };

            if (day < forecasts.Count)
            {
                var forecast = forecasts[day];
                dayPlan.WeatherCode     = forecast.Code;
                dayPlan.MaxTemperatureC = forecast.MaxTemp;
                dayPlan.MinTemperatureC = forecast.MinTemp;
                dayPlan.PrecipitationMm = forecast.Precipitation;
                dayPlan.WeatherSummary  = ClassifyWeather(forecast.Code);
            }

            var dayPlaces = (day < clusteredDays.Count ? clusteredDays[day] : new List<Place>())
                .Where(p => !usedActivityPlaceIds.Contains(p.ExternalId))
                .ToList();
            dayPlaces = EnforceCategoryDiversity(dayPlaces);

            var dayBudgetRemaining = budgetPerDay;
            var startTime          = new TimeOnly(9, 0);
            var activityCount      = 0;

            foreach (var place in dayPlaces)
            {
                if (activityCount >= MaxActivitiesPerDay) break;

                var costUser = Math.Round(place.EstimatedCost * exchangeRate, 2);
                if (costUser > dayBudgetRemaining && costUser > 0) continue;

                var visitMinutes = place.TypicalVisitMinutes > 0 ? place.TypicalVisitMinutes : 60;
                var endTime      = startTime.AddMinutes(visitMinutes);
                if (endTime > new TimeOnly(21, 0)) break;

                dayPlan.Activities.Add(new ActivitySlot
                {
                    DayPlanId            = dayPlan.Id,
                    OrderIndex           = activityCount + 1,
                    StartTime            = startTime,
                    EndTime              = endTime,
                    PlaceName            = place.Name,
                    PlaceAddress         = place.Address,
                    PlaceLatitude        = place.Latitude,
                    PlaceLongitude       = place.Longitude,
                    Category             = place.Category,
                    IsIndoor             = PlaceFilters.DetermineIsIndoor(place.Category),
                    EstimatedCostLocal   = place.EstimatedCost,
                    EstimatedCostUser    = costUser,
                    VisitDurationMinutes = visitMinutes,
                    ExternalPlaceId      = place.ExternalId
                });

                dayBudgetRemaining -= costUser;
                startTime = endTime.AddMinutes(BreakBetweenActivitiesMinutes);
                activityCount++;
            }

            if (dayPlan.WeatherCode.HasValue && IsRainyWeather(dayPlan.WeatherCode.Value))
            {
                var reordered = dayPlan.Activities
                    .OrderByDescending(a => a.IsIndoor)
                    .ThenBy(a => a.OrderIndex)
                    .ToList();
                for (int i = 0; i < reordered.Count; i++) reordered[i].OrderIndex = i + 1;
                dayPlan.Activities = reordered;
            }

            var dedupedRestaurantIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (request.IncludeRestaurants && dayPlan.Activities.Count > 0)
            {
                AddRestaurantSuggestions(
                    dayPlan, restaurantPlaces, exchangeRate,
                    request.CuisinePreferences, usedRestaurantIds, rng);

                foreach (var r in dayPlan.Restaurants)
                    if (r.ExternalPlaceId != null) dedupedRestaurantIds.Add(r.ExternalPlaceId);

                if (dedupedRestaurantIds.Count > 0)
                {
                    dayPlan.Activities = dayPlan.Activities
                        .Where(a => a.ExternalPlaceId == null ||
                                    !dedupedRestaurantIds.Contains(a.ExternalPlaceId))
                        .ToList();
                }
            }

            if (dayPlan.Activities.Count < MinActivitiesPerDay)
            {
                await TopUpActivitiesAsync(
                    dayPlan, geo.Coordinates, exchangeRate, budgetPerDay, dedupedRestaurantIds, ct);
            }

            RecalculateSchedule(dayPlan);

            foreach (var activity in dayPlan.Activities)
                if (activity.ExternalPlaceId != null)
                    usedActivityPlaceIds.Add(activity.ExternalPlaceId);

            itinerary.DayPlans.Add(dayPlan);
        }

        return itinerary;
    }

    private async Task TopUpActivitiesAsync(
        DayPlan dayPlan, Coordinates cityCenter, decimal exchangeRate, decimal dayBudget,
        HashSet<string> excludedPlaceIds, CancellationToken ct)
    {
        if (dayPlan.Activities.Count >= MinActivitiesPerDay) return;

        var existing = dayPlan.Activities
            .Select(a => a.ExternalPlaceId)
            .Where(id => id != null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var id in excludedPlaceIds) existing.Add(id);

        var raw = await _placesClient.SearchPlacesAsync(
            cityCenter, NearbySearchRadiusMeters, ["tourist_attraction"], ct);

        var candidates = PlaceFilters.FilterForbidden(
                             PlaceFilters.FilterByDistance(raw, cityCenter, MaxDistanceFromCityKm))
                         .Where(p => !PlaceTypeConstants.FoodRelatedTypes.Contains(p.Category))
                         .Where(p => !existing.Contains(p.ExternalId))
                         .OrderByDescending(p => p.Rating ?? 0)
                         .ToList();

        var currentStart = dayPlan.Activities.Count > 0
            ? dayPlan.Activities.Last().EndTime.AddMinutes(BreakBetweenActivitiesMinutes)
            : new TimeOnly(9, 0);

        var spent = dayPlan.Activities.Sum(a => a.EstimatedCostUser);

        foreach (var place in candidates)
        {
            if (dayPlan.Activities.Count >= MinActivitiesPerDay) break;

            var costUser     = Math.Round(place.EstimatedCost * exchangeRate, 2);
            var visitMinutes = place.TypicalVisitMinutes > 0 ? place.TypicalVisitMinutes : 60;
            var endTime      = currentStart.AddMinutes(visitMinutes);

            if (endTime > new TimeOnly(21, 0)) break;
            if (costUser > dayBudget - spent && costUser > 0) continue;

            dayPlan.Activities.Add(new ActivitySlot
            {
                DayPlanId            = dayPlan.Id,
                OrderIndex           = dayPlan.Activities.Count + 1,
                StartTime            = currentStart,
                EndTime              = endTime,
                PlaceName            = place.Name,
                PlaceAddress         = place.Address,
                PlaceLatitude        = place.Latitude,
                PlaceLongitude       = place.Longitude,
                Category             = place.Category,
                IsIndoor             = PlaceFilters.DetermineIsIndoor(place.Category),
                EstimatedCostLocal   = place.EstimatedCost,
                EstimatedCostUser    = costUser,
                VisitDurationMinutes = visitMinutes,
                ExternalPlaceId      = place.ExternalId
            });

            spent        += costUser;
            currentStart  = endTime.AddMinutes(BreakBetweenActivitiesMinutes);
            existing.Add(place.ExternalId);
        }
    }

    private static List<Place> EnrichDurations(IReadOnlyList<Place> places)
    {
        return places.Select(p =>
        {
            if (p.TypicalVisitMinutes <= 60 &&
                CategoryDurationMinutes.TryGetValue(p.Category, out var dur))
                p.TypicalVisitMinutes = dur;
            return p;
        }).ToList();
    }

    private static List<List<Place>> ClusterPlacesByProximity(List<Place> places, int days)
    {
        var sorted   = places.OrderByDescending(p => p.Rating ?? 0).ToList();
        var assigned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var clusters = new List<List<Place>>();

        for (int day = 0; day < days; day++)
        {
            var cluster = new List<Place>();
            var anchor  = sorted.FirstOrDefault(p => !assigned.Contains(p.ExternalId));
            if (anchor is null) break;

            cluster.Add(anchor);
            assigned.Add(anchor.ExternalId);

            var anchorCoord = new Coordinates(anchor.Latitude, anchor.Longitude);
            var nearby = sorted
                .Where(p => !assigned.Contains(p.ExternalId))
                .OrderBy(p => new Coordinates(p.Latitude, p.Longitude).DistanceToKm(anchorCoord))
                .Take(MaxActivitiesPerDay - 1)
                .ToList();

            foreach (var p in nearby)
            {
                cluster.Add(p);
                assigned.Add(p.ExternalId);
            }

            clusters.Add(cluster);
        }

        return clusters;
    }

    private static List<Place> EnforceCategoryDiversity(List<Place> dayPlaces)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var result = new List<Place>();
        foreach (var place in dayPlaces)
        {
            var c = counts.GetValueOrDefault(place.Category, 0);
            if (c >= MaxSameCategoryPerDay) continue;
            result.Add(place);
            counts[place.Category] = c + 1;
        }
        return result;
    }

    private async Task OptimizeRoutesAsync(Itinerary itinerary, CancellationToken ct, List<string> notices)
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
                    var order     = NearestNeighborOrder(matrix.DistancesKm);
                    var reordered = new List<ActivitySlot>();

                    for (int i = 0; i < order.Count; i++)
                    {
                        var activity        = activities[order[i]];
                        activity.OrderIndex = i + 1;

                        if (i > 0)
                        {
                            var prev = order[i - 1];
                            var curr = order[i];
                            activity.TravelDistanceFromPrevKm  = matrix.DistancesKm[prev][curr];
                            activity.TravelTimeFromPrevMinutes = matrix.DurationsMinutes[prev][curr];
                        }

                        reordered.Add(activity);
                    }

                    dayPlan.Activities = reordered;
                    AssignTransportModes(dayPlan);
                    ReassignTimeSlots(dayPlan);
                }
                else
                {
                    _logger.LogWarning("Empty distance matrix, using Haversine fallback");
                    ApplyHaversineFallback(activities);
                    dayPlan.Activities = activities;
                    AssignTransportModes(dayPlan);
                    ReassignTimeSlots(dayPlan);
                    AddNoticeOnce(notices, "Route optimization unavailable. Travel estimates based on straight-line distance.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Route optimisation failed, using Haversine fallback");
                ApplyHaversineFallback(activities);
                AssignTransportModes(dayPlan);
                AddNoticeOnce(notices, "Route optimization unavailable. Travel estimates based on straight-line distance.");
            }
        }
    }

    private static List<int> NearestNeighborOrder(decimal[][] distances)
    {
        var n       = distances.Length;
        var visited = new bool[n];
        var order   = new List<int> { 0 };
        visited[0]  = true;

        for (int step = 1; step < n; step++)
        {
            var last        = order[^1];
            var nearest     = -1;
            var nearestDist = decimal.MaxValue;

            for (int j = 0; j < n; j++)
            {
                if (!visited[j] && distances[last][j] < nearestDist)
                {
                    nearest     = j;
                    nearestDist = distances[last][j];
                }
            }

            if (nearest >= 0) { order.Add(nearest); visited[nearest] = true; }
        }

        return order;
    }

    private static void ReassignTimeSlots(DayPlan dayPlan)
    {
        var startTime     = new TimeOnly(9, 0);
        var lunchEnd      = new TimeOnly(13, 30);
        var lunchInserted = false;

        foreach (var activity in dayPlan.Activities.OrderBy(a => a.OrderIndex))
        {
            var travelMinutes = (int)(activity.TravelTimeFromPrevMinutes ?? 0);
            if (activity.OrderIndex > 1)
                startTime = startTime.AddMinutes(travelMinutes + BreakBetweenActivitiesMinutes);

            if (!lunchInserted && startTime >= new TimeOnly(12, 0) && startTime < new TimeOnly(14, 0))
            {
                startTime     = lunchEnd;
                lunchInserted = true;
            }

            activity.StartTime = startTime;
            activity.EndTime   = startTime.AddMinutes(activity.VisitDurationMinutes);
            startTime          = activity.EndTime;
        }
    }

    private static void AssignTransportModes(DayPlan dayPlan)
    {
        foreach (var activity in dayPlan.Activities)
        {
            if (!activity.TravelDistanceFromPrevKm.HasValue || activity.OrderIndex <= 1) continue;

            var dist = activity.TravelDistanceFromPrevKm.Value;

            if (dist < 1m)
            {
                activity.TransportMode             = "Walking";
                activity.TravelTimeFromPrevMinutes = Math.Max(
                    activity.TravelTimeFromPrevMinutes ?? 0,
                    Math.Round(dist / 5m * 60m + 5m, 0));
            }
            else if (dist <= 5m)
            {
                activity.TransportMode             = "Metro";
                activity.TravelTimeFromPrevMinutes = Math.Max(
                    activity.TravelTimeFromPrevMinutes ?? 0,
                    Math.Round(dist / 20m * 60m + 10m, 0));
            }
            else
            {
                activity.TransportMode = "Taxi";
                var travelTime = Math.Round(dist / 25m * 60m + 5m, 0);
                if (dist > 10m) travelTime = Math.Max(travelTime, 30m);
                activity.TravelTimeFromPrevMinutes = Math.Max(
                    activity.TravelTimeFromPrevMinutes ?? 0, travelTime);
            }
        }
    }

    private static void ApplyHaversineFallback(List<ActivitySlot> activities)
    {
        for (int i = 1; i < activities.Count; i++)
        {
            var prev = new Coordinates(activities[i - 1].PlaceLatitude, activities[i - 1].PlaceLongitude);
            var curr = new Coordinates(activities[i].PlaceLatitude, activities[i].PlaceLongitude);
            var dist = prev.DistanceToKm(curr);
            activities[i].TravelDistanceFromPrevKm  = dist;
            activities[i].TravelTimeFromPrevMinutes = dist * 3m;
        }
    }

    private static void AddRestaurantSuggestions(
        DayPlan dayPlan,
        IReadOnlyList<Place> restaurantPlaces,
        decimal exchangeRate,
        List<string>? cuisinePreferences,
        HashSet<string> usedRestaurantIds,
        Random rng)
    {
        var mealSlots = new[]
        {
            (Slot: "lunch",  Time: new TimeOnly(12, 30), MinCost: 20m, MaxCost: 35m),
            (Slot: "dinner", Time: new TimeOnly(19, 0),  MinCost: 35m, MaxCost: 55m)
        };

        var activities = dayPlan.Activities.OrderBy(a => a.StartTime).ToList();
        if (activities.Count == 0) return;

        foreach (var meal in mealSlots)
        {
            var closest = activities
                .OrderBy(a => Math.Abs(a.StartTime.ToTimeSpan().TotalMinutes - meal.Time.ToTimeSpan().TotalMinutes))
                .First();

            var center = new Coordinates(closest.PlaceLatitude, closest.PlaceLongitude);

            var restaurant = restaurantPlaces
                .Where(p => !usedRestaurantIds.Contains(p.ExternalId))
                .OrderBy(p => new Coordinates(p.Latitude, p.Longitude).DistanceToKm(center))
                .FirstOrDefault();

            if (restaurant is null) continue;

            var distKm   = new Coordinates(restaurant.Latitude, restaurant.Longitude).DistanceToKm(center);
            var mealCost = Math.Round(
                (meal.MinCost + (decimal)rng.NextDouble() * (meal.MaxCost - meal.MinCost)) * exchangeRate, 2);

            dayPlan.Restaurants.Add(new RestaurantSuggestion
            {
                DayPlanId              = dayPlan.Id,
                MealSlot               = meal.Slot,
                Name                   = restaurant.Name,
                CuisineType            = cuisinePreferences?.FirstOrDefault() ?? restaurant.Category,
                Latitude               = restaurant.Latitude,
                Longitude              = restaurant.Longitude,
                DistanceFromActivityKm = distKm,
                EstimatedMealCost      = mealCost,
                MealTime               = meal.Time,
                ExternalPlaceId        = restaurant.ExternalId
            });

            usedRestaurantIds.Add(restaurant.ExternalId);
        }
    }

    private static void RecalculateSchedule(DayPlan dayPlan)
    {
        var activities = dayPlan.Activities.ToList();
        var startTime  = new TimeOnly(9, 0);

        for (int i = 0; i < activities.Count; i++)
        {
            activities[i].OrderIndex = i + 1;

            if (i == 0)
            {
                activities[i].StartTime                 = startTime;
                activities[i].TravelTimeFromPrevMinutes = null;
                activities[i].TravelDistanceFromPrevKm  = null;
                activities[i].TransportMode             = null;
            }
            else
            {
                var travelMin = (int)(activities[i].TravelTimeFromPrevMinutes ?? 10);
                startTime = activities[i - 1].EndTime.AddMinutes(travelMin);
                activities[i].StartTime = startTime;
            }

            activities[i].EndTime = activities[i].StartTime.AddMinutes(activities[i].VisitDurationMinutes);
        }

        dayPlan.Activities = activities;
    }

    private void EnforceBudget(Itinerary itinerary, decimal totalBudget, List<string> notices)
    {
        var runningTotal = 0m;

        foreach (var dayPlan in itinerary.DayPlans.OrderBy(dp => dp.DayNumber))
        {
            var toKeep = new List<ActivitySlot>();

            foreach (var activity in dayPlan.Activities.OrderBy(a => a.OrderIndex))
            {
                var activityTotal = activity.EstimatedCostUser;
                if (activity.TravelDistanceFromPrevKm.HasValue)
                    activityTotal += _costService.EstimateTransportCost(
                        activity.TravelDistanceFromPrevKm.Value, _transportRatePerKm);

                if (runningTotal + activityTotal <= totalBudget)
                {
                    runningTotal += activityTotal;
                    toKeep.Add(activity);
                }
            }

            foreach (var restaurant in dayPlan.Restaurants)
            {
                if (runningTotal + restaurant.EstimatedMealCost > totalBudget)
                    restaurant.EstimatedMealCost = Math.Min(restaurant.EstimatedMealCost, 20m);
                runningTotal += restaurant.EstimatedMealCost;
            }

            if (toKeep.Count < dayPlan.Activities.Count)
            {
                dayPlan.Activities = toKeep;
                RecalculateSchedule(dayPlan);
            }

            dayPlan.DailyCostTotal = dayPlan.Activities.Sum(a => a.EstimatedCostUser)
                                   + dayPlan.Restaurants.Sum(r => r.EstimatedMealCost);
        }

        if (runningTotal > totalBudget)
            notices.Add("Budget slightly exceeded. Consider reducing activities or dining tier.");
    }

    private static void AddNoticeOnce(List<string> notices, string message)
    {
        if (!notices.Contains(message)) notices.Add(message);
    }

    private static string ClassifyWeather(int code) => code switch
    {
        >= 200 and < 300 => "Thunderstorm",
        >= 300 and < 400 => "Drizzle",
        >= 500 and < 600 => "Rainy",
        >= 600 and < 700 => "Snowy",
        >= 700 and < 762 => "Foggy",
        762              => "Volcanic ash",
        771              => "Squalls",
        781              => "Tornado",
        800              => "Clear sky",
        801              => "Few clouds",
        802              => "Scattered clouds",
        803 or 804       => "Overcast",
        _                => "Unknown"
    };

    private static bool IsRainyWeather(int code)
        => code is (>= 200 and < 300) or (>= 300 and < 400) or (>= 500 and < 600);
}