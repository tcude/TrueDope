using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrueDope.Api.DTOs;
using TrueDope.Api.DTOs.SharedLocations;
using TrueDope.Api.Services;

namespace TrueDope.Api.Controllers;

[ApiController]
[Route("api/shared-locations")]
[Authorize]
public class SharedLocationsController : ControllerBase
{
    private readonly ISharedLocationService _sharedLocationService;
    private readonly ILogger<SharedLocationsController> _logger;

    public SharedLocationsController(
        ISharedLocationService sharedLocationService,
        ILogger<SharedLocationsController> logger)
    {
        _sharedLocationService = sharedLocationService;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User not authenticated");

    /// <summary>
    /// Get all active shared locations (available to all authenticated users)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] string? state = null)
    {
        var locations = await _sharedLocationService.GetActiveLocationsAsync(search, state);
        return Ok(ApiResponse<List<SharedLocationListDto>>.Ok(locations));
    }

    /// <summary>
    /// Get a single shared location by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var location = await _sharedLocationService.GetByIdAsync(id);

        if (location == null || !location.IsActive)
        {
            return NotFound(ApiResponse<object>.Fail("Shared location not found"));
        }

        // Return only the public DTO (without admin fields) for non-admins
        var publicDto = new SharedLocationListDto
        {
            Id = location.Id,
            Name = location.Name,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            Altitude = location.Altitude,
            Description = location.Description,
            City = location.City,
            State = location.State,
            Country = location.Country,
            Website = location.Website,
            PhoneNumber = location.PhoneNumber
        };

        return Ok(ApiResponse<SharedLocationListDto>.Ok(publicDto));
    }

    /// <summary>
    /// Copy a shared location to the user's saved locations
    /// </summary>
    [HttpPost("{id}/copy")]
    public async Task<IActionResult> CopyToSaved(int id)
    {
        try
        {
            var userId = GetUserId();
            var savedLocation = await _sharedLocationService.CopyToSavedAsync(id, userId);
            return Ok(ApiResponse<object>.Ok(savedLocation, "Location saved to your locations"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.Fail("Shared location not found"));
        }
    }

    // =====================
    // Admin Endpoints
    // =====================

    /// <summary>
    /// Get all shared locations including inactive (admin only)
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AdminGetAll([FromQuery] bool includeInactive = true)
    {
        var locations = await _sharedLocationService.GetAllLocationsAsync(includeInactive);
        return Ok(ApiResponse<List<SharedLocationAdminDto>>.Ok(locations));
    }

    /// <summary>
    /// Create a new shared location (admin only)
    /// </summary>
    [HttpPost("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AdminCreate([FromBody] CreateSharedLocationDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.Fail("Invalid request data"));
        }

        var userId = GetUserId();
        var id = await _sharedLocationService.CreateAsync(userId, dto);

        return CreatedAtAction(nameof(GetById), new { id }, ApiResponse<int>.Ok(id, "Shared location created"));
    }

    /// <summary>
    /// Update a shared location (admin only)
    /// </summary>
    [HttpPut("admin/{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AdminUpdate(int id, [FromBody] UpdateSharedLocationDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.Fail("Invalid request data"));
        }

        try
        {
            await _sharedLocationService.UpdateAsync(id, dto);
            var updated = await _sharedLocationService.GetByIdAsync(id);
            return Ok(ApiResponse<SharedLocationAdminDto?>.Ok(updated, "Shared location updated"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.Fail("Shared location not found"));
        }
    }

    /// <summary>
    /// Delete a shared location (admin only)
    /// </summary>
    [HttpDelete("admin/{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AdminDelete(int id)
    {
        try
        {
            await _sharedLocationService.DeleteAsync(id);
            return Ok(ApiResponse<object>.Ok(new { }, "Shared location deleted"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.Fail("Shared location not found"));
        }
    }
}
