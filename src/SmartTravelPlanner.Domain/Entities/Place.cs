namespace SmartTravelPlanner.Domain.Entities;

public class Place
{
    public string ExternalId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsIndoor { get; set; }
    public decimal EstimatedCost { get; set; }
    public int TypicalVisitMinutes { get; set; } = 60;
    public decimal? Rating { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
    public List<string> AllTypes { get; set; } = [];
}
