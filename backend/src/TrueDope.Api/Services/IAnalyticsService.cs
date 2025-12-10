using TrueDope.Api.DTOs.Analytics;

namespace TrueDope.Api.Services;

public interface IAnalyticsService
{
    Task<AnalyticsSummaryDto> GetSummaryAsync(string userId);
    Task<DopeChartDataDto> GetDopeChartDataAsync(string userId, DopeChartFilterDto filter);
    Task<VelocityTrendsDto> GetVelocityTrendsAsync(string userId, VelocityTrendsFilterDto filter);
    Task<AmmoComparisonDto> GetAmmoComparisonAsync(string userId, int[] ammoIds);
    Task<LotComparisonDto> GetLotComparisonAsync(string userId, int ammoId);
    Task<CostSummaryDto> GetCostSummaryAsync(string userId, CostSummaryFilterDto filter);
}
