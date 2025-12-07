using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrueDope.Api.DTOs;
using TrueDope.Api.DTOs.Rifles;
using TrueDope.Api.Services;

namespace TrueDope.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RiflesController : ControllerBase
{
    private readonly IRifleService _rifleService;
    private readonly ILogger<RiflesController> _logger;

    public RiflesController(IRifleService rifleService, ILogger<RiflesController> logger)
    {
        _rifleService = rifleService;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Get all rifles for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<RifleListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetRifles([FromQuery] RifleFilterDto filter)
    {
        var result = await _rifleService.GetRiflesAsync(GetUserId(), filter);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific rifle by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<RifleDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRifle(int id)
    {
        var rifle = await _rifleService.GetRifleAsync(GetUserId(), id);

        if (rifle == null)
            return NotFound(ApiErrorResponse.Create("RIFLE_NOT_FOUND", "Rifle not found"));

        return Ok(ApiResponse<RifleDetailDto>.Ok(rifle));
    }

    /// <summary>
    /// Create a new rifle
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRifle([FromBody] CreateRifleDto dto)
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

        var rifleId = await _rifleService.CreateRifleAsync(GetUserId(), dto);
        return CreatedAtAction(nameof(GetRifle), new { id = rifleId },
            ApiResponse<int>.Ok(rifleId, "Rifle created successfully"));
    }

    /// <summary>
    /// Update a rifle
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateRifle(int id, [FromBody] UpdateRifleDto dto)
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

        var updated = await _rifleService.UpdateRifleAsync(GetUserId(), id, dto);

        if (!updated)
            return NotFound(ApiErrorResponse.Create("RIFLE_NOT_FOUND", "Rifle not found"));

        return Ok(ApiResponse.Ok("Rifle updated successfully"));
    }

    /// <summary>
    /// Delete a rifle
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteRifle(int id)
    {
        try
        {
            var deleted = await _rifleService.DeleteRifleAsync(GetUserId(), id);

            if (!deleted)
                return NotFound(ApiErrorResponse.Create("RIFLE_NOT_FOUND", "Rifle not found"));

            return Ok(ApiResponse.Ok("Rifle deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiErrorResponse.Create("RIFLE_HAS_SESSIONS", ex.Message));
        }
    }

    /// <summary>
    /// Check if rifle has any sessions
    /// </summary>
    [HttpGet("{id:int}/has-sessions")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HasSessions(int id)
    {
        var rifle = await _rifleService.GetRifleAsync(GetUserId(), id);

        if (rifle == null)
            return NotFound(ApiErrorResponse.Create("RIFLE_NOT_FOUND", "Rifle not found"));

        var hasSessions = await _rifleService.HasSessionsAsync(GetUserId(), id);
        return Ok(ApiResponse<bool>.Ok(hasSessions));
    }
}
