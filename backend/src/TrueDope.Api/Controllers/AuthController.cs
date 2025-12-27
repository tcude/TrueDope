using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TrueDope.Api.Configuration;
using TrueDope.Api.Data;
using TrueDope.Api.Data.Entities;
using TrueDope.Api.DTOs;
using TrueDope.Api.DTOs.Auth;
using TrueDope.Api.Services;

namespace TrueDope.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly IPreferencesService _preferencesService;
    private readonly ApplicationDbContext _dbContext;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IJwtService jwtService,
        IEmailService emailService,
        IPreferencesService preferencesService,
        ApplicationDbContext dbContext,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthController> logger,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _emailService = emailService;
        _preferencesService = preferencesService;
        _dbContext = dbContext;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
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

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Conflict(ApiErrorResponse.Create("USER_EXISTS", "A user with this email already exists"));
        }

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Description).ToArray()
                );
            return BadRequest(ApiErrorResponse.ValidationError("Failed to create user account", errors));
        }

        // Create default preferences for new user
        await _preferencesService.CreateDefaultPreferencesAsync(user.Id);

        TrueDopeMetrics.RecordRegistration();
        _logger.LogInformation("New user registered: {Email}", request.Email);

        var response = new RegisterResponse
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName
        };

        return CreatedAtAction(nameof(Register), ApiResponse<RegisterResponse>.Ok(response, "Account created successfully"));
    }

    /// <summary>
    /// Authenticate user and receive JWT tokens
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
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

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            TrueDopeMetrics.RecordLoginFailed();
            return Unauthorized(ApiErrorResponse.Create("INVALID_CREDENTIALS", "Invalid email or password"));
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User account locked out: {Email}", request.Email);
            return Unauthorized(ApiErrorResponse.Create("ACCOUNT_LOCKED", "Account is locked. Please try again later."));
        }

        if (!result.Succeeded)
        {
            TrueDopeMetrics.RecordLoginFailed();
            _logger.LogWarning("Failed login attempt for user: {Email}", request.Email);
            return Unauthorized(ApiErrorResponse.Create("INVALID_CREDENTIALS", "Invalid email or password"));
        }

        // Update last login time
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Store refresh token
        await _jwtService.StoreRefreshTokenAsync(user.Id, refreshToken);

        TrueDopeMetrics.RecordLoginSuccessful();
        _logger.LogInformation("User logged in: {Email}", request.Email);

        var response = new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _jwtSettings.AccessTokenExpirationMinutes * 60,
            TokenType = "Bearer",
            User = new UserInfoDto
            {
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsAdmin = user.IsAdmin
            }
        };

        return Ok(ApiResponse<LoginResponse>.Ok(response, "Login successful"));
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RefreshResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiErrorResponse.Create("VALIDATION_ERROR", "Refresh token is required"));
        }

        // Extract user ID from Authorization header (even if expired)
        string? userId = null;
        var authHeader = Request.Headers.Authorization.FirstOrDefault();

        if (authHeader != null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

                // Validate token WITHOUT checking expiration
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = false,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract user ID from expired token");
            }
        }

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiErrorResponse.Create("INVALID_TOKEN", "Invalid or missing access token"));
        }

        // Validate refresh token
        var isValid = await _jwtService.ValidateRefreshTokenAsync(userId, request.RefreshToken);
        if (!isValid)
        {
            return Unauthorized(ApiErrorResponse.Create("INVALID_REFRESH_TOKEN", "Invalid or expired refresh token"));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized(ApiErrorResponse.Create("USER_NOT_FOUND", "User not found"));
        }

        // Revoke old refresh token
        await _jwtService.RevokeRefreshTokenAsync(userId, request.RefreshToken);

        // Generate new tokens
        var newAccessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        // Store new refresh token
        await _jwtService.StoreRefreshTokenAsync(userId, newRefreshToken);

        _logger.LogDebug("Tokens refreshed for user: {UserId}", userId);

        var response = new RefreshResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = _jwtSettings.AccessTokenExpirationMinutes * 60,
            TokenType = "Bearer"
        };

        return Ok(ApiResponse<RefreshResponse>.Ok(response));
    }

    /// <summary>
    /// Logout and invalidate refresh token
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiErrorResponse.Create("UNAUTHORIZED", "User not authenticated"));
        }

        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            await _jwtService.RevokeRefreshTokenAsync(userId, request.RefreshToken);
        }

        _logger.LogInformation("User logged out: {UserId}", userId);

        return Ok(ApiResponse.Ok("Logged out successfully"));
    }

    /// <summary>
    /// Request a password reset email
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        // Always return success to prevent email enumeration
        if (!ModelState.IsValid)
        {
            return Ok(ApiResponse.Ok("If an account exists with this email, a reset link has been sent"));
        }

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user != null)
        {
            // Generate a secure reset token
            var resetToken = GenerateSecureToken();
            // Store hashed token for security - never store plain text tokens
            user.PasswordResetToken = HashToken(resetToken);
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

            await _dbContext.SaveChangesAsync();

            // Get the frontend URL for password reset
            var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:3000";
            var resetUrl = $"{frontendUrl}/reset-password";

            try
            {
                // Send the unhashed token to the user via email
                await _emailService.SendPasswordResetEmailAsync(user.Email!, resetToken, resetUrl);
                _logger.LogInformation("Password reset email sent to: {Email}", request.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to: {Email}", request.Email);
                // Don't expose the error to the user
            }
        }
        else
        {
            _logger.LogInformation("Password reset requested for non-existent email: {Email}", request.Email);
        }

        return Ok(ApiResponse.Ok("If an account exists with this email, a reset link has been sent"));
    }

    /// <summary>
    /// Reset password using token from email
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
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

        // Hash the incoming token to compare with stored hash
        var hashedToken = HashToken(request.Token);

        // Find user by reset token (comparing hashed values)
        var user = _dbContext.Users.FirstOrDefault(u =>
            u.PasswordResetToken == hashedToken &&
            u.PasswordResetTokenExpiry > DateTime.UtcNow);

        if (user == null)
        {
            return BadRequest(ApiErrorResponse.Create("INVALID_TOKEN", "Invalid or expired reset token"));
        }

        // Reset the password
        var resetResult = await _userManager.RemovePasswordAsync(user);
        if (!resetResult.Succeeded)
        {
            _logger.LogError("Failed to remove password for user: {UserId}", user.Id);
            return BadRequest(ApiErrorResponse.Create("RESET_FAILED", "Failed to reset password"));
        }

        var addResult = await _userManager.AddPasswordAsync(user, request.NewPassword);
        if (!addResult.Succeeded)
        {
            var errors = addResult.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Description).ToArray()
                );
            return BadRequest(ApiErrorResponse.ValidationError("Password does not meet requirements", errors));
        }

        // Clear the reset token
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        await _dbContext.SaveChangesAsync();

        // Revoke all refresh tokens for security
        await _jwtService.RevokeAllRefreshTokensAsync(user.Id);

        TrueDopeMetrics.RecordPasswordReset();
        _logger.LogInformation("Password reset successfully for user: {Email}", user.Email);

        return Ok(ApiResponse.Ok("Password reset successfully"));
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private static string HashToken(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToBase64String(hashBytes);
    }
}
