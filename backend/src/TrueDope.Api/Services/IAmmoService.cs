using TrueDope.Api.DTOs;
using TrueDope.Api.DTOs.Ammunition;

namespace TrueDope.Api.Services;

public interface IAmmoService
{
    // Ammunition CRUD
    Task<PaginatedResponse<AmmoListDto>> GetAmmoAsync(string userId, AmmoFilterDto filter);
    Task<AmmoDetailDto?> GetAmmoAsync(string userId, int ammoId);
    Task<int> CreateAmmoAsync(string userId, CreateAmmoDto dto);
    Task<AmmoDetailDto?> UpdateAmmoAsync(string userId, int ammoId, UpdateAmmoDto dto);
    Task<bool> DeleteAmmoAsync(string userId, int ammoId);
    Task<bool> HasSessionsAsync(string userId, int ammoId);

    // Lots CRUD
    Task<List<AmmoLotDto>> GetLotsAsync(string userId, int ammoId);
    Task<int> CreateLotAsync(string userId, int ammoId, CreateAmmoLotDto dto);
    Task<bool> UpdateLotAsync(string userId, int ammoId, int lotId, UpdateAmmoLotDto dto);
    Task<bool> DeleteLotAsync(string userId, int ammoId, int lotId);
    Task<bool> LotHasSessionsAsync(string userId, int lotId);
}
