namespace SmartTravelPlanner.Domain.Enums;

/// <summary>
/// Weather conditions derived from WMO weather interpretation codes.
/// </summary>
public enum WeatherCondition
{
    /// <summary>WMO code 0</summary>
    Clear,

    /// <summary>WMO codes 1–3</summary>
    PartlyCloudy,

    /// <summary>WMO codes 45–48</summary>
    Foggy,

    /// <summary>WMO codes 51–67, 80–82</summary>
    Rain,

    /// <summary>WMO codes 71–77</summary>
    Snow,

    /// <summary>WMO codes 95–99</summary>
    Thunderstorm
}
