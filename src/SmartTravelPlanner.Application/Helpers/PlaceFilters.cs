using SmartTravelPlanner.Domain.Constants;
using SmartTravelPlanner.Domain.Entities;
using SmartTravelPlanner.Domain.ValueObjects;

namespace SmartTravelPlanner.Application.Helpers;

public static class PlaceFilters
{
    public static bool IsForbidden(string category)
        => PlaceTypeConstants.ForbiddenCategories.Contains(category);

    public static IReadOnlyList<Place> FilterForbidden(IEnumerable<Place> places)
        => places
            .Where(p => !p.AllTypes.Any(t =>
                PlaceTypeConstants.ForbiddenCategories.Contains(t)))
            .ToList();

    public static bool DetermineIsIndoor(string category)
        => PlaceTypeConstants.IsIndoor(category);

    public static IReadOnlyList<Place> FilterByDistance(
        IEnumerable<Place> places,
        Coordinates cityCenter,
        decimal maxDistanceKm)
    {
        return places
            .Where(p =>
            {
                var placeCoord = new Coordinates(p.Latitude, p.Longitude);
                return cityCenter.DistanceToKm(placeCoord) <= maxDistanceKm;
            })
            .ToList();
    }
}