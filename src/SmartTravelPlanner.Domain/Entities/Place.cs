namespace SmartTravelPlanner.Domain.Entities;

/// <summary>
/// Cached place data from OpenTripMap. Used internally to avoid repeated API calls.
/// </summary>
public class Place
{
    /// <summary>OpenTripMap external ID (xid).</summary>
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

    /// <summary>UTC timestamp when this entry was cached.</summary>
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
}
