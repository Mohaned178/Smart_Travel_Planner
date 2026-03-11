namespace SmartTravelPlanner.Domain.Entities;

public class DayPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ItineraryId { get; set; }
    public int DayNumber { get; set; }
    public DateTime Date { get; set; }

    public string? WeatherSummary { get; set; }
    public int? WeatherCode { get; set; }
    public decimal? MaxTemperatureC { get; set; }
    public decimal? MinTemperatureC { get; set; }
    public decimal? PrecipitationMm { get; set; }

    public decimal DailyCostTotal { get; set; }

    public Itinerary Itinerary { get; set; } = null!;
    public ICollection<ActivitySlot> Activities { get; set; } = new List<ActivitySlot>();
    public ICollection<RestaurantSuggestion> Restaurants { get; set; } = new List<RestaurantSuggestion>();
}
