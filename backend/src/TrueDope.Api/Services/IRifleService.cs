using TrueDope.Api.DTOs;
using TrueDope.Api.DTOs.Rifles;

namespace TrueDope.Api.Services;

public interface IRifleService
{
    Task<PaginatedResponse<RifleListDto>> GetRiflesAsync(string userId, RifleFilterDto filter);
    Task<RifleDetailDto?> GetRifleAsync(string userId, int rifleId);
    Task<int> CreateRifleAsync(string userId, CreateRifleDto dto);
    Task<bool> UpdateRifleAsync(string userId, int rifleId, UpdateRifleDto dto);
    Task<bool> DeleteRifleAsync(string userId, int rifleId);
    Task<bool> HasSessionsAsync(string userId, int rifleId);
}
