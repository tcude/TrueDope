using Microsoft.EntityFrameworkCore;
using TrueDope.Api.Data;
using TrueDope.Api.Data.Entities;
using TrueDope.Api.DTOs.Locations;
using TrueDope.Api.DTOs.SharedLocations;

namespace TrueDope.Api.Services;

public class SharedLocationService : ISharedLocationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SharedLocationService> _logger;

    public SharedLocationService(
        ApplicationDbContext context,
        ILogger<SharedLocationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SharedLocationListDto>> GetActiveLocationsAsync(string? search = null, string? state = null)
    {
        var query = _context.SharedLocations
            .Where(l => l.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(l =>
                l.Name.ToLower().Contains(searchLower) ||
                (l.City != null && l.City.ToLower().Contains(searchLower)) ||
                (l.Description != null && l.Description.ToLower().Contains(searchLower)));
        }

        if (!string.IsNullOrWhiteSpace(state))
        {
            query = query.Where(l => l.State == state);
        }

        return await query
            .OrderBy(l => l.Name)
            .Select(l => new SharedLocationListDto
            {
                Id = l.Id,
                Name = l.Name,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                Altitude = l.Altitude,
                Description = l.Description,
                City = l.City,
                State = l.State,
                Country = l.Country,
                Website = l.Website,
                PhoneNumber = l.PhoneNumber
            })
            .ToListAsync();
    }

    public async Task<List<SharedLocationAdminDto>> GetAllLocationsAsync(bool includeInactive = true)
    {
        var query = _context.SharedLocations.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(l => l.IsActive);
        }

        return await query
            .OrderBy(l => l.Name)
            .Select(l => new SharedLocationAdminDto
            {
                Id = l.Id,
                Name = l.Name,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                Altitude = l.Altitude,
                Description = l.Description,
                City = l.City,
                State = l.State,
                Country = l.Country,
                Website = l.Website,
                PhoneNumber = l.PhoneNumber,
                IsActive = l.IsActive,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt,
                CreatedByUserId = l.CreatedByUserId
            })
            .ToListAsync();
    }

    public async Task<SharedLocationAdminDto?> GetByIdAsync(int id)
    {
        return await _context.SharedLocations
            .Where(l => l.Id == id)
            .Select(l => new SharedLocationAdminDto
            {
                Id = l.Id,
                Name = l.Name,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                Altitude = l.Altitude,
                Description = l.Description,
                City = l.City,
                State = l.State,
                Country = l.Country,
                Website = l.Website,
                PhoneNumber = l.PhoneNumber,
                IsActive = l.IsActive,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt,
                CreatedByUserId = l.CreatedByUserId
            })
            .FirstOrDefaultAsync();
    }

    public async Task<int> CreateAsync(string userId, CreateSharedLocationDto dto)
    {
        var location = new SharedLocation
        {
            Name = dto.Name,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Altitude = dto.Altitude,
            Description = dto.Description,
            City = dto.City,
            State = dto.State,
            Country = dto.Country,
            Website = dto.Website,
            PhoneNumber = dto.PhoneNumber,
            IsActive = true,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SharedLocations.Add(location);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created shared location '{Name}' (ID: {Id}) by user {UserId}",
            location.Name, location.Id, userId);

        return location.Id;
    }

    public async Task UpdateAsync(int id, UpdateSharedLocationDto dto)
    {
        var location = await _context.SharedLocations.FindAsync(id);

        if (location == null)
        {
            throw new KeyNotFoundException($"Shared location {id} not found");
        }

        if (dto.Name != null) location.Name = dto.Name;
        if (dto.Latitude.HasValue) location.Latitude = dto.Latitude.Value;
        if (dto.Longitude.HasValue) location.Longitude = dto.Longitude.Value;
        if (dto.Altitude.HasValue) location.Altitude = dto.Altitude;
        if (dto.Description != null) location.Description = dto.Description;
        if (dto.City != null) location.City = dto.City;
        if (dto.State != null) location.State = dto.State;
        if (dto.Country != null) location.Country = dto.Country;
        if (dto.Website != null) location.Website = dto.Website;
        if (dto.PhoneNumber != null) location.PhoneNumber = dto.PhoneNumber;
        if (dto.IsActive.HasValue) location.IsActive = dto.IsActive.Value;

        location.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated shared location '{Name}' (ID: {Id})", location.Name, location.Id);
    }

    public async Task DeleteAsync(int id)
    {
        var location = await _context.SharedLocations.FindAsync(id);

        if (location == null)
        {
            throw new KeyNotFoundException($"Shared location {id} not found");
        }

        _context.SharedLocations.Remove(location);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted shared location '{Name}' (ID: {Id})", location.Name, id);
    }

    public async Task<LocationDetailDto> CopyToSavedAsync(int sharedLocationId, string userId)
    {
        var sharedLocation = await _context.SharedLocations.FindAsync(sharedLocationId);

        if (sharedLocation == null || !sharedLocation.IsActive)
        {
            throw new KeyNotFoundException($"Shared location {sharedLocationId} not found");
        }

        // Create a new saved location for the user
        var savedLocation = new SavedLocation
        {
            UserId = userId,
            Name = sharedLocation.Name,
            Latitude = sharedLocation.Latitude,
            Longitude = sharedLocation.Longitude,
            Altitude = sharedLocation.Altitude,
            Description = sharedLocation.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SavedLocations.Add(savedLocation);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Copied shared location '{Name}' (ID: {SharedId}) to user {UserId} as SavedLocation (ID: {SavedId})",
            sharedLocation.Name, sharedLocationId, userId, savedLocation.Id);

        return new LocationDetailDto
        {
            Id = savedLocation.Id,
            Name = savedLocation.Name,
            Latitude = savedLocation.Latitude,
            Longitude = savedLocation.Longitude,
            Altitude = savedLocation.Altitude,
            Description = savedLocation.Description,
            CreatedAt = savedLocation.CreatedAt,
            UpdatedAt = savedLocation.UpdatedAt
        };
    }
}
