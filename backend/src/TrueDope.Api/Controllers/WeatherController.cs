using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrueDope.Api.DTOs;
using TrueDope.Api.DTOs.Weather;
using TrueDope.Api.Services;

namespace TrueDope.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;
    private readonly ILogger<WeatherController> _logger;

    public WeatherController(IWeatherService weatherService, ILogger<WeatherController> logger)
    {
        _weatherService = weatherService;
        _logger = logger;
    }

    /// <summary>
    /// Get current weather for the specified coordinates
    /// </summary>
    /// <param name="lat">Latitude (-90 to 90)</param>
    /// <param name="lon">Longitude (-180 to 180)</param>
    /// <param name="elevation">Optional elevation in feet for density altitude calculation</param>
    /// <returns>Current weather data</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<WeatherDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetWeather(
        [FromQuery] decimal lat,
        [FromQuery] decimal lon,
        [FromQuery] int? elevation = null)
    {
        // Validate coordinates
        if (lat < -90 || lat > 90)
        {
            return BadRequest(ApiErrorResponse.Create(
                "INVALID_LATITUDE",
                "Latitude must be between -90 and 90"));
        }

        if (lon < -180 || lon > 180)
        {
            return BadRequest(ApiErrorResponse.Create(
                "INVALID_LONGITUDE",
                "Longitude must be between -180 and 180"));
        }

        _logger.LogInformation("Weather requested for coordinates: {Lat}, {Lon}", lat, lon);

        var weather = await _weatherService.GetWeatherAsync(lat, lon, elevation);

        if (weather == null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiErrorResponse.Create(
                    "WEATHER_SERVICE_UNAVAILABLE",
                    "Unable to fetch weather data. Please try again later."));
        }

        return Ok(ApiResponse<WeatherDto>.Ok(weather));
    }
}
