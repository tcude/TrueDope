using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TrueDope.Api.Data;
using TrueDope.Api.Data.Entities;
using TrueDope.Api.DTOs.Locations;
using TrueDope.Api.Services;

namespace TrueDope.Api.Tests.Services;

public class LocationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly LocationService _sut;
    private readonly string _testUserId = "test-user-id";

    public LocationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        var loggerMock = new Mock<ILogger<LocationService>>();
        _sut = new LocationService(_context, loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task CreateLocationAsync_ShouldCreateAndReturnId()
    {
        // Arrange
        var dto = new CreateLocationDto
        {
            Name = "Home Range",
            Latitude = 38.8977m,
            Longitude = -77.0365m,
            Altitude = 500m,
            Description = "My home shooting range"
        };

        // Act
        var locationId = await _sut.CreateLocationAsync(_testUserId, dto);

        // Assert
        locationId.Should().BeGreaterThan(0);

        var location = await _context.SavedLocations.FindAsync(locationId);
        location.Should().NotBeNull();
        location!.Name.Should().Be("Home Range");
        location.Latitude.Should().Be(38.8977m);
        location.UserId.Should().Be(_testUserId);
    }

    [Fact]
    public async Task GetLocationsAsync_ShouldReturnOnlyUserLocations()
    {
        // Arrange
        _context.SavedLocations.AddRange(
            new SavedLocation { UserId = _testUserId, Name = "Range 1", Latitude = 38m, Longitude = -77m },
            new SavedLocation { UserId = _testUserId, Name = "Range 2", Latitude = 39m, Longitude = -78m },
            new SavedLocation { UserId = "other-user", Name = "Other Range", Latitude = 40m, Longitude = -79m }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetLocationsAsync(_testUserId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(l => l.Name.Should().NotBe("Other Range"));
    }

    [Fact]
    public async Task GetLocationAsync_WhenExists_ShouldReturnLocation()
    {
        // Arrange
        var location = new SavedLocation
        {
            UserId = _testUserId,
            Name = "Test Range",
            Latitude = 38.5m,
            Longitude = -77.5m
        };
        _context.SavedLocations.Add(location);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetLocationAsync(_testUserId, location.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Range");
    }

    [Fact]
    public async Task GetLocationAsync_WhenBelongsToDifferentUser_ShouldReturnNull()
    {
        // Arrange
        var location = new SavedLocation
        {
            UserId = "other-user",
            Name = "Other User's Range",
            Latitude = 38m,
            Longitude = -77m
        };
        _context.SavedLocations.Add(location);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetLocationAsync(_testUserId, location.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateLocationAsync_ShouldUpdateFields()
    {
        // Arrange
        var location = new SavedLocation
        {
            UserId = _testUserId,
            Name = "Original Name",
            Latitude = 38m,
            Longitude = -77m
        };
        _context.SavedLocations.Add(location);
        await _context.SaveChangesAsync();

        var dto = new UpdateLocationDto
        {
            Name = "Updated Name",
            Altitude = 1000m
        };

        // Act
        var result = await _sut.UpdateLocationAsync(_testUserId, location.Id, dto);

        // Assert
        result.Should().BeTrue();

        var updated = await _context.SavedLocations.FindAsync(location.Id);
        updated!.Name.Should().Be("Updated Name");
        updated.Altitude.Should().Be(1000m);
        updated.Latitude.Should().Be(38m); // Unchanged
    }

    [Fact]
    public async Task DeleteLocationAsync_ShouldDeleteLocation()
    {
        // Arrange
        var location = new SavedLocation
        {
            UserId = _testUserId,
            Name = "To Delete",
            Latitude = 38m,
            Longitude = -77m
        };
        _context.SavedLocations.Add(location);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteLocationAsync(_testUserId, location.Id);

        // Assert
        result.Should().BeTrue();

        var deleted = await _context.SavedLocations.FindAsync(location.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteLocationAsync_WhenNotFound_ShouldReturnFalse()
    {
        // Act
        var result = await _sut.DeleteLocationAsync(_testUserId, 999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetLocationsAsync_ShouldIncludeSessionCount()
    {
        // Arrange
        var rifle = new RifleSetup
        {
            UserId = _testUserId,
            Name = "Test Rifle",
            Caliber = ".308",
            ZeroDistance = 100
        };
        _context.RifleSetups.Add(rifle);

        var location = new SavedLocation
        {
            UserId = _testUserId,
            Name = "Test Range",
            Latitude = 38m,
            Longitude = -77m
        };
        _context.SavedLocations.Add(location);
        await _context.SaveChangesAsync();

        // Add 3 sessions at this location - set both FK and navigation property for InMemory provider
        for (int i = 0; i < 3; i++)
        {
            var session = new RangeSession
            {
                UserId = _testUserId,
                SavedLocationId = location.Id,
                SavedLocation = location, // Explicitly set navigation property for InMemory EF
                RifleSetupId = rifle.Id,
                RifleSetup = rifle,
                SessionDate = DateTime.UtcNow.AddDays(-i)
            };
            _context.RangeSessions.Add(session);
        }
        await _context.SaveChangesAsync();

        // Clear the change tracker to ensure fresh query
        _context.ChangeTracker.Clear();

        // Act
        var result = await _sut.GetLocationsAsync(_testUserId);

        // Assert
        result.Should().HaveCount(1);
        result[0].SessionCount.Should().Be(3);
    }
}
