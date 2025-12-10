namespace TrueDope.Api.DTOs.Analytics;

// =====================
// Summary DTOs
// =====================

public class AnalyticsSummaryDto
{
    public int TotalSessions { get; set; }
    public int TotalRoundsFired { get; set; }
    public LongestShotDto? LongestShot { get; set; }
    public BestGroupDto? BestGroup { get; set; }
    public LowestSdAmmoDto? LowestSdAmmo { get; set; }
    public TotalCostDto TotalCost { get; set; } = new();
    public RecentActivityDto RecentActivity { get; set; } = new();
}

public class LongestShotDto
{
    public int Distance { get; set; }
    public int RifleId { get; set; }
    public string RifleName { get; set; } = string.Empty;
    public int SessionId { get; set; }
    public DateTime SessionDate { get; set; }
}

public class BestGroupDto
{
    public decimal SizeMoa { get; set; }
    public int Distance { get; set; }
    public int NumberOfShots { get; set; }
    public string AmmoName { get; set; } = string.Empty;
    public int SessionId { get; set; }
    public DateTime SessionDate { get; set; }
}

public class LowestSdAmmoDto
{
    public int AmmoId { get; set; }
    public string AmmoName { get; set; } = string.Empty;
    public decimal AverageSd { get; set; }
    public int SessionCount { get; set; }
}

public class TotalCostDto
{
    public decimal? Amount { get; set; }
    public string Period { get; set; } = "all-time";
}

public class RecentActivityDto
{
    public DateTime? LastSessionDate { get; set; }
    public int SessionsLast30Days { get; set; }
    public int RoundsLast30Days { get; set; }
}

// =====================
// DOPE Chart DTOs
// =====================

public class DopeChartFilterDto
{
    public int RifleId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int[]? Months { get; set; }
    public decimal? MinTemp { get; set; }
    public decimal? MaxTemp { get; set; }
    public int? MinHumidity { get; set; }
    public int? MaxHumidity { get; set; }
    public decimal? MinPressure { get; set; }
    public decimal? MaxPressure { get; set; }
    public int IntervalYards { get; set; } = 50;
}

public class DopeChartDataDto
{
    public int RifleId { get; set; }
    public string RifleName { get; set; } = string.Empty;
    public string Caliber { get; set; } = string.Empty;
    public int? ZeroDistance { get; set; }
    public AmmoInfoDto? Ammunition { get; set; }
    public decimal? MuzzleVelocity { get; set; }
    public AppliedFiltersDto AppliedFilters { get; set; } = new();
    public List<DopeDataPointDto> DataPoints { get; set; } = new();
    public DopeMetadataDto Metadata { get; set; } = new();
}

public class AmmoInfoDto
{
    public string Name { get; set; } = string.Empty;
    public decimal? BulletWeight { get; set; }
}

public class AppliedFiltersDto
{
    public DateRangeDto? DateRange { get; set; }
    public int[]? Months { get; set; }
    public RangeDto<decimal>? Temperature { get; set; }
    public RangeDto<int>? Humidity { get; set; }
    public RangeDto<decimal>? Pressure { get; set; }
}

public class DateRangeDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public class RangeDto<T> where T : struct
{
    public T? Min { get; set; }
    public T? Max { get; set; }
}

public class DopeDataPointDto
{
    public int Distance { get; set; }
    public decimal ElevationMils { get; set; }
    public decimal ElevationMilsStdDev { get; set; }
    public decimal WindageMils { get; set; }
    public decimal WindageMilsStdDev { get; set; }
    public int SessionCount { get; set; }
    public string DataSource { get; set; } = "no_data"; // "direct", "interpolated", "no_data"
}

public class DopeMetadataDto
{
    public int TotalSessionsMatched { get; set; }
    public int TotalSessionsAll { get; set; }
    public RangeDto<int>? DistanceRange { get; set; }
    public ConditionsRangeDto? ConditionsRange { get; set; }
}

public class ConditionsRangeDto
{
    public RangeDto<decimal>? Temperature { get; set; }
    public RangeDto<int>? Humidity { get; set; }
    public RangeDto<decimal>? Pressure { get; set; }
}

// =====================
// Velocity Trends DTOs
// =====================

