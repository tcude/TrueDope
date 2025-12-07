using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TrueDope.Api.Data;
using TrueDope.Api.Data.Entities;
using TrueDope.Api.DTOs.Rifles;
using TrueDope.Api.Services;

namespace TrueDope.Api.Tests.Services;

public class RifleServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly RifleService _sut;
    private readonly string _testUserId = "test-user-id";

    public RifleServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        var loggerMock = new Mock<ILogger<RifleService>>();
        _sut = new RifleService(_context, loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task CreateRifleAsync_ShouldCreateAndReturnId()
    {
        // Arrange
        var dto = new CreateRifleDto
        {
            Name = "Test Rifle",
            Caliber = ".308 Win",
            Manufacturer = "Remington",
            Model = "700",
            BarrelLength = 24,
            ZeroDistance = 100
        };

        // Act
        var rifleId = await _sut.CreateRifleAsync(_testUserId, dto);

        // Assert
        rifleId.Should().BeGreaterThan(0);

        var rifle = await _context.RifleSetups.FindAsync(rifleId);
        rifle.Should().NotBeNull();
        rifle!.Name.Should().Be("Test Rifle");
        rifle.Caliber.Should().Be(".308 Win");
        rifle.UserId.Should().Be(_testUserId);
    }

    [Fact]
    public async Task GetRifleAsync_WhenExists_ShouldReturnRifle()
    {
        // Arrange
        var rifle = new RifleSetup
        {
            UserId = _testUserId,
            Name = "My Rifle",
            Caliber = "6.5 Creedmoor",
            ZeroDistance = 100
        };
        _context.RifleSetups.Add(rifle);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetRifleAsync(_testUserId, rifle.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("My Rifle");
        result.Caliber.Should().Be("6.5 Creedmoor");
    }

    [Fact]
    public async Task GetRifleAsync_WhenNotExists_ShouldReturnNull()
    {
        // Act
        var result = await _sut.GetRifleAsync(_testUserId, 999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRifleAsync_WhenBelongsToDifferentUser_ShouldReturnNull()
    {
        // Arrange
        var rifle = new RifleSetup
        {
            UserId = "other-user-id",
            Name = "Other User's Rifle",
            Caliber = ".223 Rem",
            ZeroDistance = 100
        };
        _context.RifleSetups.Add(rifle);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetRifleAsync(_testUserId, rifle.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRiflesAsync_ShouldReturnOnlyUserRifles()
    {
        // Arrange
        _context.RifleSetups.AddRange(
            new RifleSetup { UserId = _testUserId, Name = "Rifle 1", Caliber = ".308", ZeroDistance = 100 },
            new RifleSetup { UserId = _testUserId, Name = "Rifle 2", Caliber = ".223", ZeroDistance = 100 },
            new RifleSetup { UserId = "other-user", Name = "Other Rifle", Caliber = ".300", ZeroDistance = 100 }
        );
        await _context.SaveChangesAsync();

        var filter = new RifleFilterDto();

        // Act
        var result = await _sut.GetRiflesAsync(_testUserId, filter);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(r => r.Name.Should().NotBe("Other Rifle"));
    }

    [Fact]
    public async Task GetRiflesAsync_WithSearch_ShouldFilterByName()
    {
        // Arrange
        _context.RifleSetups.AddRange(
            new RifleSetup { UserId = _testUserId, Name = "Precision Rifle", Caliber = ".308", ZeroDistance = 100 },
            new RifleSetup { UserId = _testUserId, Name = "Hunting Rifle", Caliber = ".308", ZeroDistance = 100 },
            new RifleSetup { UserId = _testUserId, Name = "AR-15", Caliber = ".223", ZeroDistance = 100 }
        );
        await _context.SaveChangesAsync();

        var filter = new RifleFilterDto { Search = "rifle" };

        // Act
        var result = await _sut.GetRiflesAsync(_testUserId, filter);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(r => r.Name.Contains("Rifle"));
    }

    [Fact]
    public async Task GetRiflesAsync_ShouldPaginate()
    {
        // Arrange
        for (int i = 1; i <= 15; i++)
        {
            _context.RifleSetups.Add(new RifleSetup
            {
                UserId = _testUserId,
                Name = $"Rifle {i}",
                Caliber = ".308",
                ZeroDistance = 100
            });
        }
        await _context.SaveChangesAsync();

        var filter = new RifleFilterDto { Page = 2, PageSize = 5 };

        // Act
        var result = await _sut.GetRiflesAsync(_testUserId, filter);

        // Assert
        result.Items.Should().HaveCount(5);
        result.Pagination.CurrentPage.Should().Be(2);
        result.Pagination.TotalItems.Should().Be(15);
        result.Pagination.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task UpdateRifleAsync_ShouldUpdateFields()
    {
        // Arrange
        var rifle = new RifleSetup
        {
            UserId = _testUserId,
            Name = "Original Name",
            Caliber = ".308",
            ZeroDistance = 100
        };
        _context.RifleSetups.Add(rifle);
        await _context.SaveChangesAsync();

        var dto = new UpdateRifleDto
        {
            Name = "Updated Name",
            MuzzleVelocity = 2800
        };

        // Act
        var result = await _sut.UpdateRifleAsync(_testUserId, rifle.Id, dto);

        // Assert
        result.Should().BeTrue();

        var updated = await _context.RifleSetups.FindAsync(rifle.Id);
        updated!.Name.Should().Be("Updated Name");
        updated.MuzzleVelocity.Should().Be(2800);
        updated.Caliber.Should().Be(".308"); // Unchanged
    }

    [Fact]
    public async Task UpdateRifleAsync_WhenNotFound_ShouldReturnFalse()
    {
        // Arrange
        var dto = new UpdateRifleDto { Name = "New Name" };

        // Act
        var result = await _sut.UpdateRifleAsync(_testUserId, 999, dto);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteRifleAsync_ShouldDeleteRifle()
    {
        // Arrange
        var rifle = new RifleSetup
        {
            UserId = _testUserId,
            Name = "To Delete",
            Caliber = ".308",
            ZeroDistance = 100
        };
        _context.RifleSetups.Add(rifle);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteRifleAsync(_testUserId, rifle.Id);

        // Assert
        result.Should().BeTrue();

        var deleted = await _context.RifleSetups.FindAsync(rifle.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteRifleAsync_WhenHasSessions_ShouldThrow()
    {
        // Arrange
        var rifle = new RifleSetup
        {
            UserId = _testUserId,
            Name = "Rifle With Sessions",
            Caliber = ".308",
            ZeroDistance = 100
        };
        _context.RifleSetups.Add(rifle);
        await _context.SaveChangesAsync();

        var session = new RangeSession
        {
            UserId = _testUserId,
            RifleSetupId = rifle.Id,
            SessionDate = DateTime.UtcNow
        };
        _context.RangeSessions.Add(session);
        await _context.SaveChangesAsync();

        // Act & Assert
        await _sut.Invoking(s => s.DeleteRifleAsync(_testUserId, rifle.Id))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sessions*");
    }

    [Fact]
    public async Task HasSessionsAsync_WhenHasSessions_ShouldReturnTrue()
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
        await _context.SaveChangesAsync();

        var session = new RangeSession
        {
            UserId = _testUserId,
            RifleSetupId = rifle.Id,
            SessionDate = DateTime.UtcNow
        };
        _context.RangeSessions.Add(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.HasSessionsAsync(_testUserId, rifle.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasSessionsAsync_WhenNoSessions_ShouldReturnFalse()
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
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.HasSessionsAsync(_testUserId, rifle.Id);

        // Assert
        result.Should().BeFalse();
    }
}
