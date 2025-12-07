using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrueDope.Api.Data;
using TrueDope.Api.Data.Entities;
using TrueDope.Api.DTOs;
using TrueDope.Api.DTOs.Admin;
using TrueDope.Api.Services;

namespace TrueDope.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        UserManager<User> userManager,
        ApplicationDbContext dbContext,
        IJwtService jwtService,
        ILogger<AdminController> logger)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// List all users (admin only)
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(PaginatedResponse<UserListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] bool sortDesc = true)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _dbContext.Users.AsQueryable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(searchLower)) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(searchLower)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(searchLower)));
        }

        // Sorting
        query = sortBy.ToLower() switch
        {
            "email" => sortDesc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            "lastloginat" => sortDesc ? query.OrderByDescending(u => u.LastLoginAt) : query.OrderBy(u => u.LastLoginAt),
            "firstname" => sortDesc ? query.OrderByDescending(u => u.FirstName) : query.OrderBy(u => u.FirstName),
            "lastname" => sortDesc ? query.OrderByDescending(u => u.LastName) : query.OrderBy(u => u.LastName),
            _ => sortDesc ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt)
        };

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserListItemDto
            {
                UserId = u.Id,
                Email = u.Email!,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsAdmin = u.IsAdmin,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync();

        return Ok(new PaginatedResponse<UserListItemDto>
        {
            Success = true,
            Items = users,
            Pagination = new PaginationInfo
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems
            }
        });
    }

    /// <summary>
    /// Get specific user details (admin only)
    /// </summary>
    [HttpGet("users/{userId}")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(ApiErrorResponse.Create("USER_NOT_FOUND", "User not found"));
        }

        var response = new UserDetailDto
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsAdmin = user.IsAdmin,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            EmailConfirmed = user.EmailConfirmed,
            LockoutEnd = user.LockoutEnd,
            LockoutEnabled = user.LockoutEnabled,
            AccessFailedCount = user.AccessFailedCount
        };

        return Ok(ApiResponse<UserDetailDto>.Ok(response));
    }

    /// <summary>
    /// Update user (admin only)
    /// </summary>
    [HttpPut("users/{userId}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserRequest request)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(ApiErrorResponse.Create("USER_NOT_FOUND", "User not found"));
        }

        // Prevent demoting yourself
        if (userId == currentUserId && request.IsAdmin == false)
        {
            return BadRequest(ApiErrorResponse.Create("CANNOT_DEMOTE_SELF", "You cannot remove your own admin privileges"));
        }

        if (request.FirstName != null)
            user.FirstName = request.FirstName;

        if (request.LastName != null)
            user.LastName = request.LastName;

        if (request.IsAdmin.HasValue)
            user.IsAdmin = request.IsAdmin.Value;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to update user: {UserId}", userId);
            return BadRequest(ApiErrorResponse.Create("UPDATE_FAILED", "Failed to update user"));
        }

        _logger.LogInformation("Admin updated user: {UserId}", userId);

        return Ok(ApiResponse.Ok("User updated successfully"));
    }

    /// <summary>
    /// Force password reset for user (admin only)
    /// </summary>
    [HttpPost("users/{userId}/reset-password")]
    [ProducesResponseType(typeof(ApiResponse<ResetUserPasswordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetUserPassword(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(ApiErrorResponse.Create("USER_NOT_FOUND", "User not found"));
        }

        // Generate a temporary password
        var tempPassword = GenerateTemporaryPassword();

        // Reset the password
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, tempPassword);

        if (!result.Succeeded)
        {
            _logger.LogError("Failed to reset password for user: {UserId}", userId);
            return BadRequest(ApiErrorResponse.Create("RESET_FAILED", "Failed to reset password"));
        }

        // Revoke all refresh tokens
        await _jwtService.RevokeAllRefreshTokensAsync(userId);

        _logger.LogInformation("Admin reset password for user: {UserId}", userId);

        return Ok(ApiResponse<ResetUserPasswordResponse>.Ok(
            new ResetUserPasswordResponse { TemporaryPassword = tempPassword },
            "Password reset. User must change password on next login."));
    }

    /// <summary>
    /// Disable user account (admin only)
    /// </summary>
    [HttpDelete("users/{userId}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DisableUser(string userId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == currentUserId)
        {
            return BadRequest(ApiErrorResponse.Create("CANNOT_DISABLE_SELF", "You cannot disable your own account"));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(ApiErrorResponse.Create("USER_NOT_FOUND", "User not found"));
        }

        // Soft delete - lock the account far into the future
        user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
        user.LockoutEnabled = true;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to disable user: {UserId}", userId);
            return BadRequest(ApiErrorResponse.Create("DISABLE_FAILED", "Failed to disable user"));
        }

        // Revoke all refresh tokens
        await _jwtService.RevokeAllRefreshTokensAsync(userId);

        _logger.LogInformation("Admin disabled user: {UserId}", userId);

        return Ok(ApiResponse.Ok("User account disabled"));
    }

    /// <summary>
    /// Re-enable user account (admin only)
    /// </summary>
    [HttpPost("users/{userId}/enable")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EnableUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(ApiErrorResponse.Create("USER_NOT_FOUND", "User not found"));
        }

        user.LockoutEnd = null;
        user.AccessFailedCount = 0;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to enable user: {UserId}", userId);
            return BadRequest(ApiErrorResponse.Create("ENABLE_FAILED", "Failed to enable user"));
        }

        _logger.LogInformation("Admin enabled user: {UserId}", userId);

        return Ok(ApiResponse.Ok("User account enabled"));
    }

    private static string GenerateTemporaryPassword()
    {
        const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";

        var random = new byte[12];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(random);

        var password = new char[12];

        // Ensure at least one of each required character type
        password[0] = upperCase[random[0] % upperCase.Length];
        password[1] = lowerCase[random[1] % lowerCase.Length];
        password[2] = digits[random[2] % digits.Length];

        // Fill the rest randomly
        var allChars = upperCase + lowerCase + digits;
        for (int i = 3; i < 12; i++)
        {
            password[i] = allChars[random[i] % allChars.Length];
        }

        // Shuffle the password
        for (int i = password.Length - 1; i > 0; i--)
        {
            int j = random[i] % (i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }
}
