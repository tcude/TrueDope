using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TrueDope.Api.Data.Entities;
using TrueDope.Api.DTOs;
using TrueDope.Api.DTOs.Users;
using TrueDope.Api.Services;

namespace TrueDope.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<UsersController> _logger;
    private readonly IPreferencesService _preferencesService;

    public UsersController(
        UserManager<User> userManager,
        ILogger<UsersController> logger,
        IPreferencesService preferencesService)
    {
        _userManager = userManager;
        _logger = logger;
        _preferencesService = preferencesService;
    }

    /// <summary>
    /// Get current user's profile
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiErrorResponse.Create("UNAUTHORIZED", "User not authenticated"));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized(ApiErrorResponse.Create("USER_NOT_FOUND", "User not found"));
        }

        var response = new UserProfileResponse
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsAdmin = user.IsAdmin,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };

        return Ok(ApiResponse<UserProfileResponse>.Ok(response));
    }

    /// <summary>
    /// Update current user's profile
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            return BadRequest(ApiErrorResponse.ValidationError("Validation failed", errors));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiErrorResponse.Create("UNAUTHORIZED", "User not authenticated"));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized(ApiErrorResponse.Create("USER_NOT_FOUND", "User not found"));
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to update profile for user: {UserId}", userId);
            return BadRequest(ApiErrorResponse.Create("UPDATE_FAILED", "Failed to update profile"));
        }

        _logger.LogInformation("Profile updated for user: {UserId}", userId);

        var response = new UserProfileResponse
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsAdmin = user.IsAdmin,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };

        return Ok(ApiResponse<UserProfileResponse>.Ok(response, "Profile updated successfully"));
    }

    /// <summary>
    /// Change current user's password
    /// </summary>
    [HttpPut("me/password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            return BadRequest(ApiErrorResponse.ValidationError("Validation failed", errors));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiErrorResponse.Create("UNAUTHORIZED", "User not authenticated"));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized(ApiErrorResponse.Create("USER_NOT_FOUND", "User not found"));
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code == "PasswordMismatch"))
            {
                return BadRequest(ApiErrorResponse.Create("INVALID_PASSWORD", "Current password is incorrect"));
            }

            var errors = result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Description).ToArray()
                );
            return BadRequest(ApiErrorResponse.ValidationError("Password change failed", errors));
        }

        _logger.LogInformation("Password changed for user: {UserId}", userId);

        return Ok(ApiResponse.Ok("Password changed successfully"));
    }

    /// <summary>
    /// Get current user's preferences
    /// </summary>
    [HttpGet("me/preferences")]
    [ProducesResponseType(typeof(ApiResponse<UserPreferencesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPreferences()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiErrorResponse.Create("UNAUTHORIZED", "User not authenticated"));
        }

        var preferences = await _preferencesService.GetPreferencesAsync(userId);
        return Ok(ApiResponse<UserPreferencesResponse>.Ok(preferences));
    }

    /// <summary>
    /// Update current user's preferences
    /// </summary>
    [HttpPut("me/preferences")]
    [ProducesResponseType(typeof(ApiResponse<UserPreferencesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            return BadRequest(ApiErrorResponse.ValidationError("Validation failed", errors));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiErrorResponse.Create("UNAUTHORIZED", "User not authenticated"));
        }

        var preferences = await _preferencesService.UpdatePreferencesAsync(userId, request);
        _logger.LogInformation("Preferences updated for user: {UserId}", userId);

        return Ok(ApiResponse<UserPreferencesResponse>.Ok(preferences, "Preferences updated successfully"));
    }
}
