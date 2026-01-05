using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrueDope.Api.DTOs;
using TrueDope.Api.DTOs.Sessions;
using TrueDope.Api.Services;

namespace TrueDope.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(ISessionService sessionService, ILogger<SessionsController> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Get all range sessions for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<SessionListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSessions([FromQuery] SessionFilterDto filter)
    {
        var result = await _sessionService.GetSessionsAsync(GetUserId(), filter);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific session by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<SessionDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSession(int id)
    {
        var session = await _sessionService.GetSessionAsync(GetUserId(), id);

        if (session == null)
            return NotFound(ApiErrorResponse.Create("SESSION_NOT_FOUND", "Session not found"));

        return Ok(ApiResponse<SessionDetailDto>.Ok(session));
    }

    /// <summary>
    /// Create a new range session
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionDto dto)
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
            var sessionId = await _sessionService.CreateSessionAsync(GetUserId(), dto);
            TrueDopeMetrics.RecordSessionCreated();
            return CreatedAtAction(nameof(GetSession), new { id = sessionId },
                ApiResponse<int>.Ok(sessionId, "Session created successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiErrorResponse.Create("VALIDATION_ERROR", ex.Message));
        }
    }

    /// <summary>
    /// Update a session
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<SessionDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSession(int id, [FromBody] UpdateSessionDto dto)
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
            var updated = await _sessionService.UpdateSessionAsync(GetUserId(), id, dto);

            if (updated == null)
                return NotFound(ApiErrorResponse.Create("SESSION_NOT_FOUND", "Session not found"));

            return Ok(ApiResponse<SessionDetailDto>.Ok(updated, "Session updated successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiErrorResponse.Create("VALIDATION_ERROR", ex.Message));
        }
    }

    /// <summary>
    /// Delete a session
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSession(int id)
    {
        var deleted = await _sessionService.DeleteSessionAsync(GetUserId(), id);

        if (!deleted)
            return NotFound(ApiErrorResponse.Create("SESSION_NOT_FOUND", "Session not found"));

        return Ok(ApiResponse.Ok("Session deleted successfully"));
    }

    // ==================== DOPE Entry Endpoints ====================

    /// <summary>
    /// Add a DOPE entry to a session
    /// </summary>
    [HttpPost("{sessionId:int}/dope")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddDopeEntry(int sessionId, [FromBody] CreateDopeEntryDto dto)
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
            var dopeId = await _sessionService.AddDopeEntryAsync(GetUserId(), sessionId, dto);
            TrueDopeMetrics.RecordDopeEntry();
            return CreatedAtAction(nameof(GetSession), new { id = sessionId },
                ApiResponse<int>.Ok(dopeId, "DOPE entry added successfully"));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiErrorResponse.Create("SESSION_NOT_FOUND", ex.Message));
        }
    }

    /// <summary>
    /// Update a DOPE entry
    /// </summary>
    [HttpPut("dope/{dopeEntryId:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDopeEntry(int dopeEntryId, [FromBody] UpdateDopeEntryDto dto)
    {
        var updated = await _sessionService.UpdateDopeEntryAsync(GetUserId(), dopeEntryId, dto);

        if (!updated)
            return NotFound(ApiErrorResponse.Create("DOPE_NOT_FOUND", "DOPE entry not found"));

        return Ok(ApiResponse.Ok("DOPE entry updated successfully"));
    }

    /// <summary>
    /// Delete a DOPE entry
    /// </summary>
    [HttpDelete("dope/{dopeEntryId:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDopeEntry(int dopeEntryId)
    {
        var deleted = await _sessionService.DeleteDopeEntryAsync(GetUserId(), dopeEntryId);

        if (!deleted)
            return NotFound(ApiErrorResponse.Create("DOPE_NOT_FOUND", "DOPE entry not found"));

        return Ok(ApiResponse.Ok("DOPE entry deleted successfully"));
    }

    // ==================== Chrono Session Endpoints ====================

    /// <summary>
    /// Add a chrono session to a range session
    /// </summary>
    [HttpPost("{sessionId:int}/chrono")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddChronoSession(int sessionId, [FromBody] CreateChronoSessionDto dto)
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
            var chronoId = await _sessionService.AddChronoSessionAsync(GetUserId(), sessionId, dto);
            TrueDopeMetrics.RecordChronoSession();
            return CreatedAtAction(nameof(GetSession), new { id = sessionId },
                ApiResponse<int>.Ok(chronoId, "Chrono session added successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiErrorResponse.Create("VALIDATION_ERROR", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiErrorResponse.Create("CHRONO_EXISTS", ex.Message));
        }
    }

    /// <summary>
    /// Update a chrono session
    /// </summary>
    [HttpPut("chrono/{chronoSessionId:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateChronoSession(int chronoSessionId, [FromBody] UpdateChronoSessionDto dto)
    {
        try
        {
            var updated = await _sessionService.UpdateChronoSessionAsync(GetUserId(), chronoSessionId, dto);

            if (!updated)
                return NotFound(ApiErrorResponse.Create("CHRONO_NOT_FOUND", "Chrono session not found"));

            return Ok(ApiResponse.Ok("Chrono session updated successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiErrorResponse.Create("VALIDATION_ERROR", ex.Message));
        }
    }

    /// <summary>
    /// Delete a chrono session
    /// </summary>
    [HttpDelete("chrono/{chronoSessionId:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteChronoSession(int chronoSessionId)
    {
        var deleted = await _sessionService.DeleteChronoSessionAsync(GetUserId(), chronoSessionId);

        if (!deleted)
            return NotFound(ApiErrorResponse.Create("CHRONO_NOT_FOUND", "Chrono session not found"));

        return Ok(ApiResponse.Ok("Chrono session deleted successfully"));
    }

    /// <summary>
    /// Add a velocity reading to a chrono session
    /// </summary>
    [HttpPost("chrono/{chronoSessionId:int}/readings")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddVelocityReading(int chronoSessionId, [FromBody] CreateVelocityReadingDto dto)
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
            var readingId = await _sessionService.AddVelocityReadingAsync(GetUserId(), chronoSessionId, dto);
            TrueDopeMetrics.RecordVelocityReading();
            return Created("", ApiResponse<int>.Ok(readingId, "Velocity reading added successfully"));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiErrorResponse.Create("CHRONO_NOT_FOUND", ex.Message));
        }
    }

    /// <summary>
    /// Delete a velocity reading
    /// </summary>
    [HttpDelete("readings/{readingId:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVelocityReading(int readingId)
    {
        var deleted = await _sessionService.DeleteVelocityReadingAsync(GetUserId(), readingId);

        if (!deleted)
            return NotFound(ApiErrorResponse.Create("READING_NOT_FOUND", "Velocity reading not found"));

        return Ok(ApiResponse.Ok("Velocity reading deleted successfully"));
    }

    // ==================== Group Entry Endpoints ====================

    /// <summary>
    /// Add a group entry to a session
    /// </summary>
    [HttpPost("{sessionId:int}/groups")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddGroupEntry(int sessionId, [FromBody] CreateGroupEntryDto dto)
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
            var groupId = await _sessionService.AddGroupEntryAsync(GetUserId(), sessionId, dto);
            TrueDopeMetrics.RecordGroupEntry();
            return CreatedAtAction(nameof(GetSession), new { id = sessionId },
                ApiResponse<int>.Ok(groupId, "Group entry added successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiErrorResponse.Create("VALIDATION_ERROR", ex.Message));
        }
    }

    /// <summary>
    /// Update a group entry
    /// </summary>
    [HttpPut("groups/{groupEntryId:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateGroupEntry(int groupEntryId, [FromBody] UpdateGroupEntryDto dto)
    {
        try
        {
            var updated = await _sessionService.UpdateGroupEntryAsync(GetUserId(), groupEntryId, dto);

            if (!updated)
                return NotFound(ApiErrorResponse.Create("GROUP_NOT_FOUND", "Group entry not found"));

            return Ok(ApiResponse.Ok("Group entry updated successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiErrorResponse.Create("VALIDATION_ERROR", ex.Message));
        }
    }

    /// <summary>
    /// Delete a group entry
    /// </summary>
    [HttpDelete("groups/{groupEntryId:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGroupEntry(int groupEntryId)
    {
        var deleted = await _sessionService.DeleteGroupEntryAsync(GetUserId(), groupEntryId);

        if (!deleted)
            return NotFound(ApiErrorResponse.Create("GROUP_NOT_FOUND", "Group entry not found"));

        return Ok(ApiResponse.Ok("Group entry deleted successfully"));
    }
}
