namespace TrueDope.Api.DTOs.Weather;

/// <summary>
/// Weather data response DTO
/// </summary>
public class WeatherDto
{
    /// <summary>
    /// Temperature in Fahrenheit
    /// </summary>
    public decimal Temperature { get; set; }

    /// <summary>
    /// Humidity percentage (0-100)
    /// </summary>
    public int Humidity { get; set; }

    /// <summary>
    /// Barometric pressure in inches of mercury (inHg)
    /// </summary>
    public decimal Pressure { get; set; }

    /// <summary>
    /// Wind speed in miles per hour
    /// </summary>
    public decimal WindSpeed { get; set; }

    /// <summary>
    /// Wind direction in degrees (0-359)
    /// </summary>
    public int WindDirection { get; set; }

    /// <summary>
    /// Wind direction as cardinal direction (N, NE, E, etc.)
    /// </summary>
    public string WindDirectionCardinal { get; set; } = string.Empty;

    /// <summary>
    /// Calculated density altitude in feet (optional, requires elevation)
    /// </summary>
    public int? DensityAltitude { get; set; }

    /// <summary>
    /// Weather description (e.g., "Partly cloudy")
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Request parameters for weather fetch
/// </summary>
public class WeatherRequestDto
{
    /// <summary>
    /// Latitude (-90 to 90)
    /// </summary>
    public decimal Latitude { get; set; }

    /// <summary>
    /// Longitude (-180 to 180)
    /// </summary>
    public decimal Longitude { get; set; }

    /// <summary>
    /// Optional elevation in feet for density altitude calculation
    /// </summary>
    public int? Elevation { get; set; }
}

/// <summary>
/// OpenWeatherMap API response structure (internal use)
/// </summary>
internal class OpenWeatherMapResponse
{
    public OpenWeatherMapMain Main { get; set; } = new();
    public OpenWeatherMapWind Wind { get; set; } = new();
    public List<OpenWeatherMapWeather> Weather { get; set; } = new();
}

internal class OpenWeatherMapMain
{
    public decimal Temp { get; set; }  // Kelvin
    public int Humidity { get; set; }
    public decimal Pressure { get; set; }  // hPa
}

internal class OpenWeatherMapWind
{
    public decimal Speed { get; set; }  // m/s
    public int Deg { get; set; }
}

internal class OpenWeatherMapWeather
{
    public string Description { get; set; } = string.Empty;
}
