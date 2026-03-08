namespace SmartTravelPlanner.Domain.ValueObjects;

/// <summary>
/// Represents a geographic location with latitude and longitude.
/// </summary>
public sealed record Coordinates(decimal Latitude, decimal Longitude)
{
    /// <summary>
    /// Calculates the Haversine (great-circle) distance in kilometres between two coordinates.
    /// </summary>
    public decimal DistanceToKm(Coordinates other)
    {
        const double earthRadiusKm = 6371.0;

        var dLat = DegreesToRadians((double)(other.Latitude - Latitude));
        var dLon = DegreesToRadians((double)(other.Longitude - Longitude));

        var lat1Rad = DegreesToRadians((double)Latitude);
        var lat2Rad = DegreesToRadians((double)other.Latitude);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return (decimal)(earthRadiusKm * c);
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
