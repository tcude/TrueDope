using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TrueDope.Api.Configuration;
using TrueDope.Api.Data;
using TrueDope.Api.Data.Entities;
using TrueDope.Api.DTOs.Images;
using TrueDope.Api.Services;

namespace TrueDope.Api.Tests.Services;

public class ImageServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IStorageService> _storageServiceMock;
    private readonly Mock<ILogger<ImageService>> _loggerMock;
    private readonly ImageService _imageService;
    private readonly ImageSettings _settings;
    private readonly string _testUserId = "test-user-id";

    public ImageServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Setup mocks
        _storageServiceMock = new Mock<IStorageService>();
        _loggerMock = new Mock<ILogger<ImageService>>();

        // Setup settings
        _settings = new ImageSettings
        {
            MaxFileSizeBytes = 20 * 1024 * 1024,
            MaxImagesPerEntity = 10,
            ThumbnailSize = 300,
            FullImageMaxDimension = 4096,
            JpegQuality = 85,
            ThumbnailQuality = 80,
            PreSignedUrlExpirySeconds = 3600
        };
        var settingsOptions = Options.Create(_settings);

        // Setup storage service mock to return success
        _storageServiceMock
            .Setup(s => s.UploadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync((string bucket, string objectName, Stream stream, string contentType) => objectName);

        _storageServiceMock
            .Setup(s => s.GetPreSignedUrlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((string bucket, string objectName, int expiry) => $"https://minio/{bucket}/{objectName}");

        _imageService = new ImageService(_context, _storageServiceMock.Object, _loggerMock.Object, settingsOptions);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private async Task<RifleSetup> CreateTestRifle()
    {
        var rifle = new RifleSetup
        {
            Name = "Test Rifle",
            Caliber = "6.5 Creedmoor",
            UserId = _testUserId
        };
        _context.RifleSetups.Add(rifle);
        await _context.SaveChangesAsync();
        return rifle;
    }

    private async Task<RangeSession> CreateTestSession()
    {
        var rifle = await CreateTestRifle();
        var session = new RangeSession
        {
            SessionDate = DateTime.UtcNow,
            RifleSetupId = rifle.Id,
            UserId = _testUserId
        };
        _context.RangeSessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }

    #region Validation Tests

    [Fact]
    public async Task UploadImageAsync_InvalidContentType_ThrowsArgumentException()
    {
        // Arrange
        var rifle = await CreateTestRifle();
        using var stream = new MemoryStream(new byte[100]);

        // Act & Assert
        var act = () => _imageService.UploadImageAsync(
            _testUserId,
            ImageParentType.Rifle,
            rifle.Id,
            stream,
            "test.gif",
            "image/gif"
        );

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not allowed*");
    }

    [Fact]
    public async Task UploadImageAsync_ParentNotFound_ThrowsArgumentException()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[100]);

        // Act & Assert
        var act = () => _imageService.UploadImageAsync(
            _testUserId,
            ImageParentType.Rifle,
            99999, // Non-existent rifle
            stream,
            "test.jpg",
            "image/jpeg"
        );

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task UploadImageAsync_ParentBelongsToOtherUser_ThrowsArgumentException()
    {
        // Arrange
        var rifle = await CreateTestRifle();
        using var stream = new MemoryStream(new byte[100]);

        // Act & Assert
        var act = () => _imageService.UploadImageAsync(
            "other-user-id", // Different user
            ImageParentType.Rifle,
            rifle.Id,
            stream,
            "test.jpg",
            "image/jpeg"
        );

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not found or does not belong*");
    }

    [Fact]
    public async Task UploadImageAsync_ExceedsMaxImages_ThrowsArgumentException()
    {
        // Arrange
        var rifle = await CreateTestRifle();

        // Add maximum images
        for (int i = 0; i < _settings.MaxImagesPerEntity; i++)
        {
            _context.Images.Add(new Image
            {
                UserId = _testUserId,
                RifleSetupId = rifle.Id,
                FileName = $"test{i}.jpg",
                OriginalFileName = $"test{i}.jpg",
                ContentType = "image/jpeg",
                FileSize = 1000,
                DisplayOrder = i
            });
        }
        await _context.SaveChangesAsync();

        using var stream = new MemoryStream(new byte[100]);

        // Act & Assert
        var act = () => _imageService.UploadImageAsync(
            _testUserId,
            ImageParentType.Rifle,
            rifle.Id,
            stream,
            "test.jpg",
            "image/jpeg"
        );

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"*Maximum of {_settings.MaxImagesPerEntity} images*");
    }

    #endregion

    #region GetImageCountForEntityAsync Tests

    [Fact]
    public async Task GetImageCountForEntityAsync_ReturnsCorrectCount()
    {
        // Arrange
        var rifle = await CreateTestRifle();

        _context.Images.AddRange(
            new Image { UserId = _testUserId, RifleSetupId = rifle.Id, FileName = "1.jpg", OriginalFileName = "1.jpg", ContentType = "image/jpeg", FileSize = 100 },
            new Image { UserId = _testUserId, RifleSetupId = rifle.Id, FileName = "2.jpg", OriginalFileName = "2.jpg", ContentType = "image/jpeg", FileSize = 100 },
            new Image { UserId = _testUserId, RifleSetupId = rifle.Id, FileName = "3.jpg", OriginalFileName = "3.jpg", ContentType = "image/jpeg", FileSize = 100 }
        );
        await _context.SaveChangesAsync();

        // Act
        var count = await _imageService.GetImageCountForEntityAsync(_testUserId, ImageParentType.Rifle, rifle.Id);

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task GetImageCountForEntityAsync_DifferentUser_ReturnsZero()
    {
        // Arrange
        var rifle = await CreateTestRifle();

        _context.Images.Add(new Image
        {
            UserId = _testUserId,
            RifleSetupId = rifle.Id,
            FileName = "1.jpg",
            OriginalFileName = "1.jpg",
            ContentType = "image/jpeg",
            FileSize = 100
        });
        await _context.SaveChangesAsync();

        // Act
        var count = await _imageService.GetImageCountForEntityAsync("other-user-id", ImageParentType.Rifle, rifle.Id);

        // Assert
        count.Should().Be(0);
    }

    #endregion

    #region GetImagesForEntityAsync Tests

    [Fact]
    public async Task GetImagesForEntityAsync_ReturnsImagesOrderedByDisplayOrder()
    {
        // Arrange
        var rifle = await CreateTestRifle();

        _context.Images.AddRange(
            new Image { UserId = _testUserId, RifleSetupId = rifle.Id, FileName = "3.jpg", OriginalFileName = "3.jpg", ContentType = "image/jpeg", FileSize = 100, DisplayOrder = 2 },
            new Image { UserId = _testUserId, RifleSetupId = rifle.Id, FileName = "1.jpg", OriginalFileName = "1.jpg", ContentType = "image/jpeg", FileSize = 100, DisplayOrder = 0 },
            new Image { UserId = _testUserId, RifleSetupId = rifle.Id, FileName = "2.jpg", OriginalFileName = "2.jpg", ContentType = "image/jpeg", FileSize = 100, DisplayOrder = 1 }
        );
        await _context.SaveChangesAsync();

        // Act
        var images = await _imageService.GetImagesForEntityAsync(_testUserId, ImageParentType.Rifle, rifle.Id);

        // Assert
        images.Should().HaveCount(3);
        images[0].OriginalFileName.Should().Be("1.jpg");
        images[1].OriginalFileName.Should().Be("2.jpg");
        images[2].OriginalFileName.Should().Be("3.jpg");
    }

    #endregion

    #region DeleteImageAsync Tests

    [Fact]
    public async Task DeleteImageAsync_ExistingImage_ReturnsTrue()
    {
        // Arrange
        var rifle = await CreateTestRifle();
        var image = new Image
        {
            UserId = _testUserId,
            RifleSetupId = rifle.Id,
            FileName = "test.jpg",
            OriginalFileName = "test.jpg",
            ThumbnailFileName = "test_thumb.jpg",
            ContentType = "image/jpeg",
            FileSize = 100
        };
        _context.Images.Add(image);
        await _context.SaveChangesAsync();

        // Act
        var result = await _imageService.DeleteImageAsync(_testUserId, image.Id);

        // Assert
        result.Should().BeTrue();
        var deleted = await _context.Images.FindAsync(image.Id);
        deleted.Should().BeNull();

        _storageServiceMock.Verify(s => s.DeleteFileAsync(It.IsAny<string>(), "test.jpg"), Times.Once);
        _storageServiceMock.Verify(s => s.DeleteFileAsync(It.IsAny<string>(), "test_thumb.jpg"), Times.Once);
    }

    [Fact]
    public async Task DeleteImageAsync_NonExistingImage_ReturnsFalse()
    {
        // Act
        var result = await _imageService.DeleteImageAsync(_testUserId, 99999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteImageAsync_WrongUser_ReturnsFalse()
    {
        // Arrange
        var rifle = await CreateTestRifle();
        var image = new Image
        {
            UserId = _testUserId,
            RifleSetupId = rifle.Id,
            FileName = "test.jpg",
            OriginalFileName = "test.jpg",
            ContentType = "image/jpeg",
            FileSize = 100
        };
        _context.Images.Add(image);
        await _context.SaveChangesAsync();

        // Act
        var result = await _imageService.DeleteImageAsync("other-user-id", image.Id);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region BulkDeleteImagesAsync Tests

    [Fact]
    public async Task BulkDeleteImagesAsync_DeletesMultipleImages()
    {
        // Arrange
        var rifle = await CreateTestRifle();
        var images = new[]
        {
            new Image { UserId = _testUserId, RifleSetupId = rifle.Id, FileName = "1.jpg", OriginalFileName = "1.jpg", ContentType = "image/jpeg", FileSize = 100 },
            new Image { UserId = _testUserId, RifleSetupId = rifle.Id, FileName = "2.jpg", OriginalFileName = "2.jpg", ContentType = "image/jpeg", FileSize = 100 },
            new Image { UserId = _testUserId, RifleSetupId = rifle.Id, FileName = "3.jpg", OriginalFileName = "3.jpg", ContentType = "image/jpeg", FileSize = 100 }
        };
        _context.Images.AddRange(images);
        await _context.SaveChangesAsync();

        var ids = images.Select(i => i.Id).ToList();

        // Act
        var result = await _imageService.BulkDeleteImagesAsync(_testUserId, new BulkDeleteDto { ImageIds = ids });

        // Assert
        result.DeletedCount.Should().Be(3);
        result.FailedIds.Should().BeEmpty();
    }

    #endregion

    #region ReorderImagesAsync Tests

    [Fact]
    public async Task ReorderImagesAsync_UpdatesDisplayOrder()
    {
        // Arrange
        var rifle = await CreateTestRifle();
        var images = new[]
        {
            new Image { UserId = _testUserId, RifleSetupId = rifle.Id, FileName = "1.jpg", OriginalFileName = "1.jpg", ContentType = "image/jpeg", FileSize = 100, DisplayOrder = 0 },
            new Image { UserId = _testUserId, RifleSetupId = rifle.Id, FileName = "2.jpg", OriginalFileName = "2.jpg", ContentType = "image/jpeg", FileSize = 100, DisplayOrder = 1 },
            new Image { UserId = _testUserId, RifleSetupId = rifle.Id, FileName = "3.jpg", OriginalFileName = "3.jpg", ContentType = "image/jpeg", FileSize = 100, DisplayOrder = 2 }
        };
        _context.Images.AddRange(images);
        await _context.SaveChangesAsync();

        // Reverse the order
        var newOrder = new List<int> { images[2].Id, images[1].Id, images[0].Id };

        // Act
        var result = await _imageService.ReorderImagesAsync(_testUserId, new ReorderImagesDto
        {
            RifleSetupId = rifle.Id,
            ImageIds = newOrder
        });

        // Assert
        result.Should().BeTrue();

        var reorderedImages = await _context.Images
            .Where(i => i.RifleSetupId == rifle.Id)
            .OrderBy(i => i.DisplayOrder)
            .ToListAsync();

        reorderedImages[0].FileName.Should().Be("3.jpg");
        reorderedImages[1].FileName.Should().Be("2.jpg");
        reorderedImages[2].FileName.Should().Be("1.jpg");
    }

    [Fact]
    public async Task ReorderImagesAsync_NoParentSpecified_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => _imageService.ReorderImagesAsync(_testUserId, new ReorderImagesDto
        {
            ImageIds = new List<int> { 1, 2, 3 }
        });

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Must specify*");
    }

    #endregion

    #region UpdateImageAsync Tests

    [Fact]
    public async Task UpdateImageAsync_UpdatesCaption()
    {
        // Arrange
        var rifle = await CreateTestRifle();
        var image = new Image
        {
            UserId = _testUserId,
            RifleSetupId = rifle.Id,
            FileName = "test.jpg",
            OriginalFileName = "test.jpg",
            ContentType = "image/jpeg",
            FileSize = 100,
            Caption = "Original caption"
        };
        _context.Images.Add(image);
        await _context.SaveChangesAsync();

        // Act
        var result = await _imageService.UpdateImageAsync(_testUserId, image.Id, new UpdateImageDto
        {
            Caption = "Updated caption"
        });

        // Assert
        result.Should().BeTrue();
        var updated = await _context.Images.FindAsync(image.Id);
        updated!.Caption.Should().Be("Updated caption");
    }

    [Fact]
    public async Task UpdateImageAsync_NonExistingImage_ReturnsFalse()
    {
        // Act
        var result = await _imageService.UpdateImageAsync(_testUserId, 99999, new UpdateImageDto
        {
            Caption = "New caption"
        });

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
