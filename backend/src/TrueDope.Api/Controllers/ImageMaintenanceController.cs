using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrueDope.Api.DTOs;
using TrueDope.Api.DTOs.Admin;
using TrueDope.Api.Services;

namespace TrueDope.Api.Controllers;

[ApiController]
[Route("api/admin/images")]
[Authorize(Policy = "AdminOnly")]
public class ImageMaintenanceController : ControllerBase
{
    private readonly IImageMaintenanceService _maintenanceService;
    private readonly ILogger<ImageMaintenanceController> _logger;

    public ImageMaintenanceController(
        IImageMaintenanceService maintenanceService,
        ILogger<ImageMaintenanceController> logger)
    {
        _maintenanceService = maintenanceService;
        _logger = logger;
    }

    /// <summary>
    /// Get image maintenance statistics
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<ImageMaintenanceStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetImageStats()
    {
        var stats = await _maintenanceService.GetImageStatsAsync();
        return Ok(ApiResponse<ImageMaintenanceStatsDto>.Ok(stats));
    }

    /// <summary>
    /// Start thumbnail regeneration job
    /// </summary>
    [HttpPost("regenerate-thumbnails")]
    [ProducesResponseType(typeof(ApiResponse<ThumbnailJobStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ThumbnailJobStatusDto>), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> StartThumbnailRegeneration()
    {
        _logger.LogInformation("Admin requested thumbnail regeneration");

        var jobStatus = await _maintenanceService.StartThumbnailRegenerationAsync();

        // If job was already running, return the existing job
        if (jobStatus.ProcessedImages > 0 || jobStatus.Status == ThumbnailJobState.Running)
        {
            return Accepted(ApiResponse<ThumbnailJobStatusDto>.Ok(jobStatus, "Thumbnail regeneration in progress"));
        }

        return Ok(ApiResponse<ThumbnailJobStatusDto>.Ok(jobStatus, "Thumbnail regeneration started"));
    }

    /// <summary>
    /// Get thumbnail regeneration job status
    /// </summary>
    [HttpGet("regenerate-thumbnails/{jobId}")]
    [ProducesResponseType(typeof(ApiResponse<ThumbnailJobStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetThumbnailJobStatus(string jobId)
    {
        var status = await _maintenanceService.GetThumbnailJobStatusAsync(jobId);

        if (status == null)
        {
            return NotFound(ApiErrorResponse.Create("JOB_NOT_FOUND", $"Job '{jobId}' not found"));
        }

        return Ok(ApiResponse<ThumbnailJobStatusDto>.Ok(status));
    }

    /// <summary>
    /// List orphaned images (in storage but not in database)
    /// </summary>
    [HttpGet("orphaned")]
    [ProducesResponseType(typeof(ApiResponse<List<OrphanedImageDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrphanedImages()
    {
        var orphanedFiles = await _maintenanceService.GetOrphanedImagesAsync();
        return Ok(ApiResponse<List<OrphanedImageDto>>.Ok(orphanedFiles));
    }

    /// <summary>
    /// Delete all orphaned images
    /// </summary>
    [HttpDelete("orphaned")]
    [ProducesResponseType(typeof(ApiResponse<OrphanCleanupResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteOrphanedImages()
    {
        _logger.LogInformation("Admin requested orphaned image cleanup");

        var result = await _maintenanceService.DeleteOrphanedImagesAsync();

        var message = result.Errors.Count > 0
            ? $"Deleted {result.DeletedCount} files, freed {result.FreedSizeFormatted}. {result.Errors.Count} errors occurred."
            : $"Deleted {result.DeletedCount} files, freed {result.FreedSizeFormatted}";

        return Ok(ApiResponse<OrphanCleanupResultDto>.Ok(result, message));
    }
}
