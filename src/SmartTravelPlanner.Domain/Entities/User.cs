using Microsoft.AspNetCore.Identity;

namespace SmartTravelPlanner.Domain.Entities;

public class User : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public ICollection<Itinerary> Itineraries { get; set; } = new List<Itinerary>();
}
