namespace SmartTravelPlanner.Domain.Entities;

/// <summary>
/// Top-level trip plan entity. Aggregate root for the itinerary bounded context.
/// </summary>
public class Itinerary
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public string? CountryName { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? Timezone { get; set; }
    public decimal TotalBudget { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public DateTime TripStartDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Draft or Saved.</summary>
    public string Status { get; set; } = "Draft";

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<DayPlan> DayPlans { get; set; } = new List<DayPlan>();
    public CostBreakdown? CostBreakdown { get; set; }
}
