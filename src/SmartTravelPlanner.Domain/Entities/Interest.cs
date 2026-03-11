using SmartTravelPlanner.Domain.Enums;

namespace SmartTravelPlanner.Domain.Entities;

public class Interest
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}
