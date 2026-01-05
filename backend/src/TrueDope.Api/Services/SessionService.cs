using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TrueDope.Api.Data;
using TrueDope.Api.Data.Entities;
using TrueDope.Api.DTOs;
using TrueDope.Api.DTOs.Sessions;

namespace TrueDope.Api.Services;

public class SessionService : ISessionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SessionService> _logger;
    private readonly IGroupMeasurementCalculator _measurementCalculator;

    public SessionService(
        ApplicationDbContext context,
        ILogger<SessionService> logger,
        IGroupMeasurementCalculator measurementCalculator)
    {
        _context = context;
        _logger = logger;
        _measurementCalculator = measurementCalculator;
    }

    public async Task<PaginatedResponse<SessionListDto>> GetSessionsAsync(string userId, SessionFilterDto filter)
    {
        // Note: AsNoTracking() is implicit when using Select() projection
        // We don't need .Include() statements here since we use Select() projection below
        // which allows EF to generate efficient SQL without loading full entities
        var query = _context.RangeSessions
            .Where(s => s.UserId == userId);

        // Apply filters
        if (filter.RifleId.HasValue)
            query = query.Where(s => s.RifleSetupId == filter.RifleId.Value);

        if (filter.AmmoId.HasValue)
            query = query.Where(s =>
                (s.ChronoSession != null && s.ChronoSession.AmmunitionId == filter.AmmoId.Value) ||
                s.GroupEntries.Any(g => g.AmmunitionId == filter.AmmoId.Value));

        if (filter.HasDopeData.HasValue)
            query = filter.HasDopeData.Value
                ? query.Where(s => s.DopeEntries.Any())
                : query.Where(s => !s.DopeEntries.Any());

        if (filter.HasChronoData.HasValue)
            query = filter.HasChronoData.Value
                ? query.Where(s => s.ChronoSession != null)
                : query.Where(s => s.ChronoSession == null);

        if (filter.HasGroupData.HasValue)
            query = filter.HasGroupData.Value
                ? query.Where(s => s.GroupEntries.Any())
                : query.Where(s => !s.GroupEntries.Any());

        if (filter.FromDate.HasValue)
            query = query.Where(s => s.SessionDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(s => s.SessionDate <= filter.ToDate.Value);

        // Count total before pagination
        var totalItems = await query.CountAsync();

        // Apply sorting (with secondary sort by time for same-day sessions)
        query = filter.SortBy.ToLowerInvariant() switch
        {
            "sessiondate" => filter.SortDesc
                ? query.OrderByDescending(s => s.SessionDate)
                        .ThenByDescending(s => s.SessionTime ?? TimeSpan.Zero)
                : query.OrderBy(s => s.SessionDate)
                        .ThenBy(s => s.SessionTime ?? TimeSpan.Zero),
            "createdat" => filter.SortDesc
                ? query.OrderByDescending(s => s.CreatedAt)
                : query.OrderBy(s => s.CreatedAt),
            "rifle" => filter.SortDesc
                ? query.OrderByDescending(s => s.RifleSetup.Name)
                : query.OrderBy(s => s.RifleSetup.Name),
            _ => query.OrderByDescending(s => s.SessionDate)
                    .ThenByDescending(s => s.SessionTime ?? TimeSpan.Zero)
        };

        // Apply pagination
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var page = Math.Max(filter.Page, 1);

        var sessions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SessionListDto
            {
                Id = s.Id,
                SessionDate = s.SessionDate,
                SessionTime = s.SessionTime,
                Rifle = new RifleSummaryDto
                {
                    Id = s.RifleSetup.Id,
                    Name = s.RifleSetup.Name,
                    Caliber = s.RifleSetup.Caliber
                },
                LocationName = s.LocationName ?? s.SavedLocation!.Name,
                Temperature = s.Temperature,
                HasDopeData = s.DopeEntries.Any(),
                HasChronoData = s.ChronoSession != null,
                HasGroupData = s.GroupEntries.Any(),
                DopeEntryCount = s.DopeEntries.Count,
                VelocityReadingCount = s.ChronoSession != null ? s.ChronoSession.VelocityReadings.Count : 0,
                GroupEntryCount = s.GroupEntries.Count,
                ImageCount = s.Images.Count,
                CreatedAt = s.CreatedAt,
                // Ammunition name: prefer chrono session, fall back to first DOPE entry with ammo
                AmmunitionName = s.ChronoSession != null
                    ? s.ChronoSession.Ammunition.Manufacturer + " " + s.ChronoSession.Ammunition.Name
                    : s.DopeEntries.Where(d => d.Ammunition != null).Select(d => d.Ammunition!.Manufacturer + " " + d.Ammunition!.Name).FirstOrDefault(),
                AverageVelocity = s.ChronoSession != null ? s.ChronoSession.AverageVelocity : null,
                StandardDeviation = s.ChronoSession != null ? s.ChronoSession.StandardDeviation : null,
                ExtremeSpread = s.ChronoSession != null ? s.ChronoSession.ExtremeSpread : null,
                // DOPE summary
                MinDopeDistance = s.DopeEntries.Any() ? s.DopeEntries.Min(d => d.Distance) : (int?)null,
                MaxDopeDistance = s.DopeEntries.Any() ? s.DopeEntries.Max(d => d.Distance) : (int?)null,
                // Group summary - best (smallest) group MOA
                BestGroupMoa = s.GroupEntries.Any(g => g.GroupSizeMoa != null)
                    ? s.GroupEntries.Where(g => g.GroupSizeMoa != null).Min(g => g.GroupSizeMoa)
                    : (decimal?)null,
                BestGroupDistance = s.GroupEntries.Any(g => g.GroupSizeMoa != null)
                    ? s.GroupEntries.Where(g => g.GroupSizeMoa != null).OrderBy(g => g.GroupSizeMoa).Select(g => g.Distance).FirstOrDefault()
                    : (int?)null
            })
            .ToListAsync();

        return new PaginatedResponse<SessionListDto>
        {
            Items = sessions,
            Pagination = new PaginationInfo
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems
            }
        };
    }

    public async Task<SessionDetailDto?> GetSessionAsync(string userId, int sessionId)
    {
        var session = await _context.RangeSessions
            .Include(s => s.RifleSetup)
            .Include(s => s.SavedLocation)
            .Include(s => s.DopeEntries)
                .ThenInclude(d => d.Ammunition)
            .Include(s => s.DopeEntries)
                .ThenInclude(d => d.AmmoLot)
            .Include(s => s.ChronoSession)
                .ThenInclude(c => c!.Ammunition)
            .Include(s => s.ChronoSession)
                .ThenInclude(c => c!.AmmoLot)
            .Include(s => s.ChronoSession)
                .ThenInclude(c => c!.VelocityReadings)
            .Include(s => s.GroupEntries)
                .ThenInclude(g => g.Ammunition)
            .Include(s => s.GroupEntries)
                .ThenInclude(g => g.AmmoLot)
            .Include(s => s.GroupEntries)
                .ThenInclude(g => g.Images)
            .Include(s => s.GroupEntries)
                .ThenInclude(g => g.Measurement)
                    .ThenInclude(m => m!.OriginalImage)
            .Include(s => s.GroupEntries)
                .ThenInclude(g => g.Measurement)
                    .ThenInclude(m => m!.AnnotatedImage)
            .Include(s => s.Images)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

        if (session == null)
            return null;

        return MapToDetailDto(session);
    }

    public async Task<int> CreateSessionAsync(string userId, CreateSessionDto dto)
    {
        // Validate rifle belongs to user
        var rifleExists = await _context.RifleSetups
            .AnyAsync(r => r.Id == dto.RifleSetupId && r.UserId == userId);

        if (!rifleExists)
            throw new ArgumentException("Rifle not found or does not belong to user");

        // Validate saved location if provided
        if (dto.SavedLocationId.HasValue)
        {
            var locationExists = await _context.SavedLocations
                .AnyAsync(l => l.Id == dto.SavedLocationId.Value && l.UserId == userId);

            if (!locationExists)
                throw new ArgumentException("Location not found or does not belong to user");
        }

        // Convert to UTC for storage (frontend sends local time as ISO timestamp)
        var sessionDate = dto.SessionDate.Kind == DateTimeKind.Utc
            ? dto.SessionDate
            : dto.SessionDate.ToUniversalTime();

        var session = new RangeSession
        {
            UserId = userId,
            SessionDate = sessionDate,
            SessionTime = dto.SessionTime,
            RifleSetupId = dto.RifleSetupId,
            SavedLocationId = dto.SavedLocationId,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            LocationName = dto.LocationName,
            Temperature = dto.Temperature,
            Humidity = dto.Humidity,
            WindSpeed = dto.WindSpeed,
            WindDirection = dto.WindDirection,
            Pressure = dto.Pressure,
            DensityAltitude = dto.DensityAltitude,
            Notes = dto.Notes
        };

        _context.RangeSessions.Add(session);
        await _context.SaveChangesAsync();

        // Add DOPE entries
        if (dto.DopeEntries?.Any() == true)
        {
            foreach (var dopeDto in dto.DopeEntries)
            {
                var dopeEntry = new DopeEntry
                {
                    RangeSessionId = session.Id,
                    Distance = dopeDto.Distance,
                    ElevationMils = dopeDto.ElevationMils,
                    WindageMils = dopeDto.WindageMils,
                    Notes = dopeDto.Notes
                };
                _context.DopeEntries.Add(dopeEntry);
            }
        }

        // Add Chrono session
        if (dto.ChronoSession != null)
        {
            await ValidateAmmoOwnershipAsync(userId, dto.ChronoSession.AmmunitionId, dto.ChronoSession.AmmoLotId);

            var chronoSession = new ChronoSession
            {
                RangeSessionId = session.Id,
                AmmunitionId = dto.ChronoSession.AmmunitionId,
                AmmoLotId = dto.ChronoSession.AmmoLotId,
                BarrelTemperature = dto.ChronoSession.BarrelTemperature,
                Notes = dto.ChronoSession.Notes
            };

            _context.ChronoSessions.Add(chronoSession);
            await _context.SaveChangesAsync();

            // Add velocity readings
            if (dto.ChronoSession.VelocityReadings?.Any() == true)
            {
                foreach (var readingDto in dto.ChronoSession.VelocityReadings)
                {
                    var reading = new VelocityReading
                    {
                        ChronoSessionId = chronoSession.Id,
                        ShotNumber = readingDto.ShotNumber,
                        Velocity = readingDto.Velocity
                    };
                    _context.VelocityReadings.Add(reading);
                }
                await _context.SaveChangesAsync();

                // Calculate and update stats
                await UpdateChronoStatsAsync(chronoSession.Id);
            }
        }

        // Add Group entries
        if (dto.GroupEntries?.Any() == true)
        {
            foreach (var groupDto in dto.GroupEntries)
            {
                if (groupDto.AmmunitionId.HasValue)
                    await ValidateAmmoOwnershipAsync(userId, groupDto.AmmunitionId.Value, groupDto.AmmoLotId);

                var groupEntry = new GroupEntry
                {
                    RangeSessionId = session.Id,
                    GroupNumber = groupDto.GroupNumber,
                    Distance = groupDto.Distance,
                    NumberOfShots = groupDto.NumberOfShots,
                    GroupSizeMoa = groupDto.GroupSizeMoa,
                    MeanRadiusMoa = groupDto.MeanRadiusMoa,
                    AmmunitionId = groupDto.AmmunitionId,
                    AmmoLotId = groupDto.AmmoLotId,
                    Notes = groupDto.Notes
                };
                _context.GroupEntries.Add(groupEntry);
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Created session {SessionId} for user {UserId}", session.Id, userId);

        return session.Id;
    }

    public async Task<SessionDetailDto?> UpdateSessionAsync(string userId, int sessionId, UpdateSessionDto dto)
    {
        var session = await _context.RangeSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

        if (session == null)
            return null;

        // Update fields if provided
        if (dto.SessionDate.HasValue)
        {
            // Convert to UTC for storage (frontend sends local time as ISO timestamp)
            session.SessionDate = dto.SessionDate.Value.Kind == DateTimeKind.Utc
                ? dto.SessionDate.Value
                : dto.SessionDate.Value.ToUniversalTime();
        }

        if (dto.SessionTime.HasValue)
            session.SessionTime = dto.SessionTime;

        if (dto.RifleSetupId.HasValue)
        {
            var rifleExists = await _context.RifleSetups
                .AnyAsync(r => r.Id == dto.RifleSetupId.Value && r.UserId == userId);

            if (!rifleExists)
                throw new ArgumentException("Rifle not found or does not belong to user");

            session.RifleSetupId = dto.RifleSetupId.Value;
        }

        if (dto.SavedLocationId.HasValue)
        {
            var locationExists = await _context.SavedLocations
                .AnyAsync(l => l.Id == dto.SavedLocationId.Value && l.UserId == userId);

            if (!locationExists)
                throw new ArgumentException("Location not found or does not belong to user");

            session.SavedLocationId = dto.SavedLocationId;
        }

        if (dto.Latitude.HasValue) session.Latitude = dto.Latitude;
        if (dto.Longitude.HasValue) session.Longitude = dto.Longitude;
        if (dto.LocationName != null) session.LocationName = dto.LocationName;
        if (dto.Temperature.HasValue) session.Temperature = dto.Temperature;
        if (dto.Humidity.HasValue) session.Humidity = dto.Humidity;
        if (dto.WindSpeed.HasValue) session.WindSpeed = dto.WindSpeed;
        if (dto.WindDirection.HasValue) session.WindDirection = dto.WindDirection;
        if (dto.Pressure.HasValue) session.Pressure = dto.Pressure;
        if (dto.DensityAltitude.HasValue) session.DensityAltitude = dto.DensityAltitude;
        if (dto.Notes != null) session.Notes = dto.Notes;

        session.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Return the updated session detail
        return await GetSessionAsync(userId, sessionId);
    }

    public async Task<bool> DeleteSessionAsync(string userId, int sessionId)
    {
        var session = await _context.RangeSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

        if (session == null)
            return false;

        _context.RangeSessions.Remove(session);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted session {SessionId} for user {UserId}", sessionId, userId);

        return true;
    }

    // ==================== DOPE Operations ====================

    public async Task<int> AddDopeEntryAsync(string userId, int sessionId, CreateDopeEntryDto dto)
    {
        var session = await _context.RangeSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

        if (session == null)
            throw new ArgumentException("Session not found");

        // Validate ammunition belongs to user if provided
        if (dto.AmmunitionId.HasValue)
        {
            var ammoExists = await _context.Ammunition
                .AnyAsync(a => a.Id == dto.AmmunitionId.Value && a.UserId == userId);
            if (!ammoExists)
                throw new ArgumentException("Ammunition not found");
        }

        // Validate lot belongs to the ammunition if provided
        if (dto.AmmoLotId.HasValue)
        {
            var lotValid = await _context.AmmoLots
                .AnyAsync(l => l.Id == dto.AmmoLotId.Value &&
                              l.UserId == userId &&
                              (!dto.AmmunitionId.HasValue || l.AmmunitionId == dto.AmmunitionId.Value));
            if (!lotValid)
                throw new ArgumentException("Ammunition lot not found or doesn't match ammunition");
        }

        var dopeEntry = new DopeEntry
        {
            RangeSessionId = sessionId,
            Distance = dto.Distance,
            ElevationMils = dto.ElevationMils,
            WindageMils = dto.WindageMils,
            Notes = dto.Notes,
            AmmunitionId = dto.AmmunitionId,
            AmmoLotId = dto.AmmoLotId
        };

        _context.DopeEntries.Add(dopeEntry);
        await _context.SaveChangesAsync();

        return dopeEntry.Id;
    }

    public async Task<bool> UpdateDopeEntryAsync(string userId, int dopeEntryId, UpdateDopeEntryDto dto)
    {
        var entry = await _context.DopeEntries
            .Include(d => d.RangeSession)
            .FirstOrDefaultAsync(d => d.Id == dopeEntryId && d.RangeSession.UserId == userId);

        if (entry == null)
            return false;

        // Validate ammunition belongs to user if provided
        if (dto.AmmunitionId.HasValue)
        {
            var ammoExists = await _context.Ammunition
                .AnyAsync(a => a.Id == dto.AmmunitionId.Value && a.UserId == userId);
            if (!ammoExists)
                throw new ArgumentException("Ammunition not found");
        }

        // Validate lot belongs to the ammunition if provided
        if (dto.AmmoLotId.HasValue)
        {
            var lotValid = await _context.AmmoLots
                .AnyAsync(l => l.Id == dto.AmmoLotId.Value &&
                              l.UserId == userId &&
                              (!dto.AmmunitionId.HasValue || l.AmmunitionId == dto.AmmunitionId.Value));
            if (!lotValid)
                throw new ArgumentException("Ammunition lot not found or doesn't match ammunition");
        }

        if (dto.ElevationMils.HasValue) entry.ElevationMils = dto.ElevationMils.Value;
        if (dto.WindageMils.HasValue) entry.WindageMils = dto.WindageMils.Value;
        if (dto.Notes != null) entry.Notes = dto.Notes;
        if (dto.AmmunitionId.HasValue) entry.AmmunitionId = dto.AmmunitionId;
        if (dto.AmmoLotId.HasValue) entry.AmmoLotId = dto.AmmoLotId;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteDopeEntryAsync(string userId, int dopeEntryId)
    {
        var entry = await _context.DopeEntries
            .Include(d => d.RangeSession)
            .FirstOrDefaultAsync(d => d.Id == dopeEntryId && d.RangeSession.UserId == userId);

        if (entry == null)
            return false;

        _context.DopeEntries.Remove(entry);
        await _context.SaveChangesAsync();
        return true;
    }

    // ==================== Chrono Operations ====================

    public async Task<int> AddChronoSessionAsync(string userId, int sessionId, CreateChronoSessionDto dto)
    {
        var session = await _context.RangeSessions
            .Include(s => s.ChronoSession)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

        if (session == null)
            throw new ArgumentException("Session not found");

        if (session.ChronoSession != null)
            throw new InvalidOperationException("Session already has chrono data");

        await ValidateAmmoOwnershipAsync(userId, dto.AmmunitionId, dto.AmmoLotId);

        var chronoSession = new ChronoSession
        {
            RangeSessionId = sessionId,
            AmmunitionId = dto.AmmunitionId,
            AmmoLotId = dto.AmmoLotId,
            BarrelTemperature = dto.BarrelTemperature,
            Notes = dto.Notes
        };

        _context.ChronoSessions.Add(chronoSession);
        await _context.SaveChangesAsync();

        // Add velocity readings
        if (dto.VelocityReadings?.Any() == true)
        {
            foreach (var readingDto in dto.VelocityReadings)
            {
                var reading = new VelocityReading
                {
                    ChronoSessionId = chronoSession.Id,
                    ShotNumber = readingDto.ShotNumber,
                    Velocity = readingDto.Velocity
                };
                _context.VelocityReadings.Add(reading);
            }
            await _context.SaveChangesAsync();
            await UpdateChronoStatsAsync(chronoSession.Id);
        }

        return chronoSession.Id;
    }

    public async Task<bool> UpdateChronoSessionAsync(string userId, int chronoSessionId, UpdateChronoSessionDto dto)
    {
        var chrono = await _context.ChronoSessions
            .Include(c => c.RangeSession)
            .FirstOrDefaultAsync(c => c.Id == chronoSessionId && c.RangeSession.UserId == userId);

        if (chrono == null)
            return false;

        if (dto.AmmunitionId.HasValue)
        {
            await ValidateAmmoOwnershipAsync(userId, dto.AmmunitionId.Value, dto.AmmoLotId);
            chrono.AmmunitionId = dto.AmmunitionId.Value;
        }

        if (dto.AmmoLotId.HasValue)
            chrono.AmmoLotId = dto.AmmoLotId;

        if (dto.BarrelTemperature.HasValue)
            chrono.BarrelTemperature = dto.BarrelTemperature;

        if (dto.Notes != null)
            chrono.Notes = dto.Notes;

        chrono.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteChronoSessionAsync(string userId, int chronoSessionId)
    {
        var chrono = await _context.ChronoSessions
            .Include(c => c.RangeSession)
            .FirstOrDefaultAsync(c => c.Id == chronoSessionId && c.RangeSession.UserId == userId);

        if (chrono == null)
            return false;

        _context.ChronoSessions.Remove(chrono);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> AddVelocityReadingAsync(string userId, int chronoSessionId, CreateVelocityReadingDto dto)
    {
        var chrono = await _context.ChronoSessions
            .Include(c => c.RangeSession)
            .FirstOrDefaultAsync(c => c.Id == chronoSessionId && c.RangeSession.UserId == userId);

        if (chrono == null)
            throw new ArgumentException("Chrono session not found");

        var reading = new VelocityReading
        {
            ChronoSessionId = chronoSessionId,
            ShotNumber = dto.ShotNumber,
            Velocity = dto.Velocity
        };

        _context.VelocityReadings.Add(reading);
        await _context.SaveChangesAsync();

        await UpdateChronoStatsAsync(chronoSessionId);

        return reading.Id;
    }

    public async Task<bool> DeleteVelocityReadingAsync(string userId, int readingId)
    {
        var reading = await _context.VelocityReadings
            .Include(v => v.ChronoSession)
                .ThenInclude(c => c.RangeSession)
            .FirstOrDefaultAsync(v => v.Id == readingId && v.ChronoSession.RangeSession.UserId == userId);

        if (reading == null)
            return false;

        var chronoSessionId = reading.ChronoSessionId;

        _context.VelocityReadings.Remove(reading);
        await _context.SaveChangesAsync();

        await UpdateChronoStatsAsync(chronoSessionId);

        return true;
    }

    // ==================== Group Operations ====================

    public async Task<int> AddGroupEntryAsync(string userId, int sessionId, CreateGroupEntryDto dto)
    {
        var session = await _context.RangeSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

        if (session == null)
            throw new ArgumentException("Session not found");

        if (dto.AmmunitionId.HasValue)
            await ValidateAmmoOwnershipAsync(userId, dto.AmmunitionId.Value, dto.AmmoLotId);

        var groupEntry = new GroupEntry
        {
            RangeSessionId = sessionId,
            GroupNumber = dto.GroupNumber,
            Distance = dto.Distance,
            NumberOfShots = dto.NumberOfShots,
            GroupSizeMoa = dto.GroupSizeMoa,
            MeanRadiusMoa = dto.MeanRadiusMoa,
            AmmunitionId = dto.AmmunitionId,
            AmmoLotId = dto.AmmoLotId,
            Notes = dto.Notes
        };

        _context.GroupEntries.Add(groupEntry);
        await _context.SaveChangesAsync();

        return groupEntry.Id;
    }

    public async Task<bool> UpdateGroupEntryAsync(string userId, int groupEntryId, UpdateGroupEntryDto dto)
    {
        var entry = await _context.GroupEntries
            .Include(g => g.RangeSession)
            .FirstOrDefaultAsync(g => g.Id == groupEntryId && g.RangeSession.UserId == userId);

        if (entry == null)
            return false;

        if (dto.AmmunitionId.HasValue)
        {
            await ValidateAmmoOwnershipAsync(userId, dto.AmmunitionId.Value, dto.AmmoLotId);
            entry.AmmunitionId = dto.AmmunitionId;
        }

        if (dto.AmmoLotId.HasValue) entry.AmmoLotId = dto.AmmoLotId;
        if (dto.Distance.HasValue) entry.Distance = dto.Distance.Value;
        if (dto.NumberOfShots.HasValue) entry.NumberOfShots = dto.NumberOfShots.Value;
        if (dto.GroupSizeMoa.HasValue) entry.GroupSizeMoa = dto.GroupSizeMoa;
        if (dto.MeanRadiusMoa.HasValue) entry.MeanRadiusMoa = dto.MeanRadiusMoa;
        if (dto.Notes != null) entry.Notes = dto.Notes;

        entry.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteGroupEntryAsync(string userId, int groupEntryId)
    {
        var entry = await _context.GroupEntries
            .Include(g => g.RangeSession)
            .FirstOrDefaultAsync(g => g.Id == groupEntryId && g.RangeSession.UserId == userId);

        if (entry == null)
            return false;

        _context.GroupEntries.Remove(entry);
        await _context.SaveChangesAsync();
        return true;
    }

    // ==================== Helper Methods ====================

    private async Task ValidateAmmoOwnershipAsync(string userId, int ammoId, int? lotId)
    {
        var ammoExists = await _context.Ammunition
            .AnyAsync(a => a.Id == ammoId && a.UserId == userId);

        if (!ammoExists)
            throw new ArgumentException("Ammunition not found or does not belong to user");

        if (lotId.HasValue)
        {
            var lotExists = await _context.AmmoLots
                .AnyAsync(l => l.Id == lotId.Value && l.AmmunitionId == ammoId && l.UserId == userId);

            if (!lotExists)
                throw new ArgumentException("Ammo lot not found or does not belong to this ammunition");
        }
    }

    private async Task UpdateChronoStatsAsync(int chronoSessionId)
    {
        var chrono = await _context.ChronoSessions
            .Include(c => c.VelocityReadings)
            .FirstOrDefaultAsync(c => c.Id == chronoSessionId);

        if (chrono == null)
            return;

        var velocities = chrono.VelocityReadings.Select(v => v.Velocity).ToList();
        var stats = VelocityStatsCalculator.Calculate(velocities);

        chrono.NumberOfRounds = stats.Count;
        chrono.AverageVelocity = stats.Average;
        chrono.HighVelocity = stats.High;
        chrono.LowVelocity = stats.Low;
        chrono.ExtremeSpread = stats.ExtremeSpread;
        chrono.StandardDeviation = stats.StandardDeviation;
        chrono.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private GroupMeasurementDto MapMeasurementToDto(GroupMeasurement measurement, int distanceYards)
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
                ? _measurementCalculator.InchesToMoa(measurement.ExtremeSpreadCtc.Value, distanceYards)
                : null,
            ExtremeSpreadEteMoa = measurement.ExtremeSpreadEte.HasValue
                ? _measurementCalculator.InchesToMoa(measurement.ExtremeSpreadEte.Value, distanceYards)
                : null,
            MeanRadiusMoa = measurement.MeanRadius.HasValue
                ? _measurementCalculator.InchesToMoa(measurement.MeanRadius.Value, distanceYards)
                : null,
            CalibrationMethod = measurement.CalibrationMethod.ToString().ToLowerInvariant(),
            MeasurementConfidence = measurement.MeasurementConfidence,
            OriginalImage = measurement.OriginalImage != null ? new ImageDto
            {
                Id = measurement.OriginalImage.Id,
                Url = $"/api/images/{measurement.OriginalImage.Id}",
                ThumbnailUrl = $"/api/images/{measurement.OriginalImage.Id}/thumbnail",
                Caption = measurement.OriginalImage.Caption,
                OriginalFileName = measurement.OriginalImage.OriginalFileName,
                FileSize = measurement.OriginalImage.FileSize
            } : null,
            AnnotatedImage = measurement.AnnotatedImage != null ? new ImageDto
            {
                Id = measurement.AnnotatedImage.Id,
                Url = $"/api/images/{measurement.AnnotatedImage.Id}",
                ThumbnailUrl = $"/api/images/{measurement.AnnotatedImage.Id}/thumbnail",
                Caption = measurement.AnnotatedImage.Caption,
                OriginalFileName = measurement.AnnotatedImage.OriginalFileName,
                FileSize = measurement.AnnotatedImage.FileSize
            } : null,
            CreatedAt = measurement.CreatedAt,
            UpdatedAt = measurement.UpdatedAt
        };
    }

    private SessionDetailDto MapToDetailDto(RangeSession session)
    {
        return new SessionDetailDto
        {
            Id = session.Id,
            SessionDate = session.SessionDate,
            SessionTime = session.SessionTime,
            Rifle = new RifleSummaryDto
            {
                Id = session.RifleSetup.Id,
                Name = session.RifleSetup.Name,
                Caliber = session.RifleSetup.Caliber
            },
            SavedLocation = session.SavedLocation != null ? new LocationSummaryDto
            {
                Id = session.SavedLocation.Id,
                Name = session.SavedLocation.Name
            } : null,
            Latitude = session.Latitude,
            Longitude = session.Longitude,
            LocationName = session.LocationName,
            Temperature = session.Temperature,
            Humidity = session.Humidity,
            WindSpeed = session.WindSpeed,
            WindDirection = session.WindDirection,
            WindDirectionCardinal = session.WindDirectionCardinal,
            Pressure = session.Pressure,
            DensityAltitude = session.DensityAltitude,
            Notes = session.Notes,
            DopeEntries = session.DopeEntries.Select(d => new DopeEntryDto
            {
                Id = d.Id,
                Distance = d.Distance,
                ElevationMils = d.ElevationMils,
                WindageMils = d.WindageMils,
                ElevationInches = d.ElevationInches,
                WindageInches = d.WindageInches,
                ElevationMoa = d.ElevationMoa,
                WindageMoa = d.WindageMoa,
                Notes = d.Notes,
                Ammunition = d.Ammunition != null ? new AmmoSummaryDto
                {
                    Id = d.Ammunition.Id,
                    DisplayName = d.Ammunition.DisplayName,
                    Manufacturer = d.Ammunition.Manufacturer,
                    Name = d.Ammunition.Name,
                    Caliber = d.Ammunition.Caliber,
                    Grain = d.Ammunition.Grain
                } : null,
                AmmoLot = d.AmmoLot != null ? new AmmoLotSummaryDto
                {
                    Id = d.AmmoLot.Id,
                    LotNumber = d.AmmoLot.LotNumber
                } : null
            }).OrderBy(d => d.Distance).ToList(),
            ChronoSession = session.ChronoSession != null ? new ChronoSessionDto
            {
                Id = session.ChronoSession.Id,
                Ammunition = new AmmoSummaryDto
                {
                    Id = session.ChronoSession.Ammunition.Id,
                    DisplayName = session.ChronoSession.Ammunition.DisplayName,
                    Manufacturer = session.ChronoSession.Ammunition.Manufacturer,
                    Name = session.ChronoSession.Ammunition.Name,
                    Caliber = session.ChronoSession.Ammunition.Caliber,
                    Grain = session.ChronoSession.Ammunition.Grain
                },
                AmmoLot = session.ChronoSession.AmmoLot != null ? new AmmoLotSummaryDto
                {
                    Id = session.ChronoSession.AmmoLot.Id,
                    LotNumber = session.ChronoSession.AmmoLot.LotNumber
                } : null,
                BarrelTemperature = session.ChronoSession.BarrelTemperature,
                NumberOfRounds = session.ChronoSession.NumberOfRounds,
                AverageVelocity = session.ChronoSession.AverageVelocity,
                HighVelocity = session.ChronoSession.HighVelocity,
                LowVelocity = session.ChronoSession.LowVelocity,
                StandardDeviation = session.ChronoSession.StandardDeviation,
                ExtremeSpread = session.ChronoSession.ExtremeSpread,
                Notes = session.ChronoSession.Notes,
                VelocityReadings = session.ChronoSession.VelocityReadings
                    .OrderBy(v => v.ShotNumber)
                    .Select(v => new VelocityReadingDto
                    {
                        Id = v.Id,
                        ShotNumber = v.ShotNumber,
                        Velocity = v.Velocity
                    }).ToList()
            } : null,
            GroupEntries = session.GroupEntries.Select(g => new GroupEntryDto
            {
                Id = g.Id,
                GroupNumber = g.GroupNumber,
                Distance = g.Distance,
                NumberOfShots = g.NumberOfShots,
                GroupSizeMoa = g.GroupSizeMoa,
                MeanRadiusMoa = g.MeanRadiusMoa,
                GroupSizeInches = g.GroupSizeInches,
                Ammunition = g.Ammunition != null ? new AmmoSummaryDto
                {
                    Id = g.Ammunition.Id,
                    DisplayName = g.Ammunition.DisplayName,
                    Manufacturer = g.Ammunition.Manufacturer,
                    Name = g.Ammunition.Name,
                    Caliber = g.Ammunition.Caliber,
                    Grain = g.Ammunition.Grain
                } : null,
                AmmoLot = g.AmmoLot != null ? new AmmoLotSummaryDto
                {
                    Id = g.AmmoLot.Id,
                    LotNumber = g.AmmoLot.LotNumber
                } : null,
                Notes = g.Notes,
                // Filter out measurement images from group images to avoid duplication
                // Measurement images are shown via Measurement.OriginalImage/AnnotatedImage
                Images = g.Images
                    .Where(i =>
                        (g.Measurement?.OriginalImageId == null || i.Id != g.Measurement.OriginalImageId) &&
                        (g.Measurement?.AnnotatedImageId == null || i.Id != g.Measurement.AnnotatedImageId))
                    .Select(i => new ImageDto
                    {
                        Id = i.Id,
                        Url = $"/api/images/{i.Id}",
                        ThumbnailUrl = $"/api/images/{i.Id}/thumbnail",
                        Caption = i.Caption,
                        OriginalFileName = i.OriginalFileName,
                        FileSize = i.FileSize
                    }).ToList(),
                Measurement = g.Measurement != null ? MapMeasurementToDto(g.Measurement, g.Distance) : null
            }).OrderBy(g => g.GroupNumber).ToList(),
            Images = session.Images.Select(i => new ImageDto
            {
                Id = i.Id,
                Url = $"/api/images/{i.Id}",
                ThumbnailUrl = $"/api/images/{i.Id}/thumbnail",
                Caption = i.Caption,
                OriginalFileName = i.OriginalFileName,
                FileSize = i.FileSize
            }).ToList(),
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
    }
}
