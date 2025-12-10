using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrueDope.Api.DTOs;
using TrueDope.Api.DTOs.Analytics;
using TrueDope.Api.Services;

namespace TrueDope.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IAnalyticsService analyticsService, ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Get analytics summary for dashboard
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<AnalyticsSummaryDto>>> GetSummary()
    {
        var userId = GetUserId();
        var summary = await _analyticsService.GetSummaryAsync(userId);

        return Ok(new ApiResponse<AnalyticsSummaryDto>
        {
            Success = true,
            Data = summary
        });
    }

    /// <summary>
    /// Get DOPE chart data for a rifle
    /// </summary>
    [HttpGet("dope-chart")]
    public async Task<ActionResult<ApiResponse<DopeChartDataDto>>> GetDopeChart(
        [FromQuery] int rifleId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int[]? months,
        [FromQuery] decimal? minTemp,
        [FromQuery] decimal? maxTemp,
        [FromQuery] int? minHumidity,
        [FromQuery] int? maxHumidity,
        [FromQuery] decimal? minPressure,
        [FromQuery] decimal? maxPressure,
        [FromQuery] int intervalYards = 50)
    {
        var userId = GetUserId();

        var filter = new DopeChartFilterDto
        {
            RifleId = rifleId,
            FromDate = fromDate,
            ToDate = toDate,
            Months = months,
            MinTemp = minTemp,
            MaxTemp = maxTemp,
            MinHumidity = minHumidity,
            MaxHumidity = maxHumidity,
            MinPressure = minPressure,
            MaxPressure = maxPressure,
            IntervalYards = intervalYards
        };

        try
        {
            var data = await _analyticsService.GetDopeChartDataAsync(userId, filter);

            return Ok(new ApiResponse<DopeChartDataDto>
            {
                Success = true,
                Data = data
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ApiResponse<DopeChartDataDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Get velocity trends for ammunition
    /// </summary>
    [HttpGet("velocity-trends")]
    public async Task<ActionResult<ApiResponse<VelocityTrendsDto>>> GetVelocityTrends(
        [FromQuery] int ammoId,
        [FromQuery] int? lotId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var userId = GetUserId();

        var filter = new VelocityTrendsFilterDto
        {
            AmmoId = ammoId,
            LotId = lotId,
            FromDate = fromDate,
            ToDate = toDate
        };

        try
        {
            var data = await _analyticsService.GetVelocityTrendsAsync(userId, filter);

            return Ok(new ApiResponse<VelocityTrendsDto>
            {
                Success = true,
                Data = data
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ApiResponse<VelocityTrendsDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Compare multiple ammunition types
    /// </summary>
    [HttpGet("ammo-comparison")]
    public async Task<ActionResult<ApiResponse<AmmoComparisonDto>>> GetAmmoComparison(
        [FromQuery] int[] ammoIds)
    {
        var userId = GetUserId();

        if (ammoIds == null || ammoIds.Length < 2)
        {
            return BadRequest(new ApiResponse<AmmoComparisonDto>
            {
                Success = false,
                Message = "At least 2 ammunition types are required for comparison"
            });
        }

        if (ammoIds.Length > 5)
        {
            return BadRequest(new ApiResponse<AmmoComparisonDto>
            {
                Success = false,
                Message = "Maximum 5 ammunition types can be compared at once"
            });
        }

        try
        {
            var data = await _analyticsService.GetAmmoComparisonAsync(userId, ammoIds);

            return Ok(new ApiResponse<AmmoComparisonDto>
            {
                Success = true,
                Data = data
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<AmmoComparisonDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Compare lots for a specific ammunition
    /// </summary>
    [HttpGet("lot-comparison")]
    public async Task<ActionResult<ApiResponse<LotComparisonDto>>> GetLotComparison(
        [FromQuery] int ammoId)
    {
        var userId = GetUserId();

        try
        {
            var data = await _analyticsService.GetLotComparisonAsync(userId, ammoId);

            return Ok(new ApiResponse<LotComparisonDto>
            {
                Success = true,
                Data = data
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ApiResponse<LotComparisonDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Get cost analysis summary
    /// </summary>
    [HttpGet("cost-summary")]
    public async Task<ActionResult<ApiResponse<CostSummaryDto>>> GetCostSummary(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int? rifleId)
    {
        var userId = GetUserId();

        var filter = new CostSummaryFilterDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            RifleId = rifleId
        };

        var data = await _analyticsService.GetCostSummaryAsync(userId, filter);

        return Ok(new ApiResponse<CostSummaryDto>
        {
            Success = true,
            Data = data
        });
    }
}
