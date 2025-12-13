using Microsoft.EntityFrameworkCore;
using TrueDope.Api.Data;
using TrueDope.Api.Data.Entities;
using TrueDope.Api.DTOs;
using TrueDope.Api.DTOs.Rifles;

namespace TrueDope.Api.Services;

public class RifleService : IRifleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RifleService> _logger;

    public RifleService(ApplicationDbContext context, ILogger<RifleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PaginatedResponse<RifleListDto>> GetRiflesAsync(string userId, RifleFilterDto filter)
    {
        var query = _context.RifleSetups
            .Include(r => r.RangeSessions)
            .Include(r => r.Images)
            .Where(r => r.UserId == userId);

        // Apply search
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(r =>
                r.Name.ToLower().Contains(search) ||
                r.Caliber.ToLower().Contains(search) ||
                (r.Manufacturer != null && r.Manufacturer.ToLower().Contains(search)) ||
                (r.Model != null && r.Model.ToLower().Contains(search)));
        }

        // Count total
        var totalItems = await query.CountAsync();

        // Apply sorting
        query = filter.SortBy.ToLowerInvariant() switch
        {
            "name" => filter.SortDesc
                ? query.OrderByDescending(r => r.Name)
                : query.OrderBy(r => r.Name),
            "caliber" => filter.SortDesc
                ? query.OrderByDescending(r => r.Caliber)
                : query.OrderBy(r => r.Caliber),
            "createdat" => filter.SortDesc
                ? query.OrderByDescending(r => r.CreatedAt)
                : query.OrderBy(r => r.CreatedAt),
            "lastsession" => filter.SortDesc
                ? query.OrderByDescending(r => r.RangeSessions.Max(s => s.SessionDate))
                : query.OrderBy(r => r.RangeSessions.Max(s => s.SessionDate)),
            _ => query.OrderBy(r => r.Name)
        };

        // Apply pagination
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var page = Math.Max(filter.Page, 1);

        var rifles = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RifleListDto
            {
                Id = r.Id,
                Name = r.Name,
                Manufacturer = r.Manufacturer,
                Model = r.Model,
                Caliber = r.Caliber,
                ZeroDistance = r.ZeroDistance,
                SessionCount = r.RangeSessions.Count,
                ImageCount = r.Images.Count,
                LastSessionDate = r.RangeSessions.Any()
                    ? r.RangeSessions.Max(s => s.SessionDate)
                    : null,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return new PaginatedResponse<RifleListDto>
        {
            Items = rifles,
            Pagination = new PaginationInfo
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems
            }
        };
    }

    public async Task<RifleDetailDto?> GetRifleAsync(string userId, int rifleId)
    {
        var rifle = await _context.RifleSetups
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == rifleId && r.UserId == userId);

        if (rifle == null)
            return null;

        return new RifleDetailDto
        {
            Id = rifle.Id,
            Name = rifle.Name,
            Manufacturer = rifle.Manufacturer,
            Model = rifle.Model,
            Caliber = rifle.Caliber,
            BarrelLength = rifle.BarrelLength,
            TwistRate = rifle.TwistRate,
            ScopeMake = rifle.ScopeMake,
            ScopeModel = rifle.ScopeModel,
            ScopeHeight = rifle.ScopeHeight,
            ZeroDistance = rifle.ZeroDistance,
            ZeroElevationClicks = rifle.ZeroElevationClicks,
            ZeroWindageClicks = rifle.ZeroWindageClicks,
            MuzzleVelocity = rifle.MuzzleVelocity,
            BallisticCoefficient = rifle.BallisticCoefficient,
            DragModel = rifle.DragModel,
            Notes = rifle.Notes,
            Images = rifle.Images.Select(i => new RifleImageDto
            {
                Id = i.Id,
                Url = $"/api/images/{i.Id}",
                ThumbnailUrl = $"/api/images/{i.Id}/thumbnail",
                Caption = i.Caption
            }).ToList(),
            CreatedAt = rifle.CreatedAt,
            UpdatedAt = rifle.UpdatedAt
        };
    }

    public async Task<int> CreateRifleAsync(string userId, CreateRifleDto dto)
    {
        var rifle = new RifleSetup
        {
            UserId = userId,
            Name = dto.Name,
            Manufacturer = dto.Manufacturer,
            Model = dto.Model,
            Caliber = dto.Caliber,
            BarrelLength = dto.BarrelLength,
            TwistRate = dto.TwistRate,
            ScopeMake = dto.ScopeMake,
            ScopeModel = dto.ScopeModel,
            ScopeHeight = dto.ScopeHeight,
            ZeroDistance = dto.ZeroDistance,
            ZeroElevationClicks = dto.ZeroElevationClicks,
            ZeroWindageClicks = dto.ZeroWindageClicks,
            MuzzleVelocity = dto.MuzzleVelocity,
            BallisticCoefficient = dto.BallisticCoefficient,
            DragModel = dto.DragModel,
            Notes = dto.Notes
        };

        _context.RifleSetups.Add(rifle);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created rifle {RifleId} '{RifleName}' for user {UserId}", rifle.Id, rifle.Name, userId);

        return rifle.Id;
    }

    public async Task<bool> UpdateRifleAsync(string userId, int rifleId, UpdateRifleDto dto)
    {
        var rifle = await _context.RifleSetups
            .FirstOrDefaultAsync(r => r.Id == rifleId && r.UserId == userId);

        if (rifle == null)
            return false;

        if (dto.Name != null) rifle.Name = dto.Name;
        if (dto.Manufacturer != null) rifle.Manufacturer = dto.Manufacturer;
        if (dto.Model != null) rifle.Model = dto.Model;
        if (dto.Caliber != null) rifle.Caliber = dto.Caliber;
        if (dto.BarrelLength.HasValue) rifle.BarrelLength = dto.BarrelLength;
        if (dto.TwistRate != null) rifle.TwistRate = dto.TwistRate;
        if (dto.ScopeMake != null) rifle.ScopeMake = dto.ScopeMake;
        if (dto.ScopeModel != null) rifle.ScopeModel = dto.ScopeModel;
        if (dto.ScopeHeight.HasValue) rifle.ScopeHeight = dto.ScopeHeight;
        if (dto.ZeroDistance.HasValue) rifle.ZeroDistance = dto.ZeroDistance.Value;
        if (dto.ZeroElevationClicks.HasValue) rifle.ZeroElevationClicks = dto.ZeroElevationClicks;
        if (dto.ZeroWindageClicks.HasValue) rifle.ZeroWindageClicks = dto.ZeroWindageClicks;
        if (dto.MuzzleVelocity.HasValue) rifle.MuzzleVelocity = dto.MuzzleVelocity;
        if (dto.BallisticCoefficient.HasValue) rifle.BallisticCoefficient = dto.BallisticCoefficient;
        if (dto.DragModel != null) rifle.DragModel = dto.DragModel;
        if (dto.Notes != null) rifle.Notes = dto.Notes;

        rifle.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteRifleAsync(string userId, int rifleId)
    {
        var rifle = await _context.RifleSetups
            .FirstOrDefaultAsync(r => r.Id == rifleId && r.UserId == userId);

        if (rifle == null)
            return false;

        // Check if rifle has sessions (will fail due to Restrict)
        var hasSessions = await _context.RangeSessions
            .AnyAsync(s => s.RifleSetupId == rifleId);

        if (hasSessions)
            throw new InvalidOperationException("Cannot delete rifle with existing sessions");

        _context.RifleSetups.Remove(rifle);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted rifle {RifleId} for user {UserId}", rifleId, userId);

        return true;
    }

    public async Task<bool> HasSessionsAsync(string userId, int rifleId)
    {
        return await _context.RangeSessions
            .AnyAsync(s => s.RifleSetupId == rifleId && s.UserId == userId);
    }
}
