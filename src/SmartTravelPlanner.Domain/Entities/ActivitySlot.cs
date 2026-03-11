namespace SmartTravelPlanner.Domain.Entities;

public class ActivitySlot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DayPlanId { get; set; }
    public int OrderIndex { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string PlaceName { get; set; } = string.Empty;
    public string? PlaceAddress { get; set; }
    public decimal PlaceLatitude { get; set; }
    public decimal PlaceLongitude { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsIndoor { get; set; }
    public decimal EstimatedCostLocal { get; set; }
    public decimal EstimatedCostUser { get; set; }
    public int VisitDurationMinutes { get; set; }
    public decimal? TravelTimeFromPrevMinutes { get; set; }
    public decimal? TravelDistanceFromPrevKm { get; set; }

    public string? TransportMode { get; set; }

    public string? ExternalPlaceId { get; set; }

    public DayPlan DayPlan { get; set; } = null!;
}
