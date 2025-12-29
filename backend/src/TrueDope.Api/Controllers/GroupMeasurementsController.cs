using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrueDope.Api.Data;
using TrueDope.Api.Data.Entities;
using TrueDope.Api.DTOs;
using TrueDope.Api.DTOs.Sessions;
using TrueDope.Api.Services;

namespace TrueDope.Api.Controllers;

[ApiController]
[Route("api/groups/{groupId:int}/measurement")]
[Authorize]
public class GroupMeasurementsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IGroupMeasurementCalculator _calculator;
    private readonly ILogger<GroupMeasurementsController> _logger;

    public GroupMeasurementsController(
        ApplicationDbContext context,
        IGroupMeasurementCalculator calculator,
        ILogger<GroupMeasurementsController> logger)
    {
        _context = context;
        _calculator = calculator;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Get measurement data for a group entry
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<GroupMeasurementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMeasurement(int groupId)
    {
        var groupEntry = await GetGroupEntryWithAuth(groupId);
        if (groupEntry == null)
            return NotFound(ApiErrorResponse.Create("GROUP_NOT_FOUND", "Group entry not found"));

        var measurement = await _context.GroupMeasurements
            .Include(m => m.OriginalImage)
            .Include(m => m.AnnotatedImage)
            .FirstOrDefaultAsync(m => m.GroupEntryId == groupId);

        if (measurement == null)
            return NotFound(ApiErrorResponse.Create("MEASUREMENT_NOT_FOUND", "No measurement data for this group"));

        var dto = MapToDto(measurement, groupEntry.Distance);
        return Ok(ApiResponse<GroupMeasurementDto>.Ok(dto));
    }

    /// <summary>
    /// Create measurement data for a group entry
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<GroupMeasurementDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateMeasurement(int groupId, [FromBody] CreateGroupMeasurementDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiErrorResponse.ValidationError("Validation failed", GetValidationErrors()));

        var groupEntry = await GetGroupEntryWithAuth(groupId);
        if (groupEntry == null)
            return NotFound(ApiErrorResponse.Create("GROUP_NOT_FOUND", "Group entry not found"));

        // Check if measurement already exists
        var existing = await _context.GroupMeasurements.AnyAsync(m => m.GroupEntryId == groupId);
        if (existing)
            return Conflict(ApiErrorResponse.Create("MEASUREMENT_EXISTS", "Measurement already exists for this group. Use PUT to update."));

        // Parse calibration method
        if (!Enum.TryParse<CalibrationMethod>(dto.CalibrationMethod, true, out var calibrationMethod))
            return BadRequest(ApiErrorResponse.Create("INVALID_CALIBRATION_METHOD", $"Invalid calibration method. Valid values: {string.Join(", ", Enum.GetNames<CalibrationMethod>())}"));

        // Calculate metrics
        var holePositions = dto.HolePositions.Select(h => (h.X, h.Y)).ToList();
        var metrics = _calculator.Calculate(holePositions, dto.BulletDiameter);

        // Create entity
        var measurement = new GroupMeasurement
        {
            GroupEntryId = groupId,
            HolePositionsJson = JsonSerializer.Serialize(dto.HolePositions),
            BulletDiameter = dto.BulletDiameter,
            ExtremeSpreadCtc = metrics.ExtremeSpreadCtc,
            ExtremeSpreadEte = metrics.ExtremeSpreadEte,
            MeanRadius = metrics.MeanRadius,
            HorizontalSpreadCtc = metrics.HorizontalSpreadCtc,
            HorizontalSpreadEte = metrics.HorizontalSpreadEte,
            VerticalSpreadCtc = metrics.VerticalSpreadCtc,
            VerticalSpreadEte = metrics.VerticalSpreadEte,
            RadialStdDev = metrics.RadialStdDev,
            HorizontalStdDev = metrics.HorizontalStdDev,
            VerticalStdDev = metrics.VerticalStdDev,
            Cep50 = metrics.Cep50,
            PoiOffsetX = metrics.CentroidX,
            PoiOffsetY = metrics.CentroidY,
            CalibrationMethod = calibrationMethod,
            MeasurementConfidence = dto.MeasurementConfidence
        };

        _context.GroupMeasurements.Add(measurement);

        // Update GroupEntry with MOA values (use CTC as the default/display value)
        groupEntry.GroupSizeMoa = _calculator.InchesToMoa(metrics.ExtremeSpreadCtc, groupEntry.Distance);
        groupEntry.MeanRadiusMoa = _calculator.InchesToMoa(metrics.MeanRadius, groupEntry.Distance);
        groupEntry.NumberOfShots = dto.HolePositions.Count;
        groupEntry.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Created measurement for group {GroupId} with {HoleCount} holes", groupId, dto.HolePositions.Count);

        var resultDto = MapToDto(measurement, groupEntry.Distance);
        return CreatedAtAction(nameof(GetMeasurement), new { groupId },
            ApiResponse<GroupMeasurementDto>.Ok(resultDto, "Measurement created successfully"));
    }

    /// <summary>
    /// Update measurement data for a group entry
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<GroupMeasurementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMeasurement(int groupId, [FromBody] UpdateGroupMeasurementDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiErrorResponse.ValidationError("Validation failed", GetValidationErrors()));

        var groupEntry = await GetGroupEntryWithAuth(groupId);
        if (groupEntry == null)
            return NotFound(ApiErrorResponse.Create("GROUP_NOT_FOUND", "Group entry not found"));

        var measurement = await _context.GroupMeasurements
            .Include(m => m.OriginalImage)
            .Include(m => m.AnnotatedImage)
            .FirstOrDefaultAsync(m => m.GroupEntryId == groupId);

        if (measurement == null)
            return NotFound(ApiErrorResponse.Create("MEASUREMENT_NOT_FOUND", "No measurement data for this group. Use POST to create."));

        // Update calibration method if provided
        if (dto.CalibrationMethod != null)
        {
            if (!Enum.TryParse<CalibrationMethod>(dto.CalibrationMethod, true, out var calibrationMethod))
                return BadRequest(ApiErrorResponse.Create("INVALID_CALIBRATION_METHOD", $"Invalid calibration method. Valid values: {string.Join(", ", Enum.GetNames<CalibrationMethod>())}"));
            measurement.CalibrationMethod = calibrationMethod;
        }

        if (dto.MeasurementConfidence.HasValue)
            measurement.MeasurementConfidence = dto.MeasurementConfidence;

        // If hole positions or bullet diameter changed, recalculate everything
        if (dto.HolePositions != null || dto.BulletDiameter.HasValue)
        {
            var holePositions = dto.HolePositions ??
                JsonSerializer.Deserialize<List<HolePosition>>(measurement.HolePositionsJson) ??
                new List<HolePosition>();

            var bulletDiameter = dto.BulletDiameter ?? measurement.BulletDiameter;

            if (holePositions.Count < 2)
                return BadRequest(ApiErrorResponse.Create("INVALID_HOLES", "At least 2 hole positions are required"));

            var metrics = _calculator.Calculate(holePositions.Select(h => (h.X, h.Y)).ToList(), bulletDiameter);

            measurement.HolePositionsJson = JsonSerializer.Serialize(holePositions);
            measurement.BulletDiameter = bulletDiameter;
            measurement.ExtremeSpreadCtc = metrics.ExtremeSpreadCtc;
            measurement.ExtremeSpreadEte = metrics.ExtremeSpreadEte;
            measurement.MeanRadius = metrics.MeanRadius;
            measurement.HorizontalSpreadCtc = metrics.HorizontalSpreadCtc;
            measurement.HorizontalSpreadEte = metrics.HorizontalSpreadEte;
            measurement.VerticalSpreadCtc = metrics.VerticalSpreadCtc;
            measurement.VerticalSpreadEte = metrics.VerticalSpreadEte;
            measurement.RadialStdDev = metrics.RadialStdDev;
            measurement.HorizontalStdDev = metrics.HorizontalStdDev;
            measurement.VerticalStdDev = metrics.VerticalStdDev;
            measurement.Cep50 = metrics.Cep50;
            measurement.PoiOffsetX = metrics.CentroidX;
            measurement.PoiOffsetY = metrics.CentroidY;

            // Update GroupEntry MOA values (use CTC as the default/display value)
            groupEntry.GroupSizeMoa = _calculator.InchesToMoa(metrics.ExtremeSpreadCtc, groupEntry.Distance);
            groupEntry.MeanRadiusMoa = _calculator.InchesToMoa(metrics.MeanRadius, groupEntry.Distance);
            groupEntry.NumberOfShots = holePositions.Count;
        }

        measurement.UpdatedAt = DateTime.UtcNow;
        groupEntry.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated measurement for group {GroupId}", groupId);

        var resultDto = MapToDto(measurement, groupEntry.Distance);
        return Ok(ApiResponse<GroupMeasurementDto>.Ok(resultDto, "Measurement updated successfully"));
    }

    /// <summary>
    /// Delete measurement data for a group entry
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMeasurement(int groupId)
    {
        var groupEntry = await GetGroupEntryWithAuth(groupId);
        if (groupEntry == null)
            return NotFound(ApiErrorResponse.Create("GROUP_NOT_FOUND", "Group entry not found"));

        var measurement = await _context.GroupMeasurements.FirstOrDefaultAsync(m => m.GroupEntryId == groupId);
        if (measurement == null)
            return NotFound(ApiErrorResponse.Create("MEASUREMENT_NOT_FOUND", "No measurement data for this group"));

        _context.GroupMeasurements.Remove(measurement);

        // Note: We do NOT clear GroupEntry.GroupSizeMoa/MeanRadiusMoa
        // The user may want to keep those values even after removing detailed measurement

        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted measurement for group {GroupId}", groupId);

        return Ok(ApiResponse.Ok("Measurement deleted successfully"));
    }

    // ==================== Private Helpers ====================

    private async Task<GroupEntry?> GetGroupEntryWithAuth(int groupId)
    {
        var userId = GetUserId();
        return await _context.GroupEntries
            .Include(g => g.RangeSession)
            .FirstOrDefaultAsync(g => g.Id == groupId && g.RangeSession.UserId == userId);
    }

    private GroupMeasurementDto MapToDto(GroupMeasurement measurement, int distanceYards)
    {
        var holePositions = JsonSerializer.Deserialize<List<HolePosition>>(measurement.HolePositionsJson)
            ?? new List<HolePosition>();

        return new GroupMeasurementDto
        {
            Id = measurement.Id,
            GroupEntryId = measurement.GroupEntryId,
            HolePositions = holePositions,
            BulletDiameter = measurement.BulletDiameter,
            ExtremeSpreadCtc = measurement.ExtremeSpreadCtc,
            ExtremeSpreadEte = measurement.ExtremeSpreadEte,
            MeanRadius = measurement.MeanRadius,
            HorizontalSpreadCtc = measurement.HorizontalSpreadCtc,
            HorizontalSpreadEte = measurement.HorizontalSpreadEte,
            VerticalSpreadCtc = measurement.VerticalSpreadCtc,
            VerticalSpreadEte = measurement.VerticalSpreadEte,
            RadialStdDev = measurement.RadialStdDev,
            HorizontalStdDev = measurement.HorizontalStdDev,
            VerticalStdDev = measurement.VerticalStdDev,
            Cep50 = measurement.Cep50,
            PoiOffsetX = measurement.PoiOffsetX,
            PoiOffsetY = measurement.PoiOffsetY,
            ExtremeSpreadCtcMoa = measurement.ExtremeSpreadCtc.HasValue
                ? _calculator.InchesToMoa(measurement.ExtremeSpreadCtc.Value, distanceYards)
                : null,
            ExtremeSpreadEteMoa = measurement.ExtremeSpreadEte.HasValue
                ? _calculator.InchesToMoa(measurement.ExtremeSpreadEte.Value, distanceYards)
                : null,
            MeanRadiusMoa = measurement.MeanRadius.HasValue
                ? _calculator.InchesToMoa(measurement.MeanRadius.Value, distanceYards)
                : null,
            CalibrationMethod = measurement.CalibrationMethod.ToString().ToLowerInvariant(),
            MeasurementConfidence = measurement.MeasurementConfidence,
            OriginalImage = measurement.OriginalImage != null ? MapImageDto(measurement.OriginalImage) : null,
            AnnotatedImage = measurement.AnnotatedImage != null ? MapImageDto(measurement.AnnotatedImage) : null,
            CreatedAt = measurement.CreatedAt,
            UpdatedAt = measurement.UpdatedAt
        };
    }

    private static ImageDto MapImageDto(Image image)
    {
        return new ImageDto
        {
            Id = image.Id,
            Url = $"/api/images/{image.Id}",
            ThumbnailUrl = $"/api/images/{image.Id}/thumbnail",
            Caption = image.Caption,
            OriginalFileName = image.OriginalFileName,
            FileSize = image.FileSize
        };
    }

    private Dictionary<string, string[]> GetValidationErrors()
    {
        return ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                x => x.Key,
                x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            );
    }
}
