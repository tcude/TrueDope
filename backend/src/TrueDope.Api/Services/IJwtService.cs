using TrueDope.Api.Data.Entities;

namespace TrueDope.Api.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Task<string> StoreRefreshTokenAsync(string userId, string refreshToken);
    Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken);
    Task RevokeRefreshTokenAsync(string userId, string refreshToken);
    Task RevokeAllRefreshTokensAsync(string userId);
}