public class VelocityTrendsFilterDto
{
    public int AmmoId { get; set; }
    public int? LotId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class VelocityTrendsDto
{
    public int AmmoId { get; set; }
    public string AmmoName { get; set; } = string.Empty;
    public string Caliber { get; set; } = string.Empty;
    public int? LotId { get; set; }
    public string? LotNumber { get; set; }
    public List<VelocitySessionDto> Sessions { get; set; } = new();
    public VelocityAggregatesDto Aggregates { get; set; } = new();
    public VelocityCorrelationDto? Correlation { get; set; }
}

public class VelocitySessionDto
{
    public int SessionId { get; set; }
    public DateTime SessionDate { get; set; }
    public decimal AverageVelocity { get; set; }
    public decimal StandardDeviation { get; set; }
    public decimal ExtremeSpread { get; set; }
    public int RoundsFired { get; set; }
    public SessionConditionsDto? Conditions { get; set; }
}

public class SessionConditionsDto
{
    public decimal? Temperature { get; set; }
    public int? Humidity { get; set; }
    public decimal? Pressure { get; set; }
    public decimal? DensityAltitude { get; set; }
}

public class VelocityAggregatesDto
{
    public decimal OverallAverageVelocity { get; set; }
    public decimal OverallAverageSd { get; set; }
    public decimal OverallAverageEs { get; set; }
    public int TotalRoundsFired { get; set; }
    public int SessionCount { get; set; }
    public VelocityRangeDto VelocityRange { get; set; } = new();
}

public class VelocityRangeDto
{
    public decimal High { get; set; }
    public decimal Low { get; set; }
}

public class VelocityCorrelationDto
{
    public decimal? TemperatureCorrelation { get; set; }
    public decimal? DensityAltitudeCorrelation { get; set; }
    public decimal? VelocityPerDegreeF { get; set; }
    public decimal? VelocityPer1000ftDA { get; set; }
}

// =====================
// Ammo Comparison DTOs
// =====================

public class AmmoComparisonDto
{
    public List<AmmoComparisonItemDto> Ammunitions { get; set; } = new();
    public ComparisonWinnersDto Comparison { get; set; } = new();
}

public class AmmoComparisonItemDto
{
    public int AmmoId { get; set; }
    public string AmmoName { get; set; } = string.Empty;
    public string Caliber { get; set; } = string.Empty;
    public AmmoVelocityStatsDto? Velocity { get; set; }
    public AmmoGroupStatsDto? Groups { get; set; }
}

public class AmmoVelocityStatsDto
{
    public decimal? AverageVelocity { get; set; }
    public decimal? AverageSd { get; set; }
    public decimal? AverageEs { get; set; }
    public int SessionCount { get; set; }
    public int TotalRounds { get; set; }
}

public class AmmoGroupStatsDto
{
    public decimal? AverageGroupSizeMoa { get; set; }
    public decimal? BestGroupSizeMoa { get; set; }
    public int GroupCount { get; set; }
    public int? AverageDistance { get; set; }
}

public class ComparisonWinnersDto
{
    public int? BestVelocityConsistency { get; set; }
    public int? BestGroupSize { get; set; }
    public int? MostDataPoints { get; set; }
}

// =====================
// Lot Comparison DTOs
// =====================

public class LotComparisonDto
{
    public int AmmoId { get; set; }
    public string AmmoName { get; set; } = string.Empty;
    public List<LotComparisonItemDto> Lots { get; set; } = new();
    public LotComparisonSummaryDto Comparison { get; set; } = new();
}

public class LotComparisonItemDto
{
    public int LotId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateTime? PurchaseDate { get; set; }
    public AmmoVelocityStatsDto? Velocity { get; set; }
    public AmmoGroupStatsDto? Groups { get; set; }
}

public class LotComparisonSummaryDto
{
    public decimal? VelocitySpread { get; set; }
    public int? BestLotForConsistency { get; set; }
    public int? BestLotForGroups { get; set; }
}

// =====================
// Cost Analysis DTOs
// =====================

public class CostSummaryFilterDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? RifleId { get; set; }
}

public class CostSummaryDto
{
    public CostPeriodDto Period { get; set; } = new();
    public CostTotalsDto Totals { get; set; } = new();
    public List<CostByAmmoDto> ByAmmunition { get; set; } = new();
    public List<CostByRifleDto> ByRifle { get; set; } = new();
    public List<CostByMonthDto> ByMonth { get; set; } = new();
}

public class CostPeriodDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public class CostTotalsDto
{
    public int TotalRoundsFired { get; set; }
    public decimal? TotalCost { get; set; }
    public decimal? AverageCostPerRound { get; set; }
}

public class CostByAmmoDto
{
    public int AmmoId { get; set; }
    public string AmmoName { get; set; } = string.Empty;
    public int RoundsFired { get; set; }
    public decimal? Cost { get; set; }
    public decimal? CostPerRound { get; set; }
}

public class CostByRifleDto
{
    public int RifleId { get; set; }
    public string RifleName { get; set; } = string.Empty;
    public int RoundsFired { get; set; }
    public decimal? Cost { get; set; }
    public int Sessions { get; set; }
}

public class CostByMonthDto
{
    public string Month { get; set; } = string.Empty; // Format: "2024-01"
    public int RoundsFired { get; set; }
    public decimal? Cost { get; set; }
    public int Sessions { get; set; }
}
