using Microsoft.AspNetCore.Identity;

namespace SmartTravelPlanner.Domain.Entities;

/// <summary>
/// Registered application user. Extends ASP.NET Core Identity with custom fields.
/// </summary>
public class User : IdentityUser<Guid>
{
    /// <summary>Optional display name, max 100 characters.</summary>
    public string? DisplayName { get; set; }

    /// <summary>UTC timestamp when the account was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of last successful login.</summary>
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public ICollection<Itinerary> Itineraries { get; set; } = new List<Itinerary>();
}
