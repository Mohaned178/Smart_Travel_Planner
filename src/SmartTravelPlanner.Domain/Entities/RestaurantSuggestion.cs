namespace SmartTravelPlanner.Domain.Entities;

public class RestaurantSuggestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DayPlanId { get; set; }

    public string MealSlot { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? CuisineType { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? DistanceFromActivityKm { get; set; }
    public decimal EstimatedMealCost { get; set; }

    public TimeOnly? MealTime { get; set; }

    public string? ExternalPlaceId { get; set; }

    public DayPlan DayPlan { get; set; } = null!;
}
