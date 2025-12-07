using TrueDope.Api.DTOs.Locations;

namespace TrueDope.Api.Services;

public interface ILocationService
{
    Task<List<LocationListDto>> GetLocationsAsync(string userId);
    Task<LocationDetailDto?> GetLocationAsync(string userId, int locationId);
    Task<int> CreateLocationAsync(string userId, CreateLocationDto dto);
    Task<bool> UpdateLocationAsync(string userId, int locationId, UpdateLocationDto dto);
    Task<bool> DeleteLocationAsync(string userId, int locationId);
}
