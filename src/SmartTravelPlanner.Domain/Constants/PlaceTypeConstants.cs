namespace SmartTravelPlanner.Domain.Constants;

public static class PlaceTypeConstants
{
    public static readonly IReadOnlyDictionary<string, string[]> InterestToPlaceTypes =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["museums"]  = ["museum", "art_gallery"],
            ["history"]  = ["tourist_attraction", "church", "mosque", "hindu_temple", "synagogue"],
            ["food"]     = ["restaurant", "cafe", "bakery"],
            ["shopping"] = ["shopping_mall", "store", "market"],
            ["nature"]   = ["park", "campground"],
            ["landmarks"]= ["tourist_attraction"],
            ["nightlife"]= ["night_club", "bar"],
            ["adventure"]= ["amusement_park"],
            ["beaches"]  = ["beach"],
            ["art"]      = ["art_gallery", "museum"],
            ["parks"]    = ["park", "campground", "natural_feature"],
        };

    public static readonly HashSet<string> ForbiddenCategories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "bar", "night_club", "lodging", "hotel", "motel", "casino",
            "real_estate_agency", "transit_station", "gas_station",
            "pharmacy", "information_center", "spa"
        };

    public static readonly HashSet<string> FoodRelatedTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "restaurant", "cafe", "bakery", "meal_delivery", "meal_takeaway"
        };

    public static readonly HashSet<string> IndoorCategories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "museum", "art_gallery", "church", "mosque", "synagogue",
            "hindu_temple", "restaurant", "cafe", "bakery", "shopping_mall",
            "store", "market"
        };

    public static readonly HashSet<string> OutdoorCategories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "park", "campground", "natural_feature", "tourist_attraction",
            "beach", "amusement_park"
        };

    public static readonly HashSet<string> KnownActivityTypes =
        new(InterestToPlaceTypes.Values
            .SelectMany(v => v)
            .Except(FoodRelatedTypes, StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);

    public static bool IsIndoor(string category)
        => IndoorCategories.Contains(category);
}