using Microsoft.EntityFrameworkCore;
using TrueDope.Api.Data;
using TrueDope.Api.Data.Entities;
using TrueDope.Api.DTOs.Locations;

namespace TrueDope.Api.Services;

public class LocationService : ILocationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LocationService> _logger;

    public LocationService(ApplicationDbContext context, ILogger<LocationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<LocationListDto>> GetLocationsAsync(string userId)
    {
        return await _context.SavedLocations
            .Include(l => l.RangeSessions)
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.Name)
            .Select(l => new LocationListDto
            {
                Id = l.Id,
                Name = l.Name,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                Altitude = l.Altitude,
                Description = l.Description,
                SessionCount = l.RangeSessions.Count,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<LocationDetailDto?> GetLocationAsync(string userId, int locationId)
    {
        var location = await _context.SavedLocations
            .FirstOrDefaultAsync(l => l.Id == locationId && l.UserId == userId);

        if (location == null)
            return null;

        return new LocationDetailDto
        {
            Id = location.Id,
            Name = location.Name,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            Altitude = location.Altitude,
            Description = location.Description,
            CreatedAt = location.CreatedAt,
            UpdatedAt = location.UpdatedAt
        };
    }

    public async Task<int> CreateLocationAsync(string userId, CreateLocationDto dto)
    {
        var location = new SavedLocation
        {
            UserId = userId,
            Name = dto.Name,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Altitude = dto.Altitude,
            Description = dto.Description
        };

        _context.SavedLocations.Add(location);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created location {LocationId} '{LocationName}' for user {UserId}", location.Id, location.Name, userId);

        return location.Id;
    }

    public async Task<bool> UpdateLocationAsync(string userId, int locationId, UpdateLocationDto dto)
    {
        var location = await _context.SavedLocations
            .FirstOrDefaultAsync(l => l.Id == locationId && l.UserId == userId);

        if (location == null)
            return false;

        if (dto.Name != null) location.Name = dto.Name;
        if (dto.Latitude.HasValue) location.Latitude = dto.Latitude.Value;
        if (dto.Longitude.HasValue) location.Longitude = dto.Longitude.Value;
        if (dto.Altitude.HasValue) location.Altitude = dto.Altitude;
        if (dto.Description != null) location.Description = dto.Description;

        location.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteLocationAsync(string userId, int locationId)
    {
        var location = await _context.SavedLocations
            .FirstOrDefaultAsync(l => l.Id == locationId && l.UserId == userId);

        if (location == null)
            return false;

        // Sessions using this location will have SavedLocationId set to null (SetNull behavior)
        _context.SavedLocations.Remove(location);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted location {LocationId} for user {UserId}", locationId, userId);

        return true;
    }
}
