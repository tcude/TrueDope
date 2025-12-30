using Microsoft.EntityFrameworkCore;
using TrueDope.Api.Data;
using TrueDope.Api.Data.Entities;
using TrueDope.Api.DTOs.Users;

namespace TrueDope.Api.Services;

public class PreferencesService : IPreferencesService
{
    private readonly ApplicationDbContext _context;

    public PreferencesService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserPreferencesResponse> GetPreferencesAsync(string userId)
    {
        var preferences = await _context.UserPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preferences == null)
        {
            // Return defaults if no preferences exist
            return new UserPreferencesResponse();
        }

        return MapToResponse(preferences);
    }

    public async Task<UserPreferencesResponse> UpdatePreferencesAsync(string userId, UpdatePreferencesRequest request)
    {
        var preferences = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preferences == null)
        {
            // Try to create, but handle race condition with duplicate key
            try
            {
                preferences = new UserPreferences
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.UserPreferences.Add(preferences);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Another request created it first (race condition), fetch it
                _context.ChangeTracker.Clear();
                preferences = await _context.UserPreferences
                    .FirstAsync(p => p.UserId == userId);
            }
        }

        // Apply updates (only non-null values)
        if (request.DistanceUnit != null)
            preferences.DistanceUnit = ParseDistanceUnit(request.DistanceUnit);

        if (request.AdjustmentUnit != null)
            preferences.AdjustmentUnit = ParseAdjustmentUnit(request.AdjustmentUnit);

        if (request.TemperatureUnit != null)
            preferences.TemperatureUnit = ParseTemperatureUnit(request.TemperatureUnit);

        if (request.PressureUnit != null)
            preferences.PressureUnit = ParsePressureUnit(request.PressureUnit);

        if (request.VelocityUnit != null)
            preferences.VelocityUnit = ParseVelocityUnit(request.VelocityUnit);

        if (request.Theme != null)
            preferences.Theme = ParseTheme(request.Theme);

        if (request.GroupSizeMethod != null)
            preferences.GroupSizeMethod = ParseGroupSizeMethod(request.GroupSizeMethod);

        preferences.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToResponse(preferences);
    }

    public async Task CreateDefaultPreferencesAsync(string userId)
    {
        var exists = await _context.UserPreferences.AnyAsync(p => p.UserId == userId);
        if (exists) return;

        try
        {
            var preferences = new UserPreferences
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserPreferences.Add(preferences);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Race condition - preferences already exist, ignore
        }
    }

    private static UserPreferencesResponse MapToResponse(UserPreferences preferences)
    {
        return new UserPreferencesResponse
        {
            DistanceUnit = preferences.DistanceUnit.ToString().ToLowerInvariant(),
            AdjustmentUnit = preferences.AdjustmentUnit.ToString().ToLowerInvariant(),
            TemperatureUnit = preferences.TemperatureUnit.ToString().ToLowerInvariant(),
            PressureUnit = preferences.PressureUnit.ToString().ToLowerInvariant(),
            VelocityUnit = preferences.VelocityUnit.ToString().ToLowerInvariant(),
            Theme = preferences.Theme.ToString().ToLowerInvariant(),
            GroupSizeMethod = preferences.GroupSizeMethod == GroupSizeMethod.CenterToCenter ? "ctc" : "ete"
        };
    }

    private static DistanceUnit ParseDistanceUnit(string value) => value.ToLowerInvariant() switch
    {
        "yards" => DistanceUnit.Yards,
        "meters" => DistanceUnit.Meters,
        _ => DistanceUnit.Yards
    };

    private static AdjustmentUnit ParseAdjustmentUnit(string value) => value.ToLowerInvariant() switch
    {
        "mil" => AdjustmentUnit.MIL,
        "moa" => AdjustmentUnit.MOA,
        _ => AdjustmentUnit.MIL
    };

    private static TemperatureUnit ParseTemperatureUnit(string value) => value.ToLowerInvariant() switch
    {
        "fahrenheit" => TemperatureUnit.Fahrenheit,
        "celsius" => TemperatureUnit.Celsius,
        _ => TemperatureUnit.Fahrenheit
    };

    private static PressureUnit ParsePressureUnit(string value) => value.ToLowerInvariant() switch
    {
        "inhg" => PressureUnit.InHg,
        "hpa" => PressureUnit.HPa,
        _ => PressureUnit.InHg
    };

    private static VelocityUnit ParseVelocityUnit(string value) => value.ToLowerInvariant() switch
    {
        "fps" => VelocityUnit.FPS,
        "mps" => VelocityUnit.MPS,
        _ => VelocityUnit.FPS
    };

    private static ThemePreference ParseTheme(string value) => value.ToLowerInvariant() switch
    {
        "system" => ThemePreference.System,
        "light" => ThemePreference.Light,
        "dark" => ThemePreference.Dark,
        _ => ThemePreference.System
    };

    private static GroupSizeMethod ParseGroupSizeMethod(string value) => value.ToLowerInvariant() switch
    {
        "ctc" => GroupSizeMethod.CenterToCenter,
        "ete" => GroupSizeMethod.EdgeToEdge,
        _ => GroupSizeMethod.CenterToCenter
    };
}
