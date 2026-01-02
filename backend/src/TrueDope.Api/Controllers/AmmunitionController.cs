using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrueDope.Api.DTOs;
using TrueDope.Api.DTOs.Ammunition;
using TrueDope.Api.Services;

namespace TrueDope.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AmmunitionController : ControllerBase
{
    private readonly IAmmoService _ammoService;
    private readonly ILogger<AmmunitionController> _logger;

    public AmmunitionController(IAmmoService ammoService, ILogger<AmmunitionController> logger)
    {
        _ammoService = ammoService;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Get all ammunition for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<AmmoListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAmmunition([FromQuery] AmmoFilterDto filter)
    {
        var result = await _ammoService.GetAmmoAsync(GetUserId(), filter);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific ammunition by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AmmoDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAmmo(int id)
    {
        var ammo = await _ammoService.GetAmmoAsync(GetUserId(), id);

        if (ammo == null)
            return NotFound(ApiErrorResponse.Create("AMMO_NOT_FOUND", "Ammunition not found"));

        return Ok(ApiResponse<AmmoDetailDto>.Ok(ammo));
    }

    /// <summary>
    /// Create new ammunition
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAmmo([FromBody] CreateAmmoDto dto)
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

        var ammoId = await _ammoService.CreateAmmoAsync(GetUserId(), dto);
        TrueDopeMetrics.RecordAmmunitionCreated();
        return CreatedAtAction(nameof(GetAmmo), new { id = ammoId },
            ApiResponse<int>.Ok(ammoId, "Ammunition created successfully"));
    }

    /// <summary>
    /// Update ammunition
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AmmoDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAmmo(int id, [FromBody] UpdateAmmoDto dto)
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

        var updatedAmmo = await _ammoService.UpdateAmmoAsync(GetUserId(), id, dto);

        if (updatedAmmo == null)
            return NotFound(ApiErrorResponse.Create("AMMO_NOT_FOUND", "Ammunition not found"));

        return Ok(ApiResponse<AmmoDetailDto>.Ok(updatedAmmo, "Ammunition updated successfully"));
    }

    /// <summary>
    /// Delete ammunition
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAmmo(int id)
    {
        try
        {
            var deleted = await _ammoService.DeleteAmmoAsync(GetUserId(), id);

            if (!deleted)
                return NotFound(ApiErrorResponse.Create("AMMO_NOT_FOUND", "Ammunition not found"));

            return Ok(ApiResponse.Ok("Ammunition deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiErrorResponse.Create("AMMO_HAS_SESSIONS", ex.Message));
        }
    }

    /// <summary>
    /// Check if ammunition has any sessions
    /// </summary>
    [HttpGet("{id:int}/has-sessions")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HasSessions(int id)
    {
        var ammo = await _ammoService.GetAmmoAsync(GetUserId(), id);

        if (ammo == null)
            return NotFound(ApiErrorResponse.Create("AMMO_NOT_FOUND", "Ammunition not found"));

        var hasSessions = await _ammoService.HasSessionsAsync(GetUserId(), id);
        return Ok(ApiResponse<bool>.Ok(hasSessions));
    }

    // ==================== Lot Endpoints ====================

    /// <summary>
    /// Get all lots for a specific ammunition
    /// </summary>
    [HttpGet("{ammoId:int}/lots")]
    [ProducesResponseType(typeof(ApiResponse<List<AmmoLotDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLots(int ammoId)
    {
        var ammo = await _ammoService.GetAmmoAsync(GetUserId(), ammoId);

        if (ammo == null)
            return NotFound(ApiErrorResponse.Create("AMMO_NOT_FOUND", "Ammunition not found"));

        var lots = await _ammoService.GetLotsAsync(GetUserId(), ammoId);
        return Ok(ApiResponse<List<AmmoLotDto>>.Ok(lots));
    }

    /// <summary>
    /// Create a new lot for ammunition
    /// </summary>
    [HttpPost("{ammoId:int}/lots")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateLot(int ammoId, [FromBody] CreateAmmoLotDto dto)
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

        try
        {
            var lotId = await _ammoService.CreateLotAsync(GetUserId(), ammoId, dto);
            return CreatedAtAction(nameof(GetLots), new { ammoId },
                ApiResponse<int>.Ok(lotId, "Lot created successfully"));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiErrorResponse.Create("AMMO_NOT_FOUND", ex.Message));
        }
    }

    /// <summary>
    /// Update a lot
    /// </summary>
    [HttpPut("{ammoId:int}/lots/{lotId:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLot(int ammoId, int lotId, [FromBody] UpdateAmmoLotDto dto)
    {
        var updated = await _ammoService.UpdateLotAsync(GetUserId(), ammoId, lotId, dto);

        if (!updated)
            return NotFound(ApiErrorResponse.Create("LOT_NOT_FOUND", "Lot not found"));

        return Ok(ApiResponse.Ok("Lot updated successfully"));
    }

    /// <summary>
    /// Delete a lot
    /// </summary>
    [HttpDelete("{ammoId:int}/lots/{lotId:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteLot(int ammoId, int lotId)
    {
        try
        {
            var deleted = await _ammoService.DeleteLotAsync(GetUserId(), ammoId, lotId);

            if (!deleted)
                return NotFound(ApiErrorResponse.Create("LOT_NOT_FOUND", "Lot not found"));

            return Ok(ApiResponse.Ok("Lot deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiErrorResponse.Create("LOT_HAS_SESSIONS", ex.Message));
        }
    }
}
