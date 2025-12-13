using TrueDope.Api.DTOs.Users;

namespace TrueDope.Api.Services;

public interface IPreferencesService
{
    Task<UserPreferencesResponse> GetPreferencesAsync(string userId);
    Task<UserPreferencesResponse> UpdatePreferencesAsync(string userId, UpdatePreferencesRequest request);
    Task CreateDefaultPreferencesAsync(string userId);
}
