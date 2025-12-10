using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TrueDope.Api.Configuration;
using TrueDope.Api.DTOs.Weather;

namespace TrueDope.Api.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly WeatherSettings _settings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WeatherService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public WeatherService(
        HttpClient httpClient,
        IOptions<WeatherSettings> settings,
        IMemoryCache cache,
        ILogger<WeatherService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task<WeatherDto?> GetWeatherAsync(decimal latitude, decimal longitude, int? elevation = null)
    {
        if (string.IsNullOrEmpty(_settings.ApiKey))
        {
            _logger.LogWarning("OpenWeatherMap API key not configured");
            return null;
        }

        // Round coordinates to 3 decimal places for cache key (about 110m precision)
        var cacheKey = $"weather:{Math.Round(latitude, 3)}:{Math.Round(longitude, 3)}";

        if (_cache.TryGetValue(cacheKey, out WeatherDto? cachedWeather))
        {
            _logger.LogDebug("Returning cached weather for {Lat}, {Lon}", latitude, longitude);

            // Recalculate density altitude if elevation provided and different
            if (elevation.HasValue && cachedWeather != null)
            {
                cachedWeather.DensityAltitude = CalculateDensityAltitude(
                    cachedWeather.Temperature,
                    cachedWeather.Pressure,
                    elevation.Value);
            }

            return cachedWeather;
        }

        try
        {
            var url = $"{_settings.BaseUrl}?lat={latitude}&lon={longitude}&appid={_settings.ApiKey}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenWeatherMap API returned {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var owmResponse = JsonSerializer.Deserialize<OpenWeatherMapResponse>(json, JsonOptions);

            if (owmResponse == null)
            {
                _logger.LogWarning("Failed to parse OpenWeatherMap response");
                return null;
            }

            var weather = new WeatherDto
            {
                Temperature = KelvinToFahrenheit(owmResponse.Main.Temp),
                Humidity = owmResponse.Main.Humidity,
                Pressure = HpaToInHg(owmResponse.Main.Pressure),
                WindSpeed = MpsToMph(owmResponse.Wind.Speed),
                WindDirection = owmResponse.Wind.Deg,
                WindDirectionCardinal = DegreesToCardinal(owmResponse.Wind.Deg),
                Description = owmResponse.Weather.FirstOrDefault()?.Description ?? "Unknown"
            };

            // Calculate density altitude if elevation provided
            if (elevation.HasValue)
            {
                weather.DensityAltitude = CalculateDensityAltitude(
                    weather.Temperature,
                    weather.Pressure,
                    elevation.Value);
            }

            // Cache the result
            _cache.Set(cacheKey, weather, TimeSpan.FromMinutes(_settings.CacheMinutes));

            _logger.LogInformation("Fetched weather for {Lat}, {Lon}: {Temp}F, {Humidity}%",
                latitude, longitude, weather.Temperature, weather.Humidity);

            return weather;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch weather from OpenWeatherMap");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse OpenWeatherMap response");
            return null;
        }
    }

    /// <summary>
    /// Convert Kelvin to Fahrenheit
    /// </summary>
    private static decimal KelvinToFahrenheit(decimal kelvin)
    {
        return Math.Round((kelvin - 273.15m) * 9 / 5 + 32, 1);
    }

    /// <summary>
    /// Convert hectopascals (hPa) to inches of mercury (inHg)
    /// </summary>
    private static decimal HpaToInHg(decimal hpa)
    {
        return Math.Round(hpa * 0.02953m, 2);
    }

    /// <summary>
    /// Convert meters per second to miles per hour
    /// </summary>
    private static decimal MpsToMph(decimal mps)
    {
        return Math.Round(mps * 2.237m, 1);
    }

    /// <summary>
    /// Convert degrees to cardinal direction
    /// </summary>
    private static string DegreesToCardinal(int degrees)
    {
        var directions = new[] { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE",
                                 "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };
        var index = (int)Math.Round(degrees / 22.5) % 16;
        return directions[index];
    }

    /// <summary>
    /// Calculate density altitude using temperature, pressure, and elevation
    /// Uses a simplified formula suitable for ballistic calculations
    /// </summary>
    /// <param name="tempF">Temperature in Fahrenheit</param>
    /// <param name="pressureInHg">Pressure in inches of mercury</param>
    /// <param name="elevationFt">Elevation in feet</param>
    /// <returns>Density altitude in feet</returns>
    private static int CalculateDensityAltitude(decimal tempF, decimal pressureInHg, int elevationFt)
    {
        // Standard pressure at sea level in inHg
        const double stdPressure = 29.92;

        // Calculate pressure altitude
        var pressureAlt = (1 - Math.Pow((double)pressureInHg / stdPressure, 0.190284)) * 145366.45;

        // Calculate ISA temperature at pressure altitude (decreases 3.56F per 1000ft)
        var isaTempF = 59.0 - (pressureAlt * 0.00356);

        // Calculate density altitude
        // DA = PA + (120 * (OAT - ISA temp at PA))
        var densityAlt = pressureAlt + (120 * ((double)tempF - isaTempF));

        return (int)Math.Round(densityAlt);
    }
}
