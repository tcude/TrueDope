using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TrueDope.Api.Data;
using TrueDope.Api.Data.Entities;
using TrueDope.Api.DTOs.Admin;

namespace TrueDope.Api.Services;

public class UserDataCloneService : IUserDataCloneService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IStorageService _storageService;
    private readonly IAdminAuditService _auditService;
    private readonly ILogger<UserDataCloneService> _logger;

    private const string ImageBucket = "truedope-images";

    public UserDataCloneService(
        ApplicationDbContext context,
        UserManager<User> userManager,
        IStorageService storageService,
        IAdminAuditService auditService,
        ILogger<UserDataCloneService> logger)
    {
        _context = context;
        _userManager = userManager;
        _storageService = storageService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ClonePreviewResponse> PreviewCloneAsync(
        string sourceUserId,
        string targetUserId,
        CancellationToken cancellationToken = default)
    {
        var sourceUser = await _userManager.FindByIdAsync(sourceUserId)
            ?? throw new ArgumentException($"Source user not found: {sourceUserId}");
        var targetUser = await _userManager.FindByIdAsync(targetUserId)
            ?? throw new ArgumentException($"Target user not found: {targetUserId}");

        if (sourceUserId == targetUserId)
            throw new ArgumentException("Source and target users cannot be the same");

        var response = new ClonePreviewResponse
        {
            SourceUserId = sourceUserId,
            TargetUserId = targetUserId,
            SourceUserEmail = sourceUser.Email ?? "",
            TargetUserEmail = targetUser.Email ?? "",
            SourceDataToCopy = await GetDataCountsAsync(sourceUserId, cancellationToken),
            TargetDataToDelete = await GetDataCountsAsync(targetUserId, cancellationToken)
        };

        return response;
    }

    public async Task<CloneUserDataResponse> CloneUserDataAsync(
        string sourceUserId,
        string targetUserId,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // Validate users
        var sourceUser = await _userManager.FindByIdAsync(sourceUserId)
            ?? throw new ArgumentException($"Source user not found: {sourceUserId}");
        var targetUser = await _userManager.FindByIdAsync(targetUserId)
            ?? throw new ArgumentException($"Target user not found: {targetUserId}");

        if (sourceUserId == targetUserId)
            throw new ArgumentException("Source and target users cannot be the same");

        _logger.LogInformation(
            "Starting user data clone from {SourceUserId} ({SourceEmail}) to {TargetUserId} ({TargetEmail})",
            sourceUserId, sourceUser.Email, targetUserId, targetUser.Email);

        var statistics = new CloneStatistics();
        var copiedMinioFiles = new List<string>();

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Phase 1: Delete target user's existing data
            await DeleteTargetUserDataAsync(targetUserId, statistics, copiedMinioFiles, cancellationToken);

            // Phase 2: Copy source user's data to target
            var mappings = new IdMappings();

            await CopyUserPreferencesAsync(sourceUserId, targetUserId, statistics, cancellationToken);
            await CopySavedLocationsAsync(sourceUserId, targetUserId, mappings, statistics, cancellationToken);
            await CopyRifleSetupsAsync(sourceUserId, targetUserId, mappings, statistics, cancellationToken);
            await CopyAmmunitionAsync(sourceUserId, targetUserId, mappings, statistics, cancellationToken);
            await CopyAmmoLotsAsync(sourceUserId, targetUserId, mappings, statistics, cancellationToken);
            await CopyRangeSessionsAsync(sourceUserId, targetUserId, mappings, statistics, cancellationToken);
            await CopyChronoSessionsAsync(sourceUserId, targetUserId, mappings, statistics, cancellationToken);
            await CopyDopeEntriesAsync(sourceUserId, targetUserId, mappings, statistics, cancellationToken);
            await CopyGroupEntriesAsync(sourceUserId, targetUserId, mappings, statistics, cancellationToken);
            await CopyVelocityReadingsAsync(mappings, statistics, cancellationToken);
            await CopyImagesAsync(sourceUserId, targetUserId, mappings, statistics, copiedMinioFiles, cancellationToken);
            await CopyGroupMeasurementsAsync(mappings, statistics, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            stopwatch.Stop();

            // Log audit
            await _auditService.LogActionAsync(new AdminAuditLogEntry
            {
                AdminUserId = adminUserId,
                ActionType = "UserDataCloned",
                TargetUserId = targetUserId,
                Details = new { SourceUserId = sourceUserId, Statistics = statistics, DurationMs = stopwatch.ElapsedMilliseconds }
            });

            _logger.LogInformation(
                "Successfully cloned user data from {SourceUserId} to {TargetUserId} in {DurationMs}ms",
                sourceUserId, targetUserId, stopwatch.ElapsedMilliseconds);

            return new CloneUserDataResponse
            {
                Success = true,
                SourceUserId = sourceUserId,
                TargetUserId = targetUserId,
                Statistics = statistics,
                CompletedAt = DateTime.UtcNow,
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clone user data from {SourceUserId} to {TargetUserId}", sourceUserId, targetUserId);

            await transaction.RollbackAsync(cancellationToken);

            // Cleanup any MinIO files that were copied before the failure
            foreach (var fileName in copiedMinioFiles)
            {
                try
                {
                    await _storageService.DeleteFileAsync(ImageBucket, fileName);
                    _logger.LogDebug("Cleaned up MinIO file during rollback: {FileName}", fileName);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to cleanup MinIO file during rollback: {FileName}", fileName);
                }
            }

            throw;
        }
    }

    private async Task<DataCounts> GetDataCountsAsync(string userId, CancellationToken cancellationToken)
    {
        return new DataCounts
        {
            RifleSetups = await _context.RifleSetups.CountAsync(r => r.UserId == userId, cancellationToken),
            Ammunition = await _context.Ammunition.CountAsync(a => a.UserId == userId, cancellationToken),
            AmmoLots = await _context.AmmoLots.CountAsync(l => l.UserId == userId, cancellationToken),
            SavedLocations = await _context.SavedLocations.CountAsync(l => l.UserId == userId, cancellationToken),
            RangeSessions = await _context.RangeSessions.CountAsync(s => s.UserId == userId, cancellationToken),
            DopeEntries = await _context.DopeEntries.CountAsync(d => d.RangeSession.UserId == userId, cancellationToken),
            ChronoSessions = await _context.ChronoSessions.CountAsync(c => c.RangeSession.UserId == userId, cancellationToken),
            VelocityReadings = await _context.VelocityReadings.CountAsync(v => v.ChronoSession.RangeSession.UserId == userId, cancellationToken),
            GroupEntries = await _context.GroupEntries.CountAsync(g => g.RangeSession.UserId == userId, cancellationToken),
            GroupMeasurements = await _context.GroupMeasurements.CountAsync(m => m.GroupEntry.RangeSession.UserId == userId, cancellationToken),
            Images = await _context.Images.CountAsync(i => i.UserId == userId, cancellationToken),
            HasUserPreferences = await _context.UserPreferences.AnyAsync(p => p.UserId == userId, cancellationToken)
        };
    }

    private async Task DeleteTargetUserDataAsync(
        string targetUserId,
        CloneStatistics statistics,
        List<string> deletedMinioFiles,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Deleting existing data for target user {TargetUserId}", targetUserId);

        // Get images first so we can delete from MinIO
        var images = await _context.Images
            .Where(i => i.UserId == targetUserId)
            .ToListAsync(cancellationToken);

        // Delete from MinIO
        foreach (var image in images)
        {
            try
            {
                if (!string.IsNullOrEmpty(image.FileName))
                    await _storageService.DeleteFileAsync(ImageBucket, image.FileName);
                if (!string.IsNullOrEmpty(image.ThumbnailFileName))
                    await _storageService.DeleteFileAsync(ImageBucket, image.ThumbnailFileName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete MinIO image {FileName} during cleanup", image.FileName);
            }
        }
        statistics.ImagesDeleted = images.Count;

        // Delete in reverse dependency order using raw SQL for efficiency
        // GroupMeasurements depend on GroupEntries
        var groupMeasurementsDeleted = await _context.GroupMeasurements
            .Where(m => m.GroupEntry.RangeSession.UserId == targetUserId)
            .ExecuteDeleteAsync(cancellationToken);

        // VelocityReadings depend on ChronoSessions
        var velocityReadingsDeleted = await _context.VelocityReadings
            .Where(v => v.ChronoSession.RangeSession.UserId == targetUserId)
            .ExecuteDeleteAsync(cancellationToken);

        // DopeEntries depend on RangeSessions
        var dopeEntriesDeleted = await _context.DopeEntries
            .Where(d => d.RangeSession.UserId == targetUserId)
            .ExecuteDeleteAsync(cancellationToken);

        // GroupEntries depend on RangeSessions
        var groupEntriesDeleted = await _context.GroupEntries
            .Where(g => g.RangeSession.UserId == targetUserId)
            .ExecuteDeleteAsync(cancellationToken);

        // ChronoSessions depend on RangeSessions
        var chronoSessionsDeleted = await _context.ChronoSessions
            .Where(c => c.RangeSession.UserId == targetUserId)
            .ExecuteDeleteAsync(cancellationToken);

        // Images (already tracked count above)
        await _context.Images
            .Where(i => i.UserId == targetUserId)
            .ExecuteDeleteAsync(cancellationToken);

        // RangeSessions depend on RifleSetups and SavedLocations
        statistics.RangeSessionsDeleted = await _context.RangeSessions
            .Where(s => s.UserId == targetUserId)
            .ExecuteDeleteAsync(cancellationToken);

        // AmmoLots depend on Ammunition
        statistics.AmmoLotsDeleted = await _context.AmmoLots
            .Where(l => l.UserId == targetUserId)
            .ExecuteDeleteAsync(cancellationToken);

        // Now we can delete the independent entities
        statistics.AmmunitionDeleted = await _context.Ammunition
            .Where(a => a.UserId == targetUserId)
            .ExecuteDeleteAsync(cancellationToken);

        statistics.RifleSetupsDeleted = await _context.RifleSetups
            .Where(r => r.UserId == targetUserId)
            .ExecuteDeleteAsync(cancellationToken);

        statistics.SavedLocationsDeleted = await _context.SavedLocations
            .Where(l => l.UserId == targetUserId)
            .ExecuteDeleteAsync(cancellationToken);

        // UserPreferences
        await _context.UserPreferences
            .Where(p => p.UserId == targetUserId)
            .ExecuteDeleteAsync(cancellationToken);

        _logger.LogDebug(
            "Deleted target user data: {Rifles} rifles, {Ammo} ammo, {Sessions} sessions, {Images} images",
            statistics.RifleSetupsDeleted, statistics.AmmunitionDeleted,
            statistics.RangeSessionsDeleted, statistics.ImagesDeleted);
    }

    private async Task CopyUserPreferencesAsync(
        string sourceUserId,
        string targetUserId,
        CloneStatistics statistics,
        CancellationToken cancellationToken)
    {
        var sourcePrefs = await _context.UserPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == sourceUserId, cancellationToken);

        if (sourcePrefs == null)
            return;

        var newPrefs = new UserPreferences
        {
            UserId = targetUserId,
            DistanceUnit = sourcePrefs.DistanceUnit,
            AdjustmentUnit = sourcePrefs.AdjustmentUnit,
            TemperatureUnit = sourcePrefs.TemperatureUnit,
            PressureUnit = sourcePrefs.PressureUnit,
            VelocityUnit = sourcePrefs.VelocityUnit,
            Theme = sourcePrefs.Theme,
            GroupSizeMethod = sourcePrefs.GroupSizeMethod,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.UserPreferences.Add(newPrefs);
        await _context.SaveChangesAsync(cancellationToken);
        statistics.UserPreferencesCopied = true;
    }

    private async Task CopySavedLocationsAsync(
        string sourceUserId,
        string targetUserId,
        IdMappings mappings,
        CloneStatistics statistics,
        CancellationToken cancellationToken)
    {
        var sourceLocations = await _context.SavedLocations
            .AsNoTracking()
            .Where(l => l.UserId == sourceUserId)
            .ToListAsync(cancellationToken);

        foreach (var source in sourceLocations)
        {
            var newLocation = new SavedLocation
            {
                UserId = targetUserId,
                Name = source.Name,
                Latitude = source.Latitude,
                Longitude = source.Longitude,
                Altitude = source.Altitude,
                Description = source.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SavedLocations.Add(newLocation);
            await _context.SaveChangesAsync(cancellationToken);

            mappings.SavedLocations[source.Id] = newLocation.Id;
            statistics.SavedLocationsCopied++;
        }
    }

    private async Task CopyRifleSetupsAsync(
        string sourceUserId,
        string targetUserId,
        IdMappings mappings,
        CloneStatistics statistics,
        CancellationToken cancellationToken)
    {
        var sourceRifles = await _context.RifleSetups
            .AsNoTracking()
            .Where(r => r.UserId == sourceUserId)
            .ToListAsync(cancellationToken);

        foreach (var source in sourceRifles)
        {
            var newRifle = new RifleSetup
            {
                UserId = targetUserId,
                Name = source.Name,
                Manufacturer = source.Manufacturer,
                Model = source.Model,
                Caliber = source.Caliber,
                BarrelLength = source.BarrelLength,
                TwistRate = source.TwistRate,
                ScopeMake = source.ScopeMake,
                ScopeModel = source.ScopeModel,
                ScopeHeight = source.ScopeHeight,
                ZeroDistance = source.ZeroDistance,
                ZeroElevationClicks = source.ZeroElevationClicks,
                ZeroWindageClicks = source.ZeroWindageClicks,
                MuzzleVelocity = source.MuzzleVelocity,
                BallisticCoefficient = source.BallisticCoefficient,
                DragModel = source.DragModel,
                Notes = source.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.RifleSetups.Add(newRifle);
            await _context.SaveChangesAsync(cancellationToken);

            mappings.RifleSetups[source.Id] = newRifle.Id;
            statistics.RifleSetupsCopied++;
        }
    }

    private async Task CopyAmmunitionAsync(
        string sourceUserId,
        string targetUserId,
        IdMappings mappings,
        CloneStatistics statistics,
        CancellationToken cancellationToken)
    {
        var sourceAmmo = await _context.Ammunition
            .AsNoTracking()
            .Where(a => a.UserId == sourceUserId)
            .ToListAsync(cancellationToken);

        foreach (var source in sourceAmmo)
        {
            var newAmmo = new Ammunition
            {
                UserId = targetUserId,
                Manufacturer = source.Manufacturer,
                Name = source.Name,
                Caliber = source.Caliber,
                Grain = source.Grain,
                BulletType = source.BulletType,
                CostPerRound = source.CostPerRound,
                BallisticCoefficient = source.BallisticCoefficient,
                DragModel = source.DragModel,
                Notes = source.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Ammunition.Add(newAmmo);
            await _context.SaveChangesAsync(cancellationToken);

            mappings.Ammunition[source.Id] = newAmmo.Id;
            statistics.AmmunitionCopied++;
        }
    }

    private async Task CopyAmmoLotsAsync(
        string sourceUserId,
        string targetUserId,
        IdMappings mappings,
        CloneStatistics statistics,
        CancellationToken cancellationToken)
    {
        var sourceLots = await _context.AmmoLots
            .AsNoTracking()
            .Where(l => l.UserId == sourceUserId)
            .ToListAsync(cancellationToken);

        foreach (var source in sourceLots)
        {
            if (!mappings.Ammunition.TryGetValue(source.AmmunitionId, out var newAmmoId))
            {
                _logger.LogWarning("Skipping AmmoLot {LotId} - parent Ammunition {AmmoId} not mapped", source.Id, source.AmmunitionId);
                continue;
            }

            var newLot = new AmmoLot
            {
                UserId = targetUserId,
                AmmunitionId = newAmmoId,
                LotNumber = source.LotNumber,
                PurchaseDate = source.PurchaseDate,
                InitialQuantity = source.InitialQuantity,
                PurchasePrice = source.PurchasePrice,
                Notes = source.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.AmmoLots.Add(newLot);
            await _context.SaveChangesAsync(cancellationToken);

            mappings.AmmoLots[source.Id] = newLot.Id;
            statistics.AmmoLotsCopied++;
        }
    }

    private async Task CopyRangeSessionsAsync(
        string sourceUserId,
        string targetUserId,
        IdMappings mappings,
        CloneStatistics statistics,
        CancellationToken cancellationToken)
    {
        var sourceSessions = await _context.RangeSessions
            .AsNoTracking()
            .Where(s => s.UserId == sourceUserId)
            .ToListAsync(cancellationToken);

        foreach (var source in sourceSessions)
        {
            if (!mappings.RifleSetups.TryGetValue(source.RifleSetupId, out var newRifleId))
            {
                _logger.LogWarning("Skipping RangeSession {SessionId} - RifleSetup {RifleId} not mapped", source.Id, source.RifleSetupId);
                continue;
            }

            int? newLocationId = null;
            if (source.SavedLocationId.HasValue)
            {
                mappings.SavedLocations.TryGetValue(source.SavedLocationId.Value, out var mappedLocationId);
                newLocationId = mappedLocationId > 0 ? mappedLocationId : null;
            }

            var newSession = new RangeSession
            {
                UserId = targetUserId,
                RifleSetupId = newRifleId,
                SavedLocationId = newLocationId,
                SessionDate = source.SessionDate,
                SessionTime = source.SessionTime,
                Latitude = source.Latitude,
                Longitude = source.Longitude,
                LocationName = source.LocationName,
                Temperature = source.Temperature,
                Humidity = source.Humidity,
                WindSpeed = source.WindSpeed,
                WindDirection = source.WindDirection,
                Pressure = source.Pressure,
                DensityAltitude = source.DensityAltitude,
                Notes = source.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.RangeSessions.Add(newSession);
            await _context.SaveChangesAsync(cancellationToken);

            mappings.RangeSessions[source.Id] = newSession.Id;
            statistics.RangeSessionsCopied++;
        }
    }

    private async Task CopyChronoSessionsAsync(
        string sourceUserId,
        string targetUserId,
        IdMappings mappings,
        CloneStatistics statistics,
        CancellationToken cancellationToken)
    {
        var sourceChronos = await _context.ChronoSessions
            .AsNoTracking()
            .Where(c => c.RangeSession.UserId == sourceUserId)
            .ToListAsync(cancellationToken);

        foreach (var source in sourceChronos)
        {
            if (!mappings.RangeSessions.TryGetValue(source.RangeSessionId, out var newSessionId))
            {
                _logger.LogWarning("Skipping ChronoSession {ChronoId} - RangeSession {SessionId} not mapped", source.Id, source.RangeSessionId);
                continue;
            }

            if (!mappings.Ammunition.TryGetValue(source.AmmunitionId, out var newAmmoId))
            {
                _logger.LogWarning("Skipping ChronoSession {ChronoId} - Ammunition {AmmoId} not mapped", source.Id, source.AmmunitionId);
                continue;
            }

            int? newLotId = null;
            if (source.AmmoLotId.HasValue && mappings.AmmoLots.TryGetValue(source.AmmoLotId.Value, out var mappedLotId))
            {
                newLotId = mappedLotId;
            }

            var newChrono = new ChronoSession
            {
                RangeSessionId = newSessionId,
                AmmunitionId = newAmmoId,
                AmmoLotId = newLotId,
                BarrelTemperature = source.BarrelTemperature,
                NumberOfRounds = source.NumberOfRounds,
                AverageVelocity = source.AverageVelocity,
                HighVelocity = source.HighVelocity,
                LowVelocity = source.LowVelocity,
                StandardDeviation = source.StandardDeviation,
                ExtremeSpread = source.ExtremeSpread,
                Notes = source.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ChronoSessions.Add(newChrono);
            await _context.SaveChangesAsync(cancellationToken);

            mappings.ChronoSessions[source.Id] = newChrono.Id;
            statistics.ChronoSessionsCopied++;
        }
    }

    private async Task CopyDopeEntriesAsync(
        string sourceUserId,
        string targetUserId,
        IdMappings mappings,
        CloneStatistics statistics,
        CancellationToken cancellationToken)
    {
        var sourceEntries = await _context.DopeEntries
            .AsNoTracking()
            .Where(d => d.RangeSession.UserId == sourceUserId)
            .ToListAsync(cancellationToken);

        foreach (var source in sourceEntries)
        {
            if (!mappings.RangeSessions.TryGetValue(source.RangeSessionId, out var newSessionId))
                continue;

            int? newAmmoId = null;
            if (source.AmmunitionId.HasValue && mappings.Ammunition.TryGetValue(source.AmmunitionId.Value, out var mappedAmmoId))
            {
                newAmmoId = mappedAmmoId;
            }

            int? newLotId = null;
            if (source.AmmoLotId.HasValue && mappings.AmmoLots.TryGetValue(source.AmmoLotId.Value, out var mappedLotId))
            {
                newLotId = mappedLotId;
            }

            var newEntry = new DopeEntry
            {
                RangeSessionId = newSessionId,
                AmmunitionId = newAmmoId,
                AmmoLotId = newLotId,
                Distance = source.Distance,
                ElevationMils = source.ElevationMils,
                WindageMils = source.WindageMils,
                Notes = source.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.DopeEntries.Add(newEntry);
            statistics.DopeEntriesCopied++;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task CopyGroupEntriesAsync(
        string sourceUserId,
        string targetUserId,
        IdMappings mappings,
        CloneStatistics statistics,
        CancellationToken cancellationToken)
    {
        var sourceEntries = await _context.GroupEntries
            .AsNoTracking()
            .Where(g => g.RangeSession.UserId == sourceUserId)
            .ToListAsync(cancellationToken);

        foreach (var source in sourceEntries)
        {
            if (!mappings.RangeSessions.TryGetValue(source.RangeSessionId, out var newSessionId))
                continue;

            int? newAmmoId = null;
            if (source.AmmunitionId.HasValue && mappings.Ammunition.TryGetValue(source.AmmunitionId.Value, out var mappedAmmoId))
            {
                newAmmoId = mappedAmmoId;
            }

            int? newLotId = null;
            if (source.AmmoLotId.HasValue && mappings.AmmoLots.TryGetValue(source.AmmoLotId.Value, out var mappedLotId))
            {
                newLotId = mappedLotId;
            }

            var newEntry = new GroupEntry
            {
                RangeSessionId = newSessionId,
                AmmunitionId = newAmmoId,
                AmmoLotId = newLotId,
                GroupNumber = source.GroupNumber,
                Distance = source.Distance,
                NumberOfShots = source.NumberOfShots,
                GroupSizeMoa = source.GroupSizeMoa,
                MeanRadiusMoa = source.MeanRadiusMoa,
                Notes = source.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.GroupEntries.Add(newEntry);
            await _context.SaveChangesAsync(cancellationToken);

            mappings.GroupEntries[source.Id] = newEntry.Id;
            statistics.GroupEntriesCopied++;
        }
    }

    private async Task CopyVelocityReadingsAsync(
        IdMappings mappings,
        CloneStatistics statistics,
        CancellationToken cancellationToken)
    {
        // Get all velocity readings for the mapped chrono sessions
        var sourceChronoIds = mappings.ChronoSessions.Keys.ToList();
        var sourceReadings = await _context.VelocityReadings
            .AsNoTracking()
            .Where(v => sourceChronoIds.Contains(v.ChronoSessionId))
            .ToListAsync(cancellationToken);

        foreach (var source in sourceReadings)
        {
            if (!mappings.ChronoSessions.TryGetValue(source.ChronoSessionId, out var newChronoId))
                continue;

            var newReading = new VelocityReading
            {
                ChronoSessionId = newChronoId,
                ShotNumber = source.ShotNumber,
                Velocity = source.Velocity,
                CreatedAt = DateTime.UtcNow
            };

            _context.VelocityReadings.Add(newReading);
            statistics.VelocityReadingsCopied++;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task CopyImagesAsync(
        string sourceUserId,
        string targetUserId,
        IdMappings mappings,
        CloneStatistics statistics,
        List<string> copiedMinioFiles,
        CancellationToken cancellationToken)
    {
        var sourceImages = await _context.Images
            .AsNoTracking()
            .Where(i => i.UserId == sourceUserId)
            .ToListAsync(cancellationToken);

        foreach (var source in sourceImages)
        {
            // Map parent IDs
            int? newRifleSetupId = null;
            int? newRangeSessionId = null;
            int? newGroupEntryId = null;

            if (source.RifleSetupId.HasValue)
            {
                if (!mappings.RifleSetups.TryGetValue(source.RifleSetupId.Value, out var mappedId))
                    continue;
                newRifleSetupId = mappedId;
            }

            if (source.RangeSessionId.HasValue)
            {
                if (!mappings.RangeSessions.TryGetValue(source.RangeSessionId.Value, out var mappedId))
                    continue;
                newRangeSessionId = mappedId;
            }

            if (source.GroupEntryId.HasValue)
            {
                if (!mappings.GroupEntries.TryGetValue(source.GroupEntryId.Value, out var mappedId))
                    continue;
                newGroupEntryId = mappedId;
            }

            // Generate new file names for MinIO
            var newGuid = Guid.NewGuid();
            var extension = Path.GetExtension(source.FileName);
            var parentType = source.RifleSetupId.HasValue ? "rifles"
                : source.RangeSessionId.HasValue ? "sessions"
                : "groups";
            var parentId = newRifleSetupId ?? newRangeSessionId ?? newGroupEntryId;

            var newFileName = $"{targetUserId}/{parentType}/{parentId}/{newGuid}{extension}";
            string? newThumbnailFileName = null;

            if (!string.IsNullOrEmpty(source.ThumbnailFileName))
            {
                newThumbnailFileName = $"{targetUserId}/{parentType}/{parentId}/{newGuid}_thumb.jpg";
            }

            // Copy files in MinIO
            try
            {
                var sourceStream = await _storageService.GetFileAsync(ImageBucket, source.FileName);
                if (sourceStream != null)
                {
                    await _storageService.UploadFileAsync(ImageBucket, newFileName, sourceStream, source.ContentType);
                    copiedMinioFiles.Add(newFileName);
                    statistics.ImageBytesCopied += source.FileSize;
                }

                if (!string.IsNullOrEmpty(source.ThumbnailFileName) && newThumbnailFileName != null)
                {
                    var thumbStream = await _storageService.GetFileAsync(ImageBucket, source.ThumbnailFileName);
                    if (thumbStream != null)
                    {
                        await _storageService.UploadFileAsync(ImageBucket, newThumbnailFileName, thumbStream, "image/jpeg");
                        copiedMinioFiles.Add(newThumbnailFileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to copy image from MinIO: {FileName}", source.FileName);
                continue;
            }

            // Create new Image entity
            var newImage = new Image
            {
                UserId = targetUserId,
                FileName = newFileName,
                OriginalFileName = source.OriginalFileName,
                ContentType = source.ContentType,
                FileSize = source.FileSize,
                ThumbnailFileName = newThumbnailFileName,
                Caption = source.Caption,
                DisplayOrder = source.DisplayOrder,
                IsProcessed = source.IsProcessed,
                RifleSetupId = newRifleSetupId,
                RangeSessionId = newRangeSessionId,
                GroupEntryId = newGroupEntryId,
                UploadedAt = DateTime.UtcNow
            };

            _context.Images.Add(newImage);
            await _context.SaveChangesAsync(cancellationToken);

            mappings.Images[source.Id] = newImage.Id;
            statistics.ImagesCopied++;
        }
    }

    private async Task CopyGroupMeasurementsAsync(
        IdMappings mappings,
        CloneStatistics statistics,
        CancellationToken cancellationToken)
    {
        // Get all group measurements for the mapped group entries
        var sourceGroupEntryIds = mappings.GroupEntries.Keys.ToList();
        var sourceMeasurements = await _context.GroupMeasurements
            .AsNoTracking()
            .Where(m => sourceGroupEntryIds.Contains(m.GroupEntryId))
            .ToListAsync(cancellationToken);

        foreach (var source in sourceMeasurements)
        {
            if (!mappings.GroupEntries.TryGetValue(source.GroupEntryId, out var newGroupEntryId))
                continue;

            int? newOriginalImageId = null;
            if (source.OriginalImageId.HasValue && mappings.Images.TryGetValue(source.OriginalImageId.Value, out var mappedOrigId))
            {
                newOriginalImageId = mappedOrigId;
            }

            int? newAnnotatedImageId = null;
            if (source.AnnotatedImageId.HasValue && mappings.Images.TryGetValue(source.AnnotatedImageId.Value, out var mappedAnnotId))
            {
                newAnnotatedImageId = mappedAnnotId;
            }

            var newMeasurement = new GroupMeasurement
            {
                GroupEntryId = newGroupEntryId,
                HolePositionsJson = source.HolePositionsJson,
                BulletDiameter = source.BulletDiameter,
                ExtremeSpreadCtc = source.ExtremeSpreadCtc,
                ExtremeSpreadEte = source.ExtremeSpreadEte,
                MeanRadius = source.MeanRadius,
                HorizontalSpreadCtc = source.HorizontalSpreadCtc,
                HorizontalSpreadEte = source.HorizontalSpreadEte,
                VerticalSpreadCtc = source.VerticalSpreadCtc,
                VerticalSpreadEte = source.VerticalSpreadEte,
                RadialStdDev = source.RadialStdDev,
                HorizontalStdDev = source.HorizontalStdDev,
                VerticalStdDev = source.VerticalStdDev,
                Cep50 = source.Cep50,
                PoiOffsetX = source.PoiOffsetX,
                PoiOffsetY = source.PoiOffsetY,
                CalibrationMethod = source.CalibrationMethod,
                MeasurementConfidence = source.MeasurementConfidence,
                OriginalImageId = newOriginalImageId,
                AnnotatedImageId = newAnnotatedImageId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.GroupMeasurements.Add(newMeasurement);
            statistics.GroupMeasurementsCopied++;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Helper class to track old ID -> new ID mappings during clone
    /// </summary>
    private class IdMappings
    {
        public Dictionary<int, int> SavedLocations { get; } = new();
        public Dictionary<int, int> RifleSetups { get; } = new();
        public Dictionary<int, int> Ammunition { get; } = new();
        public Dictionary<int, int> AmmoLots { get; } = new();
        public Dictionary<int, int> RangeSessions { get; } = new();
        public Dictionary<int, int> ChronoSessions { get; } = new();
        public Dictionary<int, int> GroupEntries { get; } = new();
        public Dictionary<int, int> Images { get; } = new();
    }
}
