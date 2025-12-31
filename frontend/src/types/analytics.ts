// Analytics DTOs

export interface AnalyticsSummaryDto {
  totalSessions: number;
  totalRoundsFired: number;
  longestShot: LongestShotDto | null;
  bestGroup: BestGroupDto | null;
  lowestSdAmmo: LowestSdAmmoDto | null;
  totalCost: TotalCostDto | null;
  recentActivity: RecentActivityDto | null;
}

export interface LongestShotDto {
  distance: number;
  rifleId: number;
  rifleName: string;
  sessionId: number;
  sessionDate: string;
}

export interface BestGroupDto {
  sizeMoa: number;
  distance: number;
  numberOfShots: number;
  ammoName: string;
  sessionId: number;
  sessionDate: string;
}

export interface LowestSdAmmoDto {
  ammoId: number;
  ammoName: string;
  averageSd: number;
  sessionCount: number;
}

export interface TotalCostDto {
  amount: number | null;
  period: string;
}

export interface RecentActivityDto {
  lastSessionDate: string | null;
  sessionsLast30Days: number;
  roundsLast30Days: number;
}

// DOPE Chart Types
export interface DopeChartFilterDto {
  rifleId: number;
  fromDate?: string;
  toDate?: string;
  months?: number[];
  minTemp?: number;
  maxTemp?: number;
  minHumidity?: number;
  maxHumidity?: number;
  minPressure?: number;
  maxPressure?: number;
  intervalYards?: number;
}

export interface DopeChartDataDto {
  rifleId: number;
  rifleName: string;
  caliber: string;
  zeroDistance: number;
  muzzleVelocity: number | null;
  ammunition: AmmoInfoDto | null;
  appliedFilters: AppliedFiltersDto;
  metadata: DopeMetadataDto;
  dataPoints: DopeDataPointDto[];
}

export interface AmmoInfoDto {
  name: string;
  bulletWeight: number;
}

export interface AppliedFiltersDto {
  dateRange: DateRangeDto;
  months: number[] | null;
  temperature: RangeDto<number> | null;
  humidity: RangeDto<number> | null;
  pressure: RangeDto<number> | null;
}

export interface DateRangeDto {
  from: string | null;
  to: string | null;
}

export interface RangeDto<T> {
  min: T | null;
  max: T | null;
}

export interface DopeMetadataDto {
  totalSessionsMatched: number;
  totalSessionsAll: number;
  distanceRange: RangeDto<number> | null;
  conditionsRange: ConditionsRangeDto | null;
}

export interface ConditionsRangeDto {
  temperature: RangeDto<number>;
  humidity: RangeDto<number>;
  pressure: RangeDto<number>;
}

export interface DopeDataPointDto {
  distance: number;
  elevationMils: number;
  elevationMilsStdDev: number;
  windageMils: number;
  windageMilsStdDev: number;
  sessionCount: number;
  dataSource: 'direct' | 'interpolated' | 'no_data';
}

// Velocity Trends Types
export interface VelocityTrendsFilterDto {
  ammoId: number;
  lotId?: number;
  rifleId?: number;
  fromDate?: string;
  toDate?: string;
}

export interface VelocityTrendsDto {
  ammoId: number;
  ammoName: string;
  caliber: string;
  lotId: number | null;
  lotNumber: string | null;
  rifleId: number | null;
  rifleName: string | null;
  sessions: VelocitySessionDto[];
  aggregates: VelocityAggregatesDto | null;
  correlation: VelocityCorrelationDto | null;
}

export interface VelocitySessionDto {
  sessionId: number;
  sessionDate: string;
  averageVelocity: number;
  standardDeviation: number;
  extremeSpread: number;
  roundsFired: number;
  conditions: SessionConditionsDto | null;
}

export interface SessionConditionsDto {
  temperature: number | null;
  humidity: number | null;
  pressure: number | null;
  densityAltitude: number | null;
}

export interface VelocityAggregatesDto {
  overallAverageVelocity: number;
  overallAverageSd: number;
  overallAverageEs: number;
  totalRoundsFired: number;
  sessionCount: number;
  velocityRange: VelocityRangeDto;
}

export interface VelocityRangeDto {
  high: number;
  low: number;
}

export interface VelocityCorrelationDto {
  temperatureCorrelation: number | null;
  velocityPerDegreeF: number | null;
  densityAltitudeCorrelation: number | null;
  velocityPer1000ftDA: number | null;
}

// Ammo Comparison Types
export interface AmmoComparisonFilterDto {
  ammoIds: number[];
  rifleId?: number;
}

export interface AmmoComparisonDto {
  rifleId: number | null;
  rifleName: string | null;
  ammunitions: AmmoComparisonItemDto[];
  comparison: AmmoComparisonSummaryDto;
}

export interface AmmoComparisonItemDto {
  ammoId: number;
  ammoName: string;
  caliber: string;
  velocity: AmmoVelocityStatsDto | null;
  groups: AmmoGroupStatsDto | null;
}

export interface AmmoVelocityStatsDto {
  averageVelocity: number;
  averageSd: number;
  averageEs: number;
  sessionCount: number;
  totalRounds: number;
}

export interface AmmoGroupStatsDto {
  averageGroupSizeMoa: number;
  bestGroupSizeMoa: number;
  groupCount: number;
  averageDistance: number;
}

export interface AmmoComparisonSummaryDto {
  bestVelocityConsistency: number | null;
  bestGroupSize: number | null;
  mostDataPoints: number | null;
}

// Lot Comparison Types
export interface LotComparisonFilterDto {
  ammoId: number;
  rifleId?: number;
}

export interface LotComparisonDto {
  ammoId: number;
  ammoName: string;
  rifleId: number | null;
  rifleName: string | null;
  lots: LotComparisonItemDto[];
  comparison: LotComparisonSummaryDto;
}

export interface LotComparisonItemDto {
  lotId: number;
  lotNumber: string;
  purchaseDate: string | null;
  velocity: AmmoVelocityStatsDto | null;
  groups: AmmoGroupStatsDto | null;
}

export interface LotComparisonSummaryDto {
  velocitySpread: number | null;
  bestLotForConsistency: number | null;
  bestLotForGroups: number | null;
}

// Cost Summary Types
export interface CostSummaryFilterDto {
  fromDate?: string;
  toDate?: string;
  rifleId?: number;
}

export interface CostSummaryDto {
  period: CostPeriodDto;
  totals: CostTotalsDto;
  byAmmunition: CostByAmmoDto[];
  byRifle: CostByRifleDto[];
  byMonth: CostByMonthDto[];
}

export interface CostPeriodDto {
  from: string | null;
  to: string | null;
}

export interface CostTotalsDto {
  totalRoundsFired: number;
  totalCost: number | null;
  averageCostPerRound: number | null;
}

export interface CostByAmmoDto {
  ammoId: number;
  ammoName: string;
  roundsFired: number;
  cost: number | null;
  costPerRound: number | null;
}

export interface CostByRifleDto {
  rifleId: number;
  rifleName: string;
  roundsFired: number;
  cost: number | null;
  sessions: number;
}

export interface CostByMonthDto {
  month: string;
  roundsFired: number;
  cost: number | null;
  sessions: number;
}
