using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TrueDope.Api.Data;
using TrueDope.Api.Data.Entities;
using TrueDope.Api.DTOs.Ammunition;
using TrueDope.Api.Services;

namespace TrueDope.Api.Tests.Services;

public class AmmoServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AmmoService _sut;
    private readonly string _testUserId = "test-user-id";

    public AmmoServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        var loggerMock = new Mock<ILogger<AmmoService>>();
        _sut = new AmmoService(_context, loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task CreateAmmoAsync_ShouldCreateAndReturnId()
    {
        // Arrange
        var dto = new CreateAmmoDto
        {
            Manufacturer = "Federal",
            Name = "Gold Medal Match",
            Caliber = ".308 Win",
            Grain = 175,
            BulletType = "SMK",
            CostPerRound = 1.50m
        };

        // Act
        var ammoId = await _sut.CreateAmmoAsync(_testUserId, dto);

        // Assert
        ammoId.Should().BeGreaterThan(0);

        var ammo = await _context.Ammunition.FindAsync(ammoId);
        ammo.Should().NotBeNull();
        ammo!.Manufacturer.Should().Be("Federal");
        ammo.Name.Should().Be("Gold Medal Match");
        ammo.Grain.Should().Be(175);
    }

    [Fact]
    public async Task GetAmmoAsync_List_ShouldReturnOnlyUserAmmo()
    {
        // Arrange
        _context.Ammunition.AddRange(
            new Ammunition { UserId = _testUserId, Manufacturer = "Federal", Name = "GMM", Caliber = ".308", Grain = 175 },
            new Ammunition { UserId = _testUserId, Manufacturer = "Hornady", Name = "ELD-M", Caliber = ".308", Grain = 178 },
            new Ammunition { UserId = "other-user", Manufacturer = "Lapua", Name = "Scenar", Caliber = ".308", Grain = 185 }
        );
        await _context.SaveChangesAsync();

        var filter = new AmmoFilterDto();

        // Act
        var result = await _sut.GetAmmoAsync(_testUserId, filter);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().NotContain(a => a.Manufacturer == "Lapua");
    }

    [Fact]
    public async Task GetAmmoAsync_WithCaliberFilter_ShouldFilter()
    {
        // Arrange
        _context.Ammunition.AddRange(
            new Ammunition { UserId = _testUserId, Manufacturer = "Federal", Name = "GMM 308", Caliber = ".308 Win", Grain = 175 },
            new Ammunition { UserId = _testUserId, Manufacturer = "Federal", Name = "GMM 223", Caliber = ".223 Rem", Grain = 77 },
            new Ammunition { UserId = _testUserId, Manufacturer = "Hornady", Name = "ELD-M", Caliber = ".308 Win", Grain = 178 }
        );
        await _context.SaveChangesAsync();

        var filter = new AmmoFilterDto { Caliber = ".308 Win" };

        // Act
        var result = await _sut.GetAmmoAsync(_testUserId, filter);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(a => a.Caliber == ".308 Win");
    }

    [Fact]
    public async Task GetAmmoAsync_WithSearch_ShouldSearchMultipleFields()
    {
        // Arrange
        _context.Ammunition.AddRange(
            new Ammunition { UserId = _testUserId, Manufacturer = "Federal", Name = "Gold Medal", Caliber = ".308", Grain = 175 },
            new Ammunition { UserId = _testUserId, Manufacturer = "Hornady", Name = "ELD Match", Caliber = ".308", Grain = 178 },
            new Ammunition { UserId = _testUserId, Manufacturer = "Lapua", Name = "Scenar", Caliber = ".308", Grain = 185 }
        );
        await _context.SaveChangesAsync();

        var filter = new AmmoFilterDto { Search = "federal" };

        // Act
        var result = await _sut.GetAmmoAsync(_testUserId, filter);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Manufacturer.Should().Be("Federal");
    }

    [Fact]
    public async Task GetAmmoAsync_Detail_ShouldIncludeLots()
    {
        // Arrange
        var ammo = new Ammunition
        {
            UserId = _testUserId,
            Manufacturer = "Federal",
            Name = "GMM",
            Caliber = ".308",
            Grain = 175
        };
        _context.Ammunition.Add(ammo);
        await _context.SaveChangesAsync();

        _context.AmmoLots.AddRange(
            new AmmoLot { AmmunitionId = ammo.Id, UserId = _testUserId, LotNumber = "LOT-001", InitialQuantity = 200 },
            new AmmoLot { AmmunitionId = ammo.Id, UserId = _testUserId, LotNumber = "LOT-002", InitialQuantity = 500 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAmmoAsync(_testUserId, ammo.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Lots.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateLotAsync_ShouldCreateAndReturnId()
    {
        // Arrange
        var ammo = new Ammunition
        {
            UserId = _testUserId,
            Manufacturer = "Federal",
            Name = "GMM",
            Caliber = ".308",
            Grain = 175
        };
        _context.Ammunition.Add(ammo);
        await _context.SaveChangesAsync();

        var dto = new CreateAmmoLotDto
        {
            LotNumber = "LOT-2024-001",
            InitialQuantity = 500,
            PurchasePrice = 750m,
            PurchaseDate = DateTime.UtcNow
        };

        // Act
        var lotId = await _sut.CreateLotAsync(_testUserId, ammo.Id, dto);

        // Assert
        lotId.Should().BeGreaterThan(0);

        var lot = await _context.AmmoLots.FindAsync(lotId);
        lot.Should().NotBeNull();
        lot!.LotNumber.Should().Be("LOT-2024-001");
        lot.InitialQuantity.Should().Be(500);
    }

    [Fact]
    public async Task CreateLotAsync_WhenAmmoNotFound_ShouldThrow()
    {
        // Arrange
        var dto = new CreateAmmoLotDto { LotNumber = "LOT-001" };

        // Act & Assert
        await _sut.Invoking(s => s.CreateLotAsync(_testUserId, 999, dto))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task GetLotsAsync_ShouldReturnOnlyLotsForAmmo()
    {
        // Arrange
        var ammo1 = new Ammunition { UserId = _testUserId, Manufacturer = "Federal", Name = "GMM", Caliber = ".308", Grain = 175 };
        var ammo2 = new Ammunition { UserId = _testUserId, Manufacturer = "Hornady", Name = "ELD", Caliber = ".308", Grain = 178 };
        _context.Ammunition.AddRange(ammo1, ammo2);
        await _context.SaveChangesAsync();

        _context.AmmoLots.AddRange(
            new AmmoLot { AmmunitionId = ammo1.Id, UserId = _testUserId, LotNumber = "LOT-A1" },
            new AmmoLot { AmmunitionId = ammo1.Id, UserId = _testUserId, LotNumber = "LOT-A2" },
            new AmmoLot { AmmunitionId = ammo2.Id, UserId = _testUserId, LotNumber = "LOT-B1" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetLotsAsync(_testUserId, ammo1.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(l => l.LotNumber.StartsWith("LOT-A"));
    }

    [Fact]
    public async Task UpdateAmmoAsync_ShouldUpdateFields()
    {
        // Arrange
        var ammo = new Ammunition
        {
            UserId = _testUserId,
            Manufacturer = "Federal",
            Name = "Original",
            Caliber = ".308",
            Grain = 175
        };
        _context.Ammunition.Add(ammo);
        await _context.SaveChangesAsync();

        var dto = new UpdateAmmoDto
        {
            Name = "Updated Name",
            BallisticCoefficient = 0.505m
        };

        // Act
        var result = await _sut.UpdateAmmoAsync(_testUserId, ammo.Id, dto);

        // Assert
        result.Should().BeTrue();

        var updated = await _context.Ammunition.FindAsync(ammo.Id);
        updated!.Name.Should().Be("Updated Name");
        updated.BallisticCoefficient.Should().Be(0.505m);
        updated.Manufacturer.Should().Be("Federal"); // Unchanged
    }

    [Fact]
    public async Task DeleteAmmoAsync_ShouldDeleteAmmo()
    {
        // Arrange
        var ammo = new Ammunition
        {
            UserId = _testUserId,
            Manufacturer = "Federal",
            Name = "To Delete",
            Caliber = ".308",
            Grain = 175
        };
        _context.Ammunition.Add(ammo);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteAmmoAsync(_testUserId, ammo.Id);

        // Assert
        result.Should().BeTrue();

        var deleted = await _context.Ammunition.FindAsync(ammo.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteLotAsync_ShouldDeleteLot()
    {
        // Arrange
        var ammo = new Ammunition { UserId = _testUserId, Manufacturer = "Federal", Name = "GMM", Caliber = ".308", Grain = 175 };
        _context.Ammunition.Add(ammo);
        await _context.SaveChangesAsync();

        var lot = new AmmoLot { AmmunitionId = ammo.Id, UserId = _testUserId, LotNumber = "LOT-001" };
        _context.AmmoLots.Add(lot);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteLotAsync(_testUserId, ammo.Id, lot.Id);

        // Assert
        result.Should().BeTrue();

        var deleted = await _context.AmmoLots.FindAsync(lot.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task Ammunition_DisplayName_ShouldCombineFields()
    {
        // Arrange
        var ammo = new Ammunition
        {
            UserId = _testUserId,
            Manufacturer = "Federal",
            Name = "Gold Medal Match",
            Caliber = ".308 Win",
            Grain = 175
        };
        _context.Ammunition.Add(ammo);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAmmoAsync(_testUserId, ammo.Id);

        // Assert
        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("Federal Gold Medal Match (.308 Win - 175gr)");
    }
}
