namespace SmartTravelPlanner.Domain.Entities;

/// <summary>
/// A single day within an itinerary, including weather and activities.
/// </summary>
public class DayPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ItineraryId { get; set; }
    public int DayNumber { get; set; }
    public DateTime Date { get; set; }

    // Weather data from Open-Meteo
    public string? WeatherSummary { get; set; }
    public int? WeatherCode { get; set; }
    public decimal? MaxTemperatureC { get; set; }
    public decimal? MinTemperatureC { get; set; }
    public decimal? PrecipitationMm { get; set; }

    public decimal DailyCostTotal { get; set; }

    // Navigation properties
    public Itinerary Itinerary { get; set; } = null!;
    public ICollection<ActivitySlot> Activities { get; set; } = new List<ActivitySlot>();
    public ICollection<RestaurantSuggestion> Restaurants { get; set; } = new List<RestaurantSuggestion>();
}
