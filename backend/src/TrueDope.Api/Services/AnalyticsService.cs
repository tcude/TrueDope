using Microsoft.EntityFrameworkCore;
using TrueDope.Api.Data;
using TrueDope.Api.DTOs.Analytics;

namespace TrueDope.Api.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _context;

    public AnalyticsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AnalyticsSummaryDto> GetSummaryAsync(string userId)
    {
        var summary = new AnalyticsSummaryDto();

        // Total sessions
        summary.TotalSessions = await _context.RangeSessions
            .Where(s => s.UserId == userId)
            .CountAsync();

        // Total rounds fired (from chrono sessions)
        summary.TotalRoundsFired = await _context.ChronoSessions
            .Where(cs => cs.RangeSession.UserId == userId)
            .SumAsync(cs => cs.NumberOfRounds);

        // Longest shot (from DOPE entries)
        var longestShot = await _context.DopeEntries
            .Where(d => d.RangeSession.UserId == userId)
            .OrderByDescending(d => d.Distance)
            .Select(d => new
            {
                d.Distance,
                d.RangeSession.RifleSetupId,
                RifleName = d.RangeSession.RifleSetup.Name,
                SessionId = d.RangeSessionId,
                d.RangeSession.SessionDate
            })
            .FirstOrDefaultAsync();

        if (longestShot != null)
        {
            summary.LongestShot = new LongestShotDto
            {
                Distance = longestShot.Distance,
                RifleId = longestShot.RifleSetupId,
                RifleName = longestShot.RifleName,
                SessionId = longestShot.SessionId,
                SessionDate = longestShot.SessionDate
            };
        }

        // Best group (smallest MOA)
        var bestGroup = await _context.GroupEntries
            .Where(g => g.RangeSession.UserId == userId && g.GroupSizeMoa.HasValue)
            .OrderBy(g => g.GroupSizeMoa)
            .Select(g => new
            {
                g.GroupSizeMoa,
                g.Distance,
                g.NumberOfShots,
                AmmoManufacturer = g.Ammunition != null ? g.Ammunition.Manufacturer : null,
                AmmoProductName = g.Ammunition != null ? g.Ammunition.Name : null,
                AmmoCaliber = g.Ammunition != null ? g.Ammunition.Caliber : null,
                AmmoGrain = g.Ammunition != null ? g.Ammunition.Grain : (decimal?)null,
                SessionId = g.RangeSessionId,
                g.RangeSession.SessionDate
            })
            .FirstOrDefaultAsync();

        if (bestGroup != null)
        {
            var ammoName = bestGroup.AmmoManufacturer != null
                ? $"{bestGroup.AmmoManufacturer} {bestGroup.AmmoProductName} ({bestGroup.AmmoCaliber} - {bestGroup.AmmoGrain}gr)"
                : "Unknown";

            summary.BestGroup = new BestGroupDto
            {
                SizeMoa = bestGroup.GroupSizeMoa!.Value,
                Distance = bestGroup.Distance,
                NumberOfShots = bestGroup.NumberOfShots,
                AmmoName = ammoName,
                SessionId = bestGroup.SessionId,
                SessionDate = bestGroup.SessionDate
            };
        }

        // Lowest SD ammo (best velocity consistency)
        var ammoSdStats = await _context.ChronoSessions
            .Where(cs => cs.RangeSession.UserId == userId && cs.StandardDeviation.HasValue)
            .GroupBy(cs => new {
                cs.AmmunitionId,
                cs.Ammunition.Manufacturer,
                cs.Ammunition.Name,
                cs.Ammunition.Caliber,
                cs.Ammunition.Grain
            })
            .Select(g => new
            {
                AmmoId = g.Key.AmmunitionId,
                Manufacturer = g.Key.Manufacturer,
                Name = g.Key.Name,
                Caliber = g.Key.Caliber,
                Grain = g.Key.Grain,
                AverageSd = g.Average(cs => cs.StandardDeviation!.Value),
                SessionCount = g.Count()
            })
            .Where(x => x.SessionCount >= 2) // Only ammo with 2+ sessions
            .OrderBy(x => x.AverageSd)
            .FirstOrDefaultAsync();

        if (ammoSdStats != null)
        {
            summary.LowestSdAmmo = new LowestSdAmmoDto
            {
                AmmoId = ammoSdStats.AmmoId,
                AmmoName = $"{ammoSdStats.Manufacturer} {ammoSdStats.Name} ({ammoSdStats.Caliber} - {ammoSdStats.Grain}gr)",
                AverageSd = Math.Round(ammoSdStats.AverageSd, 2),
                SessionCount = ammoSdStats.SessionCount
            };
        }

        // Total cost (all time)
        // Cost = sum of (rounds * cost per round) for each chrono session
        var costData = await _context.ChronoSessions
            .Where(cs => cs.RangeSession.UserId == userId)
            .Select(cs => new
            {
                cs.NumberOfRounds,
                LotCostPerRound = cs.AmmoLot != null && cs.AmmoLot.InitialQuantity.HasValue && cs.AmmoLot.PurchasePrice.HasValue && cs.AmmoLot.InitialQuantity > 0
                    ? cs.AmmoLot.PurchasePrice.Value / cs.AmmoLot.InitialQuantity.Value
                    : (decimal?)null,
                AmmoCostPerRound = cs.Ammunition.CostPerRound
            })
            .ToListAsync();

        decimal totalCost = 0;
        foreach (var item in costData)
        {
            var costPerRound = item.LotCostPerRound ?? item.AmmoCostPerRound;
            if (costPerRound.HasValue)
            {
                totalCost += item.NumberOfRounds * costPerRound.Value;
            }
        }

        summary.TotalCost = new TotalCostDto
        {
            Amount = totalCost > 0 ? Math.Round(totalCost, 2) : null,
            Period = "all-time"
        };

        // Recent activity
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var lastSession = await _context.RangeSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SessionDate)
            .Select(s => s.SessionDate)
            .FirstOrDefaultAsync();

        var recentSessions = await _context.RangeSessions
            .Where(s => s.UserId == userId && s.SessionDate >= thirtyDaysAgo)
            .CountAsync();

        var recentRounds = await _context.ChronoSessions
            .Where(cs => cs.RangeSession.UserId == userId && cs.RangeSession.SessionDate >= thirtyDaysAgo)
            .SumAsync(cs => cs.NumberOfRounds);

        summary.RecentActivity = new RecentActivityDto
        {
            LastSessionDate = lastSession == default ? null : lastSession,
            SessionsLast30Days = recentSessions,
            RoundsLast30Days = recentRounds
        };

        return summary;
    }

    public async Task<DopeChartDataDto> GetDopeChartDataAsync(string userId, DopeChartFilterDto filter)
    {
        // Verify rifle belongs to user
        var rifle = await _context.RifleSetups
            .Where(r => r.Id == filter.RifleId && r.UserId == userId)
            .FirstOrDefaultAsync();

        if (rifle == null)
        {
            throw new InvalidOperationException("Rifle not found or access denied");
        }

        var result = new DopeChartDataDto
        {
            RifleId = rifle.Id,
            RifleName = rifle.Name,
            Caliber = rifle.Caliber ?? string.Empty,
            ZeroDistance = rifle.ZeroDistance,
            MuzzleVelocity = rifle.MuzzleVelocity,
            Ammunition = null // DOPE data is tracked per session, not per rifle
        };

        // Build base query for sessions
        var sessionsQuery = _context.RangeSessions
            .Where(s => s.UserId == userId && s.RifleSetupId == filter.RifleId)
            .AsQueryable();

        // Apply filters
        if (filter.FromDate.HasValue)
            sessionsQuery = sessionsQuery.Where(s => s.SessionDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            sessionsQuery = sessionsQuery.Where(s => s.SessionDate <= filter.ToDate.Value);

        if (filter.Months != null && filter.Months.Length > 0)
            sessionsQuery = sessionsQuery.Where(s => filter.Months.Contains(s.SessionDate.Month));

        if (filter.MinTemp.HasValue)
            sessionsQuery = sessionsQuery.Where(s => s.Temperature >= filter.MinTemp.Value);

        if (filter.MaxTemp.HasValue)
            sessionsQuery = sessionsQuery.Where(s => s.Temperature <= filter.MaxTemp.Value);

        if (filter.MinHumidity.HasValue)
            sessionsQuery = sessionsQuery.Where(s => s.Humidity >= filter.MinHumidity.Value);

        if (filter.MaxHumidity.HasValue)
            sessionsQuery = sessionsQuery.Where(s => s.Humidity <= filter.MaxHumidity.Value);

        if (filter.MinPressure.HasValue)
            sessionsQuery = sessionsQuery.Where(s => s.Pressure >= filter.MinPressure.Value);

        if (filter.MaxPressure.HasValue)
            sessionsQuery = sessionsQuery.Where(s => s.Pressure <= filter.MaxPressure.Value);

        // Get all DOPE entries for matching sessions
        var dopeEntries = await sessionsQuery
            .SelectMany(s => s.DopeEntries)
            .Select(d => new
            {
                d.Distance,
                d.ElevationMils,
                d.WindageMils
            })
            .ToListAsync();

        // Get metadata
        var totalSessionsAll = await _context.RangeSessions
            .Where(s => s.UserId == userId && s.RifleSetupId == filter.RifleId)
            .CountAsync();

        var totalSessionsMatched = await sessionsQuery.CountAsync();

        // Get conditions range from all sessions for this rifle
        var conditionsData = await _context.RangeSessions
            .Where(s => s.UserId == userId && s.RifleSetupId == filter.RifleId)
            .Where(s => s.Temperature.HasValue || s.Humidity.HasValue || s.Pressure.HasValue)
            .Select(s => new
            {
                s.Temperature,
                s.Humidity,
                s.Pressure
            })
            .ToListAsync();

        result.AppliedFilters = new AppliedFiltersDto
        {
            DateRange = new DateRangeDto { From = filter.FromDate, To = filter.ToDate },
            Months = filter.Months,
            Temperature = filter.MinTemp.HasValue || filter.MaxTemp.HasValue
                ? new RangeDto<decimal> { Min = filter.MinTemp, Max = filter.MaxTemp } : null,
            Humidity = filter.MinHumidity.HasValue || filter.MaxHumidity.HasValue
                ? new RangeDto<int> { Min = filter.MinHumidity, Max = filter.MaxHumidity } : null,
            Pressure = filter.MinPressure.HasValue || filter.MaxPressure.HasValue
                ? new RangeDto<decimal> { Min = filter.MinPressure, Max = filter.MaxPressure } : null
        };

        result.Metadata = new DopeMetadataDto
        {
            TotalSessionsMatched = totalSessionsMatched,
            TotalSessionsAll = totalSessionsAll,
            ConditionsRange = conditionsData.Any() ? new ConditionsRangeDto
            {
                Temperature = new RangeDto<decimal>
                {
                    Min = conditionsData.Where(c => c.Temperature.HasValue).Select(c => c.Temperature!.Value).DefaultIfEmpty().Min(),
                    Max = conditionsData.Where(c => c.Temperature.HasValue).Select(c => c.Temperature!.Value).DefaultIfEmpty().Max()
                },
                Humidity = new RangeDto<int>
                {
                    Min = conditionsData.Where(c => c.Humidity.HasValue).Select(c => c.Humidity!.Value).DefaultIfEmpty().Min(),
                    Max = conditionsData.Where(c => c.Humidity.HasValue).Select(c => c.Humidity!.Value).DefaultIfEmpty().Max()
                },
                Pressure = new RangeDto<decimal>
                {
                    Min = conditionsData.Where(c => c.Pressure.HasValue).Select(c => c.Pressure!.Value).DefaultIfEmpty().Min(),
                    Max = conditionsData.Where(c => c.Pressure.HasValue).Select(c => c.Pressure!.Value).DefaultIfEmpty().Max()
                }
            } : null
        };

        if (!dopeEntries.Any())
        {
            return result;
        }

        // Group by distance and calculate stats
        var groupedByDistance = dopeEntries
            .GroupBy(d => d.Distance)
            .ToDictionary(
                g => g.Key,
                g => new DopeDistanceData
                {
                    AvgElevation = g.Average(d => d.ElevationMils),
                    StdDevElevation = CalculateStdDev(g.Select(d => d.ElevationMils)),
                    AvgWindage = g.Average(d => d.WindageMils),
                    StdDevWindage = CalculateStdDev(g.Select(d => d.WindageMils)),
                    Count = g.Count()
                });

        // Determine distance range
        var minDistance = 100; // Always start at 100
        var maxDistance = groupedByDistance.Keys.Max();

        result.Metadata.DistanceRange = new RangeDto<int> { Min = minDistance, Max = maxDistance };

        // Generate data points at intervals
        var interval = filter.IntervalYards;
        var dataPoints = new List<DopeDataPointDto>();

        for (var distance = minDistance; distance <= maxDistance; distance += interval)
        {
            if (groupedByDistance.TryGetValue(distance, out var directData))
            {
                // Direct data exists
                dataPoints.Add(new DopeDataPointDto
                {
                    Distance = distance,
                    ElevationMils = Math.Round(directData.AvgElevation, 3),
                    ElevationMilsStdDev = Math.Round(directData.StdDevElevation, 3),
                    WindageMils = Math.Round(directData.AvgWindage, 3),
                    WindageMilsStdDev = Math.Round(directData.StdDevWindage, 3),
                    SessionCount = directData.Count,
                    DataSource = "direct"
                });
            }
            else
            {
                // Try to interpolate
                var interpolated = TryInterpolate(distance, groupedByDistance, interval);
                if (interpolated != null)
                {
                    dataPoints.Add(interpolated);
                }
                else
                {
                    // No data
                    dataPoints.Add(new DopeDataPointDto
                    {
                        Distance = distance,
                        ElevationMils = 0,
                        ElevationMilsStdDev = 0,
                        WindageMils = 0,
                        WindageMilsStdDev = 0,
                        SessionCount = 0,
                        DataSource = "no_data"
                    });
                }
            }
        }

        result.DataPoints = dataPoints;
        return result;
    }

    public async Task<VelocityTrendsDto> GetVelocityTrendsAsync(string userId, VelocityTrendsFilterDto filter)
    {
        // Verify ammo belongs to user
        var ammo = await _context.Ammunition
            .Where(a => a.Id == filter.AmmoId && a.UserId == userId)
            .FirstOrDefaultAsync();

        if (ammo == null)
        {
            throw new InvalidOperationException("Ammunition not found or access denied");
        }

        var result = new VelocityTrendsDto
        {
            AmmoId = ammo.Id,
            AmmoName = ammo.DisplayName,
            Caliber = ammo.Caliber,
            LotId = filter.LotId
        };

        // Get lot number if filtering by lot
        if (filter.LotId.HasValue)
        {
            var lot = await _context.AmmoLots
                .Where(l => l.Id == filter.LotId.Value && l.UserId == userId)
                .Select(l => l.LotNumber)
                .FirstOrDefaultAsync();
            result.LotNumber = lot;
        }

        // Build query
        var query = _context.ChronoSessions
            .Include(cs => cs.RangeSession)
            .Where(cs => cs.AmmunitionId == filter.AmmoId && cs.RangeSession.UserId == userId)
            .AsQueryable();

        if (filter.LotId.HasValue)
            query = query.Where(cs => cs.AmmoLotId == filter.LotId.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(cs => cs.RangeSession.SessionDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(cs => cs.RangeSession.SessionDate <= filter.ToDate.Value);

        var sessions = await query
            .OrderBy(cs => cs.RangeSession.SessionDate)
            .Select(cs => new VelocitySessionDto
            {
                SessionId = cs.RangeSessionId,
                SessionDate = cs.RangeSession.SessionDate,
                AverageVelocity = cs.AverageVelocity ?? 0,
                StandardDeviation = cs.StandardDeviation ?? 0,
                ExtremeSpread = cs.ExtremeSpread ?? 0,
                RoundsFired = cs.NumberOfRounds,
                Conditions = new SessionConditionsDto
                {
                    Temperature = cs.RangeSession.Temperature,
                    Humidity = cs.RangeSession.Humidity,
                    Pressure = cs.RangeSession.Pressure,
                    DensityAltitude = cs.RangeSession.DensityAltitude
                }
            })
            .ToListAsync();

        result.Sessions = sessions;

        // Calculate aggregates
        if (sessions.Any())
        {
            var totalRounds = sessions.Sum(s => s.RoundsFired);

            // Weighted averages based on rounds fired
            var weightedVelocity = sessions.Sum(s => s.AverageVelocity * s.RoundsFired) / totalRounds;
            var avgSd = sessions.Average(s => s.StandardDeviation);
            var avgEs = sessions.Average(s => s.ExtremeSpread);

            result.Aggregates = new VelocityAggregatesDto
            {
                OverallAverageVelocity = Math.Round(weightedVelocity, 1),
                OverallAverageSd = Math.Round(avgSd, 2),
                OverallAverageEs = Math.Round(avgEs, 1),
                TotalRoundsFired = totalRounds,
                SessionCount = sessions.Count,
                VelocityRange = new VelocityRangeDto
                {
                    High = sessions.Max(s => s.AverageVelocity),
                    Low = sessions.Min(s => s.AverageVelocity)
                }
            };

            // Calculate correlations if we have enough data
            if (sessions.Count >= 3)
            {
                result.Correlation = CalculateCorrelations(sessions);
            }
        }

        return result;
    }

    public async Task<AmmoComparisonDto> GetAmmoComparisonAsync(string userId, int[] ammoIds)
    {
        if (ammoIds.Length > 5)
        {
            throw new InvalidOperationException("Maximum 5 ammunition types can be compared at once");
        }

        var result = new AmmoComparisonDto();

        foreach (var ammoId in ammoIds)
        {
            var ammo = await _context.Ammunition
                .Where(a => a.Id == ammoId && a.UserId == userId)
                .FirstOrDefaultAsync();

            if (ammo == null) continue;

            var item = new AmmoComparisonItemDto
            {
                AmmoId = ammo.Id,
                AmmoName = ammo.DisplayName,
                Caliber = ammo.Caliber
            };

            // Get velocity stats
            var chronoData = await _context.ChronoSessions
                .Where(cs => cs.AmmunitionId == ammoId && cs.RangeSession.UserId == userId)
                .Where(cs => cs.AverageVelocity.HasValue)
                .Select(cs => new
                {
                    cs.AverageVelocity,
                    cs.StandardDeviation,
                    cs.ExtremeSpread,
                    cs.NumberOfRounds
                })
                .ToListAsync();

            if (chronoData.Any())
            {
                var totalRounds = chronoData.Sum(c => c.NumberOfRounds);
                item.Velocity = new AmmoVelocityStatsDto
                {
                    AverageVelocity = Math.Round(chronoData.Sum(c => c.AverageVelocity!.Value * c.NumberOfRounds) / totalRounds, 1),
                    AverageSd = Math.Round(chronoData.Average(c => c.StandardDeviation ?? 0), 2),
                    AverageEs = Math.Round(chronoData.Average(c => c.ExtremeSpread ?? 0), 1),
                    SessionCount = chronoData.Count,
                    TotalRounds = totalRounds
                };
            }

            // Get group stats
            var groupData = await _context.GroupEntries
                .Where(g => g.AmmunitionId == ammoId && g.RangeSession.UserId == userId)
                .Where(g => g.GroupSizeMoa.HasValue)
                .Select(g => new
                {
                    g.GroupSizeMoa,
                    g.Distance
                })
                .ToListAsync();

            if (groupData.Any())
            {
                item.Groups = new AmmoGroupStatsDto
                {
                    AverageGroupSizeMoa = Math.Round(groupData.Average(g => g.GroupSizeMoa!.Value), 2),
                    BestGroupSizeMoa = Math.Round(groupData.Min(g => g.GroupSizeMoa!.Value), 2),
                    GroupCount = groupData.Count,
                    AverageDistance = (int)Math.Round(groupData.Average(g => g.Distance))
                };
            }

            result.Ammunitions.Add(item);
        }

        // Determine winners
        var withVelocity = result.Ammunitions.Where(a => a.Velocity != null).ToList();
        var withGroups = result.Ammunitions.Where(a => a.Groups != null).ToList();

        if (withVelocity.Any())
        {
            result.Comparison.BestVelocityConsistency = withVelocity
                .OrderBy(a => a.Velocity!.AverageSd)
                .First().AmmoId;
        }

        if (withGroups.Any())
        {
            result.Comparison.BestGroupSize = withGroups
                .OrderBy(a => a.Groups!.AverageGroupSizeMoa)
                .First().AmmoId;
        }

        if (result.Ammunitions.Any())
        {
            result.Comparison.MostDataPoints = result.Ammunitions
                .OrderByDescending(a => (a.Velocity?.TotalRounds ?? 0) + (a.Groups?.GroupCount ?? 0))
                .First().AmmoId;
        }

        return result;
    }

    public async Task<LotComparisonDto> GetLotComparisonAsync(string userId, int ammoId)
    {
        var ammo = await _context.Ammunition
            .Where(a => a.Id == ammoId && a.UserId == userId)
            .FirstOrDefaultAsync();

        if (ammo == null)
        {
            throw new InvalidOperationException("Ammunition not found or access denied");
        }

        var result = new LotComparisonDto
        {
            AmmoId = ammo.Id,
            AmmoName = ammo.DisplayName
        };

        var lots = await _context.AmmoLots
            .Where(l => l.AmmunitionId == ammoId && l.UserId == userId)
            .ToListAsync();

        foreach (var lot in lots)
        {
            var item = new LotComparisonItemDto
            {
                LotId = lot.Id,
                LotNumber = lot.LotNumber,
                PurchaseDate = lot.PurchaseDate
            };

            // Get velocity stats for this lot
            var chronoData = await _context.ChronoSessions
                .Where(cs => cs.AmmoLotId == lot.Id && cs.RangeSession.UserId == userId)
                .Where(cs => cs.AverageVelocity.HasValue)
                .Select(cs => new
                {
                    cs.AverageVelocity,
                    cs.StandardDeviation,
                    cs.ExtremeSpread,
                    cs.NumberOfRounds
                })
                .ToListAsync();

            if (chronoData.Any())
            {
                var totalRounds = chronoData.Sum(c => c.NumberOfRounds);
                item.Velocity = new AmmoVelocityStatsDto
                {
                    AverageVelocity = Math.Round(chronoData.Sum(c => c.AverageVelocity!.Value * c.NumberOfRounds) / totalRounds, 1),
                    AverageSd = Math.Round(chronoData.Average(c => c.StandardDeviation ?? 0), 2),
                    AverageEs = Math.Round(chronoData.Average(c => c.ExtremeSpread ?? 0), 1),
                    SessionCount = chronoData.Count,
                    TotalRounds = totalRounds
                };
            }

            // Get group stats for this lot
            var groupData = await _context.GroupEntries
                .Where(g => g.AmmoLotId == lot.Id && g.RangeSession.UserId == userId)
                .Where(g => g.GroupSizeMoa.HasValue)
                .Select(g => new
                {
                    g.GroupSizeMoa,
                    g.Distance
                })
                .ToListAsync();

            if (groupData.Any())
            {
                item.Groups = new AmmoGroupStatsDto
                {
                    AverageGroupSizeMoa = Math.Round(groupData.Average(g => g.GroupSizeMoa!.Value), 2),
                    BestGroupSizeMoa = Math.Round(groupData.Min(g => g.GroupSizeMoa!.Value), 2),
                    GroupCount = groupData.Count,
                    AverageDistance = (int)Math.Round(groupData.Average(g => g.Distance))
                };
            }

            result.Lots.Add(item);
        }

        // Calculate comparison summary
        var lotsWithVelocity = result.Lots.Where(l => l.Velocity != null).ToList();
        var lotsWithGroups = result.Lots.Where(l => l.Groups != null).ToList();

        if (lotsWithVelocity.Count >= 2)
        {
            var velocities = lotsWithVelocity.Select(l => l.Velocity!.AverageVelocity!.Value).ToList();
            result.Comparison.VelocitySpread = Math.Round(velocities.Max() - velocities.Min(), 1);
            result.Comparison.BestLotForConsistency = lotsWithVelocity
                .OrderBy(l => l.Velocity!.AverageSd)
                .First().LotId;
        }

        if (lotsWithGroups.Any())
        {
            result.Comparison.BestLotForGroups = lotsWithGroups
                .OrderBy(l => l.Groups!.AverageGroupSizeMoa)
                .First().LotId;
        }

        return result;
    }

    public async Task<CostSummaryDto> GetCostSummaryAsync(string userId, CostSummaryFilterDto filter)
    {
        var result = new CostSummaryDto
        {
            Period = new CostPeriodDto
            {
                From = filter.FromDate,
                To = filter.ToDate
            }
        };

        // Build base query
        var query = _context.ChronoSessions
            .Include(cs => cs.RangeSession)
            .Include(cs => cs.Ammunition)
            .Include(cs => cs.AmmoLot)
            .Where(cs => cs.RangeSession.UserId == userId)
            .AsQueryable();

        if (filter.FromDate.HasValue)
            query = query.Where(cs => cs.RangeSession.SessionDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(cs => cs.RangeSession.SessionDate <= filter.ToDate.Value);

        if (filter.RifleId.HasValue)
            query = query.Where(cs => cs.RangeSession.RifleSetupId == filter.RifleId.Value);

        var data = await query
            .Select(cs => new
            {
                cs.NumberOfRounds,
                cs.AmmunitionId,
                AmmoManufacturer = cs.Ammunition.Manufacturer,
                AmmoProductName = cs.Ammunition.Name,
                AmmoCaliber = cs.Ammunition.Caliber,
                AmmoGrain = cs.Ammunition.Grain,
                LotCostPerRound = cs.AmmoLot != null && cs.AmmoLot.InitialQuantity.HasValue && cs.AmmoLot.PurchasePrice.HasValue && cs.AmmoLot.InitialQuantity > 0
                    ? cs.AmmoLot.PurchasePrice.Value / cs.AmmoLot.InitialQuantity.Value
                    : (decimal?)null,
                AmmoCostPerRound = cs.Ammunition.CostPerRound,
                RifleId = cs.RangeSession.RifleSetupId,
                RifleName = cs.RangeSession.RifleSetup.Name,
                SessionDate = cs.RangeSession.SessionDate,
                SessionId = cs.RangeSessionId
            })
            .ToListAsync();

        // Calculate totals
        int totalRounds = 0;
        decimal totalCost = 0;

        var byAmmo = new Dictionary<int, (string Name, int Rounds, decimal Cost)>();
        var byRifle = new Dictionary<int, (string Name, int Rounds, decimal Cost, HashSet<int> Sessions)>();
        var byMonth = new Dictionary<string, (int Rounds, decimal Cost, HashSet<int> Sessions)>();

        foreach (var item in data)
        {
            var costPerRound = item.LotCostPerRound ?? item.AmmoCostPerRound;
            var cost = costPerRound.HasValue ? item.NumberOfRounds * costPerRound.Value : 0;

            totalRounds += item.NumberOfRounds;
            totalCost += cost;

            // By ammo
            var ammoDisplayName = $"{item.AmmoManufacturer} {item.AmmoProductName} ({item.AmmoCaliber} - {item.AmmoGrain}gr)";
            if (!byAmmo.ContainsKey(item.AmmunitionId))
                byAmmo[item.AmmunitionId] = (ammoDisplayName, 0, 0);
            var ammoData = byAmmo[item.AmmunitionId];
            byAmmo[item.AmmunitionId] = (ammoData.Name, ammoData.Rounds + item.NumberOfRounds, ammoData.Cost + cost);

            // By rifle
            if (!byRifle.ContainsKey(item.RifleId))
                byRifle[item.RifleId] = (item.RifleName, 0, 0, new HashSet<int>());
            var rifleData = byRifle[item.RifleId];
            rifleData.Sessions.Add(item.SessionId);
            byRifle[item.RifleId] = (rifleData.Name, rifleData.Rounds + item.NumberOfRounds, rifleData.Cost + cost, rifleData.Sessions);

            // By month
            var monthKey = item.SessionDate.ToString("yyyy-MM");
            if (!byMonth.ContainsKey(monthKey))
                byMonth[monthKey] = (0, 0, new HashSet<int>());
            var monthData = byMonth[monthKey];
            monthData.Sessions.Add(item.SessionId);
            byMonth[monthKey] = (monthData.Rounds + item.NumberOfRounds, monthData.Cost + cost, monthData.Sessions);
        }

        result.Totals = new CostTotalsDto
        {
            TotalRoundsFired = totalRounds,
            TotalCost = totalCost > 0 ? Math.Round(totalCost, 2) : null,
            AverageCostPerRound = totalRounds > 0 && totalCost > 0 ? Math.Round(totalCost / totalRounds, 2) : null
        };

        result.ByAmmunition = byAmmo
            .Select(kv => new CostByAmmoDto
            {
                AmmoId = kv.Key,
                AmmoName = kv.Value.Name,
                RoundsFired = kv.Value.Rounds,
                Cost = kv.Value.Cost > 0 ? Math.Round(kv.Value.Cost, 2) : null,
                CostPerRound = kv.Value.Rounds > 0 && kv.Value.Cost > 0 ? Math.Round(kv.Value.Cost / kv.Value.Rounds, 2) : null
            })
            .OrderByDescending(a => a.RoundsFired)
            .ToList();

        result.ByRifle = byRifle
            .Select(kv => new CostByRifleDto
            {
                RifleId = kv.Key,
                RifleName = kv.Value.Name,
                RoundsFired = kv.Value.Rounds,
                Cost = kv.Value.Cost > 0 ? Math.Round(kv.Value.Cost, 2) : null,
                Sessions = kv.Value.Sessions.Count
            })
            .OrderByDescending(r => r.RoundsFired)
            .ToList();

        result.ByMonth = byMonth
            .Select(kv => new CostByMonthDto
            {
                Month = kv.Key,
                RoundsFired = kv.Value.Rounds,
                Cost = kv.Value.Cost > 0 ? Math.Round(kv.Value.Cost, 2) : null,
                Sessions = kv.Value.Sessions.Count
            })
            .OrderBy(m => m.Month)
            .ToList();

        return result;
    }

    // Helper classes

    private class DopeDistanceData
    {
        public decimal AvgElevation { get; set; }
        public decimal StdDevElevation { get; set; }
        public decimal AvgWindage { get; set; }
        public decimal StdDevWindage { get; set; }
        public int Count { get; set; }
    }

    // Helper methods

    private static decimal CalculateStdDev(IEnumerable<decimal> values)
    {
        var list = values.ToList();
        if (list.Count <= 1) return 0;

        var avg = list.Average();
        var sumOfSquares = list.Sum(v => (v - avg) * (v - avg));
        var variance = sumOfSquares / list.Count;
        return (decimal)Math.Sqrt((double)variance);
    }

    private static DopeDataPointDto? TryInterpolate(
        int targetDistance,
        Dictionary<int, DopeDistanceData> directData,
        int interval)
    {
        // Find nearest lower and higher direct data points
        var distances = directData.Keys.OrderBy(d => d).ToList();

        int? lower = distances.Where(d => d < targetDistance).DefaultIfEmpty().Max();
        int? higher = distances.Where(d => d > targetDistance).DefaultIfEmpty().Min();

        // Only interpolate if both neighbors exist and are within 100 yards
        if (lower == null || lower == 0 || higher == null || higher == 0) return null;
        if (targetDistance - lower > 100 || higher - targetDistance > 100) return null;

        var lowerData = directData[lower.Value];
        var higherData = directData[higher.Value];

        // Linear interpolation
        var ratio = (decimal)(targetDistance - lower.Value) / (higher.Value - lower.Value);

        var elevationMils = lowerData.AvgElevation + ratio * (higherData.AvgElevation - lowerData.AvgElevation);
        var windageMils = lowerData.AvgWindage + ratio * (higherData.AvgWindage - lowerData.AvgWindage);

        return new DopeDataPointDto
        {
            Distance = targetDistance,
            ElevationMils = Math.Round(elevationMils, 3),
            ElevationMilsStdDev = 0,
            WindageMils = Math.Round(windageMils, 3),
            WindageMilsStdDev = 0,
            SessionCount = 0,
            DataSource = "interpolated"
        };
    }

    private static VelocityCorrelationDto? CalculateCorrelations(List<VelocitySessionDto> sessions)
    {
        var sessionsWithTemp = sessions
            .Where(s => s.Conditions?.Temperature.HasValue == true)
            .ToList();

        var sessionsWithDA = sessions
            .Where(s => s.Conditions?.DensityAltitude.HasValue == true)
            .ToList();

        if (sessionsWithTemp.Count < 3 && sessionsWithDA.Count < 3) return null;

        var result = new VelocityCorrelationDto();

        // Temperature correlation
        if (sessionsWithTemp.Count >= 3)
        {
            var temps = sessionsWithTemp.Select(s => (double)s.Conditions!.Temperature!.Value).ToList();
            var vels = sessionsWithTemp.Select(s => (double)s.AverageVelocity).ToList();

            var (correlation, slope) = CalculatePearsonCorrelation(temps, vels);
            result.TemperatureCorrelation = Math.Round((decimal)correlation, 2);
            result.VelocityPerDegreeF = Math.Round((decimal)slope, 2);
        }

        // Density altitude correlation
        if (sessionsWithDA.Count >= 3)
        {
            var das = sessionsWithDA.Select(s => (double)s.Conditions!.DensityAltitude!.Value).ToList();
            var vels = sessionsWithDA.Select(s => (double)s.AverageVelocity).ToList();

            var (correlation, slope) = CalculatePearsonCorrelation(das, vels);
            result.DensityAltitudeCorrelation = Math.Round((decimal)correlation, 2);
            // Convert to per-1000ft
            result.VelocityPer1000ftDA = Math.Round((decimal)(slope * 1000), 2);
        }

        return result;
    }

    private static (double correlation, double slope) CalculatePearsonCorrelation(List<double> x, List<double> y)
    {
        if (x.Count != y.Count || x.Count < 2) return (0, 0);

        var n = x.Count;
        var sumX = x.Sum();
        var sumY = y.Sum();
        var sumXY = x.Zip(y, (a, b) => a * b).Sum();
        var sumX2 = x.Sum(a => a * a);
        var sumY2 = y.Sum(b => b * b);

        var numerator = n * sumXY - sumX * sumY;
        var denominator = Math.Sqrt((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY));

        var correlation = denominator == 0 ? 0 : numerator / denominator;

        // Calculate slope for linear regression
        var meanX = sumX / n;
        var meanY = sumY / n;
        var slopeNumerator = x.Zip(y, (a, b) => (a - meanX) * (b - meanY)).Sum();
        var slopeDenominator = x.Sum(a => (a - meanX) * (a - meanX));
        var slope = slopeDenominator == 0 ? 0 : slopeNumerator / slopeDenominator;

        return (correlation, slope);
    }
}
