using TrueDope.Api.DTOs.Locations;
using TrueDope.Api.DTOs.SharedLocations;

namespace TrueDope.Api.Services;

public interface ISharedLocationService
{
    /// <summary>
    /// Get all active shared locations (for all users)
    /// </summary>
    Task<List<SharedLocationListDto>> GetActiveLocationsAsync(string? search = null, string? state = null);

    /// <summary>
    /// Get all shared locations including inactive (for admin)
    /// </summary>
    Task<List<SharedLocationAdminDto>> GetAllLocationsAsync(bool includeInactive = true);

    /// <summary>
    /// Get a single shared location by ID
    /// </summary>
    Task<SharedLocationAdminDto?> GetByIdAsync(int id);

    /// <summary>
    /// Create a new shared location (admin only)
    /// </summary>
    Task<int> CreateAsync(string userId, CreateSharedLocationDto dto);

    /// <summary>
    /// Update an existing shared location (admin only)
    /// </summary>
    Task UpdateAsync(int id, UpdateSharedLocationDto dto);

    /// <summary>
    /// Delete a shared location (admin only)
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Copy a shared location to a user's saved locations
    /// </summary>
    Task<LocationDetailDto> CopyToSavedAsync(int sharedLocationId, string userId);
}
