using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrueDope.Api.DTOs;
using TrueDope.Api.DTOs.Locations;
using TrueDope.Api.Services;

namespace TrueDope.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly ILogger<LocationsController> _logger;

    public LocationsController(ILocationService locationService, ILogger<LocationsController> logger)
    {
        _locationService = locationService;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Get all saved locations for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<LocationListDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetLocations()
    {
        var locations = await _locationService.GetLocationsAsync(GetUserId());
        return Ok(ApiResponse<List<LocationListDto>>.Ok(locations));
    }

    /// <summary>
    /// Get a specific location by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<LocationDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLocation(int id)
    {
        var location = await _locationService.GetLocationAsync(GetUserId(), id);

        if (location == null)
            return NotFound(ApiErrorResponse.Create("LOCATION_NOT_FOUND", "Location not found"));

        return Ok(ApiResponse<LocationDetailDto>.Ok(location));
    }

    /// <summary>
    /// Create a new saved location
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLocation([FromBody] CreateLocationDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            return BadRequest(ApiErrorResponse.ValidationError("Validation failed", errors));
        }

        var locationId = await _locationService.CreateLocationAsync(GetUserId(), dto);
        TrueDopeMetrics.RecordLocationCreated();
        return CreatedAtAction(nameof(GetLocation), new { id = locationId },
            ApiResponse<int>.Ok(locationId, "Location created successfully"));
    }

    /// <summary>
    /// Update a location
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateLocation(int id, [FromBody] UpdateLocationDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            return BadRequest(ApiErrorResponse.ValidationError("Validation failed", errors));
        }

        var updated = await _locationService.UpdateLocationAsync(GetUserId(), id, dto);

        if (!updated)
            return NotFound(ApiErrorResponse.Create("LOCATION_NOT_FOUND", "Location not found"));

        return Ok(ApiResponse.Ok("Location updated successfully"));
    }

    /// <summary>
    /// Delete a location
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteLocation(int id)
    {
        try
        {
            var deleted = await _locationService.DeleteLocationAsync(GetUserId(), id);

            if (!deleted)
                return NotFound(ApiErrorResponse.Create("LOCATION_NOT_FOUND", "Location not found"));

            return Ok(ApiResponse.Ok("Location deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiErrorResponse.Create("LOCATION_HAS_SESSIONS", ex.Message));
        }
    }
}
