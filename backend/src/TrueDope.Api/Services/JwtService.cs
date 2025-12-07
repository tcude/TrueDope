using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using TrueDope.Api.Configuration;
using TrueDope.Api.Data.Entities;

namespace TrueDope.Api.Services;

public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<JwtService> _logger;

    public JwtService(
        IOptions<JwtSettings> jwtSettings,
        IConnectionMultiplexer redis,
        ILogger<JwtService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _redis = redis;
        _logger = logger;
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id)
        };

        if (!string.IsNullOrEmpty(user.FirstName))
        {
            claims.Add(new Claim(ClaimTypes.GivenName, user.FirstName));
        }

        if (!string.IsNullOrEmpty(user.LastName))
        {
            claims.Add(new Claim(ClaimTypes.Surname, user.LastName));
        }

        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            claims.Add(new Claim("IsAdmin", "true"));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public async Task<string> StoreRefreshTokenAsync(string userId, string refreshToken)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = GetRefreshTokenKey(userId, refreshToken);
            var expiration = TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays);

            await db.StringSetAsync(key, DateTime.UtcNow.ToString("o"), expiration);

            _logger.LogDebug("Refresh token stored for user {UserId}", userId);
            return refreshToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing refresh token for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = GetRefreshTokenKey(userId, refreshToken);

            var exists = await db.KeyExistsAsync(key);

            if (!exists)
            {
                _logger.LogWarning("Invalid or expired refresh token for user {UserId}", userId);
            }

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating refresh token for user {UserId}", userId);
            return false;
        }
    }

    public async Task RevokeRefreshTokenAsync(string userId, string refreshToken)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = GetRefreshTokenKey(userId, refreshToken);

            await db.KeyDeleteAsync(key);

            _logger.LogDebug("Refresh token revoked for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token for user {UserId}", userId);
            throw;
        }
    }

    public async Task RevokeAllRefreshTokensAsync(string userId)
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var db = _redis.GetDatabase();
            var pattern = $"refresh_token:{userId}:*";

            var keys = server.Keys(pattern: pattern).ToArray();

            if (keys.Length > 0)
            {
                await db.KeyDeleteAsync(keys);
                _logger.LogInformation("Revoked {Count} refresh tokens for user {UserId}", keys.Length, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all refresh tokens for user {UserId}", userId);
            throw;
        }
    }

    private static string GetRefreshTokenKey(string userId, string refreshToken)
    {
        return $"refresh_token:{userId}:{refreshToken}";
    }
}
