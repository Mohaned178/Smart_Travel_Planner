namespace SmartTravelPlanner.Domain.Entities;

/// <summary>
/// Optional dining recommendation attached to a day plan.
/// </summary>
public class RestaurantSuggestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DayPlanId { get; set; }

    /// <summary>One of: breakfast, lunch, dinner.</summary>
    public string MealSlot { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? CuisineType { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? DistanceFromActivityKm { get; set; }
    public decimal EstimatedMealCost { get; set; }
    public string? ExternalPlaceId { get; set; }

    // Navigation
    public DayPlan DayPlan { get; set; } = null!;
}
