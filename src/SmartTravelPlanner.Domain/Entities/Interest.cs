using SmartTravelPlanner.Domain.Enums;

namespace SmartTravelPlanner.Domain.Entities;

/// <summary>
/// A predefined travel interest from the catalog.
/// </summary>
public class Interest
{
    public int Id { get; set; }

    /// <summary>Internal name used for matching (e.g., "museums").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The interest category mapping to OpenTripMap.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Human-readable display name (e.g., "Museums").</summary>
    public string DisplayName { get; set; } = string.Empty;
}
