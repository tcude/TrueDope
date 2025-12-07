using Microsoft.EntityFrameworkCore;
using TrueDope.Api.Data;
using TrueDope.Api.Data.Entities;
using TrueDope.Api.DTOs;
using TrueDope.Api.DTOs.Ammunition;

namespace TrueDope.Api.Services;

public class AmmoService : IAmmoService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AmmoService> _logger;

    public AmmoService(ApplicationDbContext context, ILogger<AmmoService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PaginatedResponse<AmmoListDto>> GetAmmoAsync(string userId, AmmoFilterDto filter)
    {
        var query = _context.Ammunition
            .Include(a => a.AmmoLots)
            .Include(a => a.ChronoSessions)
            .Include(a => a.GroupEntries)
            .Where(a => a.UserId == userId);

        // Apply search
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(a =>
                a.Name.ToLower().Contains(search) ||
                a.Manufacturer.ToLower().Contains(search) ||
                a.Caliber.ToLower().Contains(search));
        }

        // Apply caliber filter
        if (!string.IsNullOrWhiteSpace(filter.Caliber))
        {
            query = query.Where(a => a.Caliber == filter.Caliber);
        }

        // Count total
        var totalItems = await query.CountAsync();

        // Apply sorting
        query = filter.SortBy.ToLowerInvariant() switch
        {
            "name" => filter.SortDesc
                ? query.OrderByDescending(a => a.Name)
                : query.OrderBy(a => a.Name),
            "manufacturer" => filter.SortDesc
                ? query.OrderByDescending(a => a.Manufacturer)
                : query.OrderBy(a => a.Manufacturer),
            "caliber" => filter.SortDesc
                ? query.OrderByDescending(a => a.Caliber)
                : query.OrderBy(a => a.Caliber),
            "grain" => filter.SortDesc
                ? query.OrderByDescending(a => a.Grain)
                : query.OrderBy(a => a.Grain),
            "createdat" => filter.SortDesc
                ? query.OrderByDescending(a => a.CreatedAt)
                : query.OrderBy(a => a.CreatedAt),
            _ => query.OrderBy(a => a.Manufacturer).ThenBy(a => a.Name)
        };

        // Apply pagination
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var page = Math.Max(filter.Page, 1);

        var ammoList = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AmmoListDto
            {
                Id = a.Id,
                Manufacturer = a.Manufacturer,
                Name = a.Name,
                Caliber = a.Caliber,
                Grain = a.Grain,
                BulletType = a.BulletType,
                CostPerRound = a.CostPerRound,
                DisplayName = a.DisplayName,
                LotCount = a.AmmoLots.Count,
                SessionCount = a.ChronoSessions.Count + a.GroupEntries.Count,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return new PaginatedResponse<AmmoListDto>
        {
            Items = ammoList,
            Pagination = new PaginationInfo
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems
            }
        };
    }

    public async Task<AmmoDetailDto?> GetAmmoAsync(string userId, int ammoId)
    {
        var ammo = await _context.Ammunition
            .Include(a => a.AmmoLots)
                .ThenInclude(l => l.ChronoSessions)
            .FirstOrDefaultAsync(a => a.Id == ammoId && a.UserId == userId);

        if (ammo == null)
            return null;

        return new AmmoDetailDto
        {
            Id = ammo.Id,
            Manufacturer = ammo.Manufacturer,
            Name = ammo.Name,
            Caliber = ammo.Caliber,
            Grain = ammo.Grain,
            BulletType = ammo.BulletType,
            CostPerRound = ammo.CostPerRound,
            BallisticCoefficient = ammo.BallisticCoefficient,
            DragModel = ammo.DragModel,
            Notes = ammo.Notes,
            DisplayName = ammo.DisplayName,
            Lots = ammo.AmmoLots.Select(l => new AmmoLotDto
            {
                Id = l.Id,
                LotNumber = l.LotNumber,
                PurchaseDate = l.PurchaseDate,
                InitialQuantity = l.InitialQuantity,
                PurchasePrice = l.PurchasePrice,
                CostPerRound = l.CostPerRound,
                Notes = l.Notes,
                DisplayName = l.DisplayName,
                SessionCount = l.ChronoSessions.Count,
                CreatedAt = l.CreatedAt
            }).ToList(),
            CreatedAt = ammo.CreatedAt,
            UpdatedAt = ammo.UpdatedAt
        };
    }

    public async Task<int> CreateAmmoAsync(string userId, CreateAmmoDto dto)
    {
        var ammo = new Ammunition
        {
            UserId = userId,
            Manufacturer = dto.Manufacturer,
            Name = dto.Name,
            Caliber = dto.Caliber,
            Grain = dto.Grain,
            BulletType = dto.BulletType,
            CostPerRound = dto.CostPerRound,
            BallisticCoefficient = dto.BallisticCoefficient,
            DragModel = dto.DragModel,
            Notes = dto.Notes
        };

        _context.Ammunition.Add(ammo);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created ammunition {AmmoId} '{AmmoName}' for user {UserId}", ammo.Id, ammo.DisplayName, userId);

        return ammo.Id;
    }

    public async Task<bool> UpdateAmmoAsync(string userId, int ammoId, UpdateAmmoDto dto)
    {
        var ammo = await _context.Ammunition
            .FirstOrDefaultAsync(a => a.Id == ammoId && a.UserId == userId);

        if (ammo == null)
            return false;

        if (dto.Manufacturer != null) ammo.Manufacturer = dto.Manufacturer;
        if (dto.Name != null) ammo.Name = dto.Name;
        if (dto.Caliber != null) ammo.Caliber = dto.Caliber;
        if (dto.Grain.HasValue) ammo.Grain = dto.Grain.Value;
        if (dto.BulletType != null) ammo.BulletType = dto.BulletType;
        if (dto.CostPerRound.HasValue) ammo.CostPerRound = dto.CostPerRound;
        if (dto.BallisticCoefficient.HasValue) ammo.BallisticCoefficient = dto.BallisticCoefficient;
        if (dto.DragModel != null) ammo.DragModel = dto.DragModel;
        if (dto.Notes != null) ammo.Notes = dto.Notes;

        ammo.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAmmoAsync(string userId, int ammoId)
    {
        var ammo = await _context.Ammunition
            .FirstOrDefaultAsync(a => a.Id == ammoId && a.UserId == userId);

        if (ammo == null)
            return false;

        // Check if ammo has sessions (Restrict behavior)
        var hasSessions = await HasSessionsAsync(userId, ammoId);
        if (hasSessions)
            throw new InvalidOperationException("Cannot delete ammunition with existing sessions");

        _context.Ammunition.Remove(ammo);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted ammunition {AmmoId} for user {UserId}", ammoId, userId);

        return true;
    }

    public async Task<bool> HasSessionsAsync(string userId, int ammoId)
    {
        return await _context.ChronoSessions
            .AnyAsync(c => c.AmmunitionId == ammoId && c.RangeSession.UserId == userId);
    }

    // ==================== Lots ====================

    public async Task<List<AmmoLotDto>> GetLotsAsync(string userId, int ammoId)
    {
        var lots = await _context.AmmoLots
            .Include(l => l.ChronoSessions)
            .Where(l => l.AmmunitionId == ammoId && l.UserId == userId)
            .OrderBy(l => l.LotNumber)
            .Select(l => new AmmoLotDto
            {
                Id = l.Id,
                LotNumber = l.LotNumber,
                PurchaseDate = l.PurchaseDate,
                InitialQuantity = l.InitialQuantity,
                PurchasePrice = l.PurchasePrice,
                CostPerRound = l.CostPerRound,
                Notes = l.Notes,
                DisplayName = l.DisplayName,
                SessionCount = l.ChronoSessions.Count,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        return lots;
    }

    public async Task<int> CreateLotAsync(string userId, int ammoId, CreateAmmoLotDto dto)
    {
        // Verify ammo belongs to user
        var ammoExists = await _context.Ammunition
            .AnyAsync(a => a.Id == ammoId && a.UserId == userId);

        if (!ammoExists)
            throw new ArgumentException("Ammunition not found");

        var lot = new AmmoLot
        {
            AmmunitionId = ammoId,
            UserId = userId,
            LotNumber = dto.LotNumber,
            PurchaseDate = dto.PurchaseDate.HasValue
                ? DateTime.SpecifyKind(dto.PurchaseDate.Value, DateTimeKind.Utc)
                : null,
            InitialQuantity = dto.InitialQuantity,
            PurchasePrice = dto.PurchasePrice,
            Notes = dto.Notes
        };

        _context.AmmoLots.Add(lot);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created ammo lot {LotId} '{LotNumber}' for ammo {AmmoId}", lot.Id, lot.LotNumber, ammoId);

        return lot.Id;
    }

    public async Task<bool> UpdateLotAsync(string userId, int ammoId, int lotId, UpdateAmmoLotDto dto)
    {
        var lot = await _context.AmmoLots
            .FirstOrDefaultAsync(l => l.Id == lotId && l.AmmunitionId == ammoId && l.UserId == userId);

        if (lot == null)
            return false;

        if (dto.LotNumber != null) lot.LotNumber = dto.LotNumber;
        if (dto.PurchaseDate.HasValue) lot.PurchaseDate = DateTime.SpecifyKind(dto.PurchaseDate.Value, DateTimeKind.Utc);
        if (dto.InitialQuantity.HasValue) lot.InitialQuantity = dto.InitialQuantity;
        if (dto.PurchasePrice.HasValue) lot.PurchasePrice = dto.PurchasePrice;
        if (dto.Notes != null) lot.Notes = dto.Notes;

        lot.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteLotAsync(string userId, int ammoId, int lotId)
    {
        var lot = await _context.AmmoLots
            .FirstOrDefaultAsync(l => l.Id == lotId && l.AmmunitionId == ammoId && l.UserId == userId);

        if (lot == null)
            return false;

        // Check if lot has sessions (Restrict behavior)
        var hasSessions = await LotHasSessionsAsync(userId, lotId);
        if (hasSessions)
            throw new InvalidOperationException("Cannot delete lot with existing sessions");

        _context.AmmoLots.Remove(lot);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted ammo lot {LotId} for user {UserId}", lotId, userId);

        return true;
    }

    public async Task<bool> LotHasSessionsAsync(string userId, int lotId)
    {
        return await _context.ChronoSessions
            .AnyAsync(c => c.AmmoLotId == lotId && c.RangeSession.UserId == userId);
    }
}
