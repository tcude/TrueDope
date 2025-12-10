using TrueDope.Api.DTOs.Weather;

namespace TrueDope.Api.Services;

public interface IWeatherService
{
    /// <summary>
    /// Fetches current weather data for the given coordinates
    /// </summary>
    /// <param name="latitude">Latitude (-90 to 90)</param>
    /// <param name="longitude">Longitude (-180 to 180)</param>
    /// <param name="elevation">Optional elevation in feet for density altitude calculation</param>
    /// <returns>Weather data or null if fetch failed</returns>
    Task<WeatherDto?> GetWeatherAsync(decimal latitude, decimal longitude, int? elevation = null);
}
