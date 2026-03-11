namespace SmartTravelPlanner.Api.DTOs.Responses;

public record ItineraryResponse
{
    public Guid ItineraryId { get; init; }
    public string CityName { get; init; } = string.Empty;
    public string? CountryName { get; init; }
    public CoordinatesDto Coordinates { get; init; } = new();
    public decimal TotalBudget { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public int DurationDays { get; init; }
    public DateTime TripStartDate { get; init; }
    public List<DayPlanResponse> DayPlans { get; init; } = [];
    public CostBreakdownResponse? CostBreakdown { get; init; }
    public List<string> Notices { get; init; } = [];
    public string Status { get; init; } = "Draft";
    public DateTime CreatedAt { get; init; }
}

public record CoordinatesDto
{
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
}

public record DayPlanResponse
{
    public int DayNumber { get; init; }
    public DateTime Date { get; init; }
    public WeatherResponse? Weather { get; init; }
    public List<ActivitySlotResponse> Activities { get; init; } = [];
    public List<RestaurantResponse> Restaurants { get; init; } = [];
    public decimal DailyCostTotal { get; init; }
}

public record WeatherResponse
{
    public string? Summary { get; init; }
    public int WeatherCode { get; init; }
    public decimal MaxTemperatureC { get; init; }
    public decimal MinTemperatureC { get; init; }
    public decimal PrecipitationMm { get; init; }
}

public record ActivitySlotResponse
{
    public int OrderIndex { get; init; }
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public string PlaceName { get; init; } = string.Empty;
    public string? PlaceAddress { get; init; }
    public CoordinatesDto Coordinates { get; init; } = new();
    public string Category { get; init; } = string.Empty;
    public bool IsIndoor { get; init; }
    public decimal EstimatedCost { get; init; }
    public int VisitDurationMinutes { get; init; }
    public decimal? TravelTimeFromPrevMinutes { get; init; }
    public decimal? TravelDistanceFromPrevKm { get; init; }
    public string? TransportMode { get; init; }
}

public record RestaurantResponse
{
    public string MealSlot { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? CuisineType { get; init; }
    public CoordinatesDto Coordinates { get; init; } = new();
    public decimal? DistanceFromActivityKm { get; init; }
    public decimal EstimatedMealCost { get; init; }
    public string? MealTime { get; init; }
}

public record CostBreakdownResponse
{
    public decimal TotalActivitiesCost { get; init; }
    public decimal TotalDiningCost { get; init; }
    public decimal TotalTransportCost { get; init; }
    public decimal GrandTotal { get; init; }
    public decimal RemainingBudget { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
}

public record ItinerarySummaryResponse
{
    public Guid ItineraryId { get; init; }
    public string CityName { get; init; } = string.Empty;
    public int DurationDays { get; init; }
    public decimal TotalBudget { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public DateTime TripStartDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record PaginatedResponse<T>
{
    public List<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}
