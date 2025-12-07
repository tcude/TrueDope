using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TrueDope.Api.Configuration;
using TrueDope.Api.Controllers;
using TrueDope.Api.Data;
using TrueDope.Api.Data.Entities;
using TrueDope.Api.DTOs;
using TrueDope.Api.DTOs.Auth;
using TrueDope.Api.Services;

namespace TrueDope.Api.Tests.Controllers;

public class AuthControllerTests : IDisposable
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<SignInManager<User>> _signInManagerMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly JwtSettings _jwtSettings;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        var userStoreMock = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<User>>();
        _signInManagerMock = new Mock<SignInManager<User>>(
            _userManagerMock.Object,
            contextAccessorMock.Object,
            claimsFactoryMock.Object,
            null!, null!, null!, null!);

        _jwtServiceMock = new Mock<IJwtService>();
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        _configurationMock = new Mock<IConfiguration>();

        // Create in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);

        _jwtSettings = new JwtSettings
        {
            SecretKey = "ThisIsATestSecretKeyThatIsAtLeast32CharactersLong!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };

        var jwtOptions = Options.Create(_jwtSettings);

        _controller = new AuthController(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _jwtServiceMock.Object,
            _emailServiceMock.Object,
            _dbContext,
            jwtOptions,
            _loggerMock.Object,
            _configurationMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region Register Tests

    [Fact]
    public async Task Register_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "Password123",
            FirstName = "New",
            LastName = "User"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<User>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = createdResult.Value.Should().BeAssignableTo<ApiResponse<RegisterResponse>>().Subject;

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ShouldReturnConflict()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "Password123"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(new User { Email = request.Email });

        // Act
        var result = await _controller.Register(request);

        // Assert
        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        var response = conflictResult.Value.Should().BeAssignableTo<ApiErrorResponse>().Subject;

        response.Error.Code.Should().Be("USER_EXISTS");
    }

    [Fact]
    public async Task Register_WhenIdentityFails_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "weak"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<User>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "PasswordTooShort", Description = "Password is too short" }));

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeAssignableTo<ApiErrorResponse>().Subject;

        // Controller uses VALIDATION_ERROR for Identity failures
        response.Error.Code.Should().Be("VALIDATION_ERROR");
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "ValidPassword123"
        };

        var user = new User
        {
            Id = "user-id",
            Email = request.Email,
            UserName = request.Email,
            FirstName = "Test",
            LastName = "User",
            IsAdmin = false,
            EmailConfirmed = true
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        // Use SignInManager.CheckPasswordSignInAsync instead of UserManager.CheckPasswordAsync
        _signInManagerMock
            .Setup(x => x.CheckPasswordSignInAsync(user, request.Password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        _userManagerMock
            .Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        _jwtServiceMock
            .Setup(x => x.GenerateAccessToken(user))
            .Returns("access-token");

        _jwtServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        _jwtServiceMock
            .Setup(x => x.StoreRefreshTokenAsync(user.Id, "refresh-token"))
            .ReturnsAsync("refresh-token");

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<ApiResponse<LoginResponse>>().Subject;

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.AccessToken.Should().Be("access-token");
        response.Data.RefreshToken.Should().Be("refresh-token");
        response.Data.User.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "Password123"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = unauthorizedResult.Value.Should().BeAssignableTo<ApiErrorResponse>().Subject;

        response.Error.Code.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "WrongPassword"
        };

        var user = new User
        {
            Id = "user-id",
            Email = request.Email,
            EmailConfirmed = true
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        // Use SignInManager.CheckPasswordSignInAsync - return Failed for wrong password
        _signInManagerMock
            .Setup(x => x.CheckPasswordSignInAsync(user, request.Password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = unauthorizedResult.Value.Should().BeAssignableTo<ApiErrorResponse>().Subject;

        response.Error.Code.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Login_WhenLockedOut_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "locked@example.com",
            Password = "Password123"
        };

        var user = new User
        {
            Id = "user-id",
            Email = request.Email,
            EmailConfirmed = true
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        // Use SignInManager.CheckPasswordSignInAsync - return LockedOut
        _signInManagerMock
            .Setup(x => x.CheckPasswordSignInAsync(user, request.Password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = unauthorizedResult.Value.Should().BeAssignableTo<ApiErrorResponse>().Subject;

        response.Error.Code.Should().Be("ACCOUNT_LOCKED");
    }

    #endregion

    #region Refresh Token Tests

    [Fact]
    public async Task Refresh_WithValidToken_ShouldReturnNewTokens()
    {
        // Arrange
        var request = new RefreshRequest
        {
            RefreshToken = "valid-refresh-token"
        };

        var user = new User
        {
            Id = "user-id",
            Email = "user@example.com"
        };

        // The Refresh endpoint extracts user ID from the Authorization header JWT
        // We need to generate a real JWT for this test
        var httpContext = new DefaultHttpContext();

        // Generate an actual JWT token using the test JWT settings
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var key = System.Text.Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);
        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email!)
            }),
            NotBefore = DateTime.UtcNow.AddMinutes(-10), // Set NotBefore before Expires
            Expires = DateTime.UtcNow.AddMinutes(-5), // Expired token is fine for refresh
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var accessToken = tokenHandler.WriteToken(token);

        httpContext.Request.Headers.Authorization = $"Bearer {accessToken}";

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        _jwtServiceMock
            .Setup(x => x.ValidateRefreshTokenAsync(user.Id, request.RefreshToken))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        _jwtServiceMock
            .Setup(x => x.RevokeRefreshTokenAsync(user.Id, request.RefreshToken))
            .Returns(Task.CompletedTask);

        _jwtServiceMock
            .Setup(x => x.GenerateAccessToken(user))
            .Returns("new-access-token");

        _jwtServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("new-refresh-token");

        _jwtServiceMock
            .Setup(x => x.StoreRefreshTokenAsync(user.Id, "new-refresh-token"))
            .ReturnsAsync("new-refresh-token");

        // Act
        var result = await _controller.Refresh(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<ApiResponse<RefreshResponse>>().Subject;

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.AccessToken.Should().Be("new-access-token");
        response.Data.RefreshToken.Should().Be("new-refresh-token");
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new RefreshRequest
        {
            RefreshToken = "invalid-refresh-token"
        };

        var userId = "user-id";

        // Generate an actual JWT token for the Authorization header
        var httpContext = new DefaultHttpContext();
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var key = System.Text.Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);
        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId)
            }),
            NotBefore = DateTime.UtcNow.AddMinutes(-10), // Set NotBefore before Expires
            Expires = DateTime.UtcNow.AddMinutes(-5),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var accessToken = tokenHandler.WriteToken(token);

        httpContext.Request.Headers.Authorization = $"Bearer {accessToken}";

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        _jwtServiceMock
            .Setup(x => x.ValidateRefreshTokenAsync(userId, request.RefreshToken))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Refresh(request);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = unauthorizedResult.Value.Should().BeAssignableTo<ApiErrorResponse>().Subject;

        response.Error.Code.Should().Be("INVALID_REFRESH_TOKEN");
    }

    [Fact]
    public async Task Refresh_WithMissingAuthorizationHeader_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new RefreshRequest
        {
            RefreshToken = "valid-refresh-token"
        };

        // No Authorization header set
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await _controller.Refresh(request);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = unauthorizedResult.Value.Should().BeAssignableTo<ApiErrorResponse>().Subject;

        response.Error.Code.Should().Be("INVALID_TOKEN");
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_ShouldRevokeRefreshToken()
    {
        // Arrange
        var request = new LogoutRequest
        {
            RefreshToken = "token-to-revoke"
        };

        var userId = "user-id";
        var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId) };
        var identity = new System.Security.Claims.ClaimsIdentity(claims);
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _jwtServiceMock
            .Setup(x => x.RevokeRefreshTokenAsync(userId, request.RefreshToken))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Logout(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<ApiResponse>().Subject;

        response.Success.Should().BeTrue();

        _jwtServiceMock.Verify(
            x => x.RevokeRefreshTokenAsync(userId, request.RefreshToken),
            Times.Once);
    }

    #endregion
}
