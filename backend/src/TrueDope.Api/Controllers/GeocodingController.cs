using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrueDope.Api.DTOs;
using TrueDope.Api.Services;

namespace TrueDope.Api.Controllers;

[ApiController]
[Route("api/geocoding")]
[Authorize]
public class GeocodingController : ControllerBase
{
    private readonly IGeocodingService _geocodingService;
    private readonly ILogger<GeocodingController> _logger;

    public GeocodingController(
        IGeocodingService geocodingService,
        ILogger<GeocodingController> logger)
    {
        _geocodingService = geocodingService;
        _logger = logger;
    }

    /// <summary>
    /// Search for locations by query string (forward geocoding)
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(ApiResponse<object>.Fail("Query parameter 'q' is required"));
        }

        if (q.Length < 3)
        {
            return BadRequest(ApiResponse<object>.Fail("Query must be at least 3 characters"));
        }

        if (limit < 1 || limit > 10)
        {
            limit = 5;
        }

        var results = await _geocodingService.SearchAsync(q, limit);
        return Ok(ApiResponse<object>.Ok(results));
    }

    /// <summary>
    /// Get address information from coordinates (reverse geocoding)
    /// </summary>
    [HttpGet("reverse")]
    public async Task<IActionResult> Reverse([FromQuery] decimal lat, [FromQuery] decimal lon)
    {
        if (lat < -90 || lat > 90)
        {
            return BadRequest(ApiResponse<object>.Fail("Latitude must be between -90 and 90"));
        }

        if (lon < -180 || lon > 180)
        {
            return BadRequest(ApiResponse<object>.Fail("Longitude must be between -180 and 180"));
        }

        var result = await _geocodingService.ReverseGeocodeAsync(lat, lon);

        if (result == null)
        {
            return NotFound(ApiResponse<object>.Fail("No location found for these coordinates"));
        }

        return Ok(ApiResponse<object>.Ok(result));
    }

    /// <summary>
    /// Get elevation for coordinates
    /// </summary>
    [HttpGet("elevation")]
    public async Task<IActionResult> GetElevation([FromQuery] decimal lat, [FromQuery] decimal lon)
    {
        if (lat < -90 || lat > 90)
        {
            return BadRequest(ApiResponse<object>.Fail("Latitude must be between -90 and 90"));
        }

        if (lon < -180 || lon > 180)
        {
            return BadRequest(ApiResponse<object>.Fail("Longitude must be between -180 and 180"));
        }

        var result = await _geocodingService.GetElevationAsync(lat, lon);

        if (result == null)
        {
            return StatusCode(503, ApiResponse<object>.Fail("Elevation service unavailable"));
        }

        return Ok(ApiResponse<object>.Ok(result));
    }
}
