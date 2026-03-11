namespace SmartTravelPlanner.Application.DTOs;

public record GenerateItineraryCommand
{
    public string CityName { get; init; } = string.Empty;
    public decimal TotalBudget { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public int DurationDays { get; init; }
    public DateTime TripStartDate { get; init; }
    public List<string> Interests { get; init; } = [];
    public bool IncludeRestaurants { get; init; }
    public bool IncludeAccommodations { get; init; }
    public List<string>? CuisinePreferences { get; init; }
}
