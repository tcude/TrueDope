using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using TrueDope.Api.Configuration;
using TrueDope.Api.Data.Entities;
using TrueDope.Api.Services;

namespace TrueDope.Api.Tests.Services;

public class JwtServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly Mock<IServer> _serverMock;
    private readonly Mock<ILogger<JwtService>> _loggerMock;
    private readonly JwtSettings _jwtSettings;
    private readonly JwtService _sut;

    public JwtServiceTests()
    {
        _redisMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        _serverMock = new Mock<IServer>();
        _loggerMock = new Mock<ILogger<JwtService>>();

        _jwtSettings = new JwtSettings
        {
            SecretKey = "ThisIsATestSecretKeyThatIsAtLeast32CharactersLong!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };

        _redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);

        _redisMock.Setup(x => x.GetEndPoints(It.IsAny<bool>()))
            .Returns(new[] { new System.Net.DnsEndPoint("localhost", 6379) });

        _redisMock.Setup(x => x.GetServer(It.IsAny<System.Net.EndPoint>(), It.IsAny<object>()))
            .Returns(_serverMock.Object);

        var options = Options.Create(_jwtSettings);
        _sut = new JwtService(options, _redisMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwt()
    {
        // Arrange
        var user = new User
        {
            Id = "test-user-id",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            IsAdmin = false
        };

        // Act
        var token = _sut.GenerateAccessToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be(_jwtSettings.Issuer);
        jwtToken.Audiences.Should().Contain(_jwtSettings.Audience);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
    }

    [Fact]
    public void GenerateAccessToken_ForAdmin_ShouldIncludeAdminClaim()
    {
        // Arrange
        var adminUser = new User
        {
            Id = "admin-user-id",
            Email = "admin@example.com",
            FirstName = "Admin",
            LastName = "User",
            IsAdmin = true
        };

        // Act
        var token = _sut.GenerateAccessToken(adminUser);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == "IsAdmin" && c.Value == "true");
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    [Fact]
    public void GenerateAccessToken_ForNonAdmin_ShouldNotIncludeAdminClaim()
    {
        // Arrange
        var user = new User
        {
            Id = "user-id",
            Email = "user@example.com",
            IsAdmin = false
        };

        // Act
        var token = _sut.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().NotContain(c => c.Type == "IsAdmin");
        jwtToken.Claims.Should().NotContain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnNonEmptyBase64String()
    {
        // Act
        var refreshToken = _sut.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNullOrEmpty();

        // Should be valid base64
        var bytes = Convert.FromBase64String(refreshToken);
        bytes.Should().HaveCount(64); // 64 random bytes
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnUniqueTokens()
    {
        // Act
        var token1 = _sut.GenerateRefreshToken();
        var token2 = _sut.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public async Task StoreRefreshTokenAsync_ShouldStoreInRedis()
    {
        // Arrange
        var userId = "test-user-id";
        var refreshToken = "test-refresh-token";

        _databaseMock
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.StoreRefreshTokenAsync(userId, refreshToken);

        // Assert
        result.Should().Be(refreshToken);
        _databaseMock.Verify(
            x => x.StringSetAsync(
                It.Is<RedisKey>(k => k.ToString().Contains(userId) && k.ToString().Contains(refreshToken)),
                It.IsAny<RedisValue>(),
                It.Is<TimeSpan?>(t => t.HasValue && t.Value.Days == _jwtSettings.RefreshTokenExpirationDays),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WhenValid_ShouldReturnTrue()
    {
        // Arrange
        var userId = "test-user-id";
        var refreshToken = "valid-token";

        _databaseMock
            .Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.ValidateRefreshTokenAsync(userId, refreshToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WhenInvalid_ShouldReturnFalse()
    {
        // Arrange
        var userId = "test-user-id";
        var refreshToken = "invalid-token";

        _databaseMock
            .Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.ValidateRefreshTokenAsync(userId, refreshToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_ShouldDeleteFromRedis()
    {
        // Arrange
        var userId = "test-user-id";
        var refreshToken = "token-to-revoke";

        _databaseMock
            .Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _sut.RevokeRefreshTokenAsync(userId, refreshToken);

        // Assert
        _databaseMock.Verify(
            x => x.KeyDeleteAsync(
                It.Is<RedisKey>(k => k.ToString().Contains(userId) && k.ToString().Contains(refreshToken)),
                It.IsAny<CommandFlags>()),
            Times.Once);
    }
}
