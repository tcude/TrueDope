using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TrueDope.Api.Configuration;
using TrueDope.Api.Controllers;
using TrueDope.Api.DTOs.Images;
using TrueDope.Api.Services;

namespace TrueDope.Api.Tests.Controllers;

public class ImagesControllerTests
{
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly Mock<ILogger<ImagesController>> _loggerMock;
    private readonly Mock<IOptions<JwtSettings>> _jwtSettingsMock;
    private readonly ImagesController _controller;
    private readonly string _testUserId = "test-user-id";

    public ImagesControllerTests()
    {
        _imageServiceMock = new Mock<IImageService>();
        _loggerMock = new Mock<ILogger<ImagesController>>();
        _jwtSettingsMock = new Mock<IOptions<JwtSettings>>();
        _jwtSettingsMock.Setup(x => x.Value).Returns(new JwtSettings
        {
            SecretKey = "TestSecretKeyThatIsAtLeast32Characters!",
            Issuer = "TestIssuer",
            Audience = "TestAudience"
        });
        _controller = new ImagesController(_imageServiceMock.Object, _loggerMock.Object, _jwtSettingsMock.Object);

        // Setup user claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private IFormFile CreateMockFormFile(string fileName = "test.jpg", string contentType = "image/jpeg", long size = 1000)
    {
        var content = new byte[size];
        var stream = new MemoryStream(content);
        var formFile = new Mock<IFormFile>();
        formFile.Setup(f => f.FileName).Returns(fileName);
        formFile.Setup(f => f.ContentType).Returns(contentType);
        formFile.Setup(f => f.Length).Returns(size);
        formFile.Setup(f => f.OpenReadStream()).Returns(stream);
        return formFile.Object;
    }

    #region Upload Tests

    [Fact]
    public async Task UploadRifleImage_ValidFile_ReturnsCreated()
    {
        // Arrange
        var rifleId = 1;
        var file = CreateMockFormFile();
        var expectedResult = new ImageUploadResultDto
        {
            Id = 1,
            Url = "https://test/image.jpg",
            ThumbnailUrl = "https://test/thumb.jpg",
            OriginalFileName = "test.jpg",
            DisplayOrder = 0
        };

        _imageServiceMock
            .Setup(s => s.UploadImageAsync(_testUserId, ImageParentType.Rifle, rifleId, It.IsAny<Stream>(), "test.jpg", "image/jpeg"))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.UploadRifleImage(rifleId, file);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task UploadRifleImage_NoFile_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.UploadRifleImage(1, null!);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task UploadRifleImage_FileTooLarge_ReturnsBadRequest()
    {
        // Arrange
        var file = CreateMockFormFile(size: 25 * 1024 * 1024); // 25MB

        // Act
        var result = await _controller.UploadRifleImage(1, file);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task UploadRifleImage_RifleNotFound_ReturnsNotFound()
    {
        // Arrange
        var file = CreateMockFormFile();
        _imageServiceMock
            .Setup(s => s.UploadImageAsync(_testUserId, ImageParentType.Rifle, 99999, It.IsAny<Stream>(), "test.jpg", "image/jpeg"))
            .ThrowsAsync(new ArgumentException("Rifle not found or does not belong to user"));

        // Act
        var result = await _controller.UploadRifleImage(99999, file);

        // Assert
        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UploadSessionImage_ValidFile_ReturnsCreated()
    {
        // Arrange
        var sessionId = 1;
        var file = CreateMockFormFile();
        var expectedResult = new ImageUploadResultDto
        {
            Id = 1,
            Url = "https://test/image.jpg",
            ThumbnailUrl = "https://test/thumb.jpg",
            OriginalFileName = "test.jpg",
            DisplayOrder = 0
        };

        _imageServiceMock
            .Setup(s => s.UploadImageAsync(_testUserId, ImageParentType.Session, sessionId, It.IsAny<Stream>(), "test.jpg", "image/jpeg"))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.UploadSessionImage(sessionId, file);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task UploadGroupImage_ValidFile_ReturnsCreated()
    {
        // Arrange
        var groupId = 1;
        var file = CreateMockFormFile();
        var expectedResult = new ImageUploadResultDto
        {
            Id = 1,
            Url = "https://test/image.jpg",
            ThumbnailUrl = "https://test/thumb.jpg",
            OriginalFileName = "test.jpg",
            DisplayOrder = 0
        };

        _imageServiceMock
            .Setup(s => s.UploadImageAsync(_testUserId, ImageParentType.Group, groupId, It.IsAny<Stream>(), "test.jpg", "image/jpeg"))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.UploadGroupImage(groupId, file);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task UploadRifleImage_UnsupportedFormat_ReturnsBadRequest()
    {
        // Arrange
        var file = CreateMockFormFile();
        _imageServiceMock
            .Setup(s => s.UploadImageAsync(_testUserId, ImageParentType.Rifle, 1, It.IsAny<Stream>(), "test.jpg", "image/jpeg"))
            .ThrowsAsync(new NotSupportedException("HEIC format detected but conversion is not yet supported."));

        // Act
        var result = await _controller.UploadRifleImage(1, file);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(400);
    }

    #endregion

    #region Bulk Upload Tests

    [Fact]
    public async Task BulkUploadRifleImages_ValidFiles_ReturnsOk()
    {
        // Arrange
        var rifleId = 1;
        var files = new List<IFormFile> { CreateMockFormFile("test1.jpg"), CreateMockFormFile("test2.jpg") };
        var expectedResult = new BulkUploadResultDto
        {
            Uploaded = new List<ImageUploadResultDto>
            {
                new() { Id = 1, OriginalFileName = "test1.jpg" },
                new() { Id = 2, OriginalFileName = "test2.jpg" }
            },
            Errors = new List<string>()
        };

        _imageServiceMock
            .Setup(s => s.BulkUploadImagesAsync(_testUserId, ImageParentType.Rifle, rifleId, It.IsAny<IEnumerable<(Stream, string, string)>>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.BulkUploadRifleImages(rifleId, files);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task BulkUploadRifleImages_NoFiles_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.BulkUploadRifleImages(1, new List<IFormFile>());

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(400);
    }

    #endregion

    #region Get Image Tests

    [Fact]
    public async Task GetImageDetails_ExistingImage_ReturnsOk()
    {
        // Arrange
        var imageId = 1;
        var expectedImage = new ImageDetailDto
        {
            Id = imageId,
            Url = "https://test/image.jpg",
            ThumbnailUrl = "https://test/thumb.jpg",
            OriginalFileName = "test.jpg",
            ContentType = "image/jpeg",
            FileSize = 1000,
            DisplayOrder = 0,
            IsProcessed = true,
            UploadedAt = DateTime.UtcNow
        };

        _imageServiceMock
            .Setup(s => s.GetImageDetailAsync(_testUserId, imageId))
            .ReturnsAsync(expectedImage);

        // Act
        var result = await _controller.GetImageDetails(imageId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetImageDetails_NonExistingImage_ReturnsNotFound()
    {
        // Arrange
        _imageServiceMock
            .Setup(s => s.GetImageDetailAsync(_testUserId, 99999))
            .ReturnsAsync((ImageDetailDto?)null);

        // Act
        var result = await _controller.GetImageDetails(99999);

        // Assert
        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetImage_ExistingImage_ReturnsFile()
    {
        // Arrange
        var imageId = 1;
        var stream = new MemoryStream(new byte[100]);
        _imageServiceMock
            .Setup(s => s.GetImageAsync(_testUserId, imageId))
            .ReturnsAsync((stream, "image/jpeg", "test.jpg"));

        // Act
        var result = await _controller.GetImage(imageId);

        // Assert
        result.Should().BeOfType<FileStreamResult>();
    }

    [Fact]
    public async Task GetImage_NonExistingImage_ReturnsNotFound()
    {
        // Arrange
        _imageServiceMock
            .Setup(s => s.GetImageAsync(_testUserId, 99999))
            .ReturnsAsync((ValueTuple<Stream, string, string>?)null);

        // Act
        var result = await _controller.GetImage(99999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetThumbnail_ExistingImage_ReturnsFile()
    {
        // Arrange
        var imageId = 1;
        var stream = new MemoryStream(new byte[50]);
        _imageServiceMock
            .Setup(s => s.GetThumbnailAsync(_testUserId, imageId))
            .ReturnsAsync((stream, "image/jpeg", "thumb_test.jpg"));

        // Act
        var result = await _controller.GetThumbnail(imageId);

        // Assert
        result.Should().BeOfType<FileStreamResult>();
    }

    [Fact]
    public async Task GetRifleImages_ReturnsImages()
    {
        // Arrange
        var rifleId = 1;
        var images = new List<ImageDetailDto>
        {
            new() { Id = 1, OriginalFileName = "test1.jpg" },
            new() { Id = 2, OriginalFileName = "test2.jpg" }
        };

        _imageServiceMock
            .Setup(s => s.GetImagesForEntityAsync(_testUserId, ImageParentType.Rifle, rifleId))
            .ReturnsAsync(images);

        // Act
        var result = await _controller.GetRifleImages(rifleId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetSessionImages_ReturnsImages()
    {
        // Arrange
        var sessionId = 1;
        var images = new List<ImageDetailDto>
        {
            new() { Id = 1, OriginalFileName = "target.jpg" }
        };

        _imageServiceMock
            .Setup(s => s.GetImagesForEntityAsync(_testUserId, ImageParentType.Session, sessionId))
            .ReturnsAsync(images);

        // Act
        var result = await _controller.GetSessionImages(sessionId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task UpdateImage_ExistingImage_ReturnsOk()
    {
        // Arrange
        var imageId = 1;
        var dto = new UpdateImageDto { Caption = "Updated caption" };

        _imageServiceMock
            .Setup(s => s.UpdateImageAsync(_testUserId, imageId, dto))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateImage(imageId, dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateImage_NonExistingImage_ReturnsNotFound()
    {
        // Arrange
        var dto = new UpdateImageDto { Caption = "Updated caption" };

        _imageServiceMock
            .Setup(s => s.UpdateImageAsync(_testUserId, 99999, dto))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateImage(99999, dto);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ReorderImages_ValidRequest_ReturnsOk()
    {
        // Arrange
        var dto = new ReorderImagesDto
        {
            RifleSetupId = 1,
            ImageIds = new List<int> { 3, 1, 2 }
        };

        _imageServiceMock
            .Setup(s => s.ReorderImagesAsync(_testUserId, dto))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ReorderImages(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ReorderImages_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var dto = new ReorderImagesDto
        {
            ImageIds = new List<int> { 1, 2, 3 }
        };

        _imageServiceMock
            .Setup(s => s.ReorderImagesAsync(_testUserId, dto))
            .ThrowsAsync(new ArgumentException("Must specify either rangeSessionId, rifleSetupId, or groupEntryId"));

        // Act
        var result = await _controller.ReorderImages(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteImage_ExistingImage_ReturnsOk()
    {
        // Arrange
        var imageId = 1;
        _imageServiceMock
            .Setup(s => s.DeleteImageAsync(_testUserId, imageId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteImage(imageId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteImage_NonExistingImage_ReturnsNotFound()
    {
        // Arrange
        _imageServiceMock
            .Setup(s => s.DeleteImageAsync(_testUserId, 99999))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteImage(99999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task BulkDeleteImages_ReturnsResult()
    {
        // Arrange
        var dto = new BulkDeleteDto { ImageIds = new List<int> { 1, 2, 3 } };
        var expectedResult = new BulkDeleteResultDto
        {
            DeletedCount = 3,
            FailedIds = new List<int>()
        };

        _imageServiceMock
            .Setup(s => s.BulkDeleteImagesAsync(_testUserId, dto))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.BulkDeleteImages(dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task BulkDeleteImages_PartialFailure_ReturnsResultWithFailures()
    {
        // Arrange
        var dto = new BulkDeleteDto { ImageIds = new List<int> { 1, 2, 99999 } };
        var expectedResult = new BulkDeleteResultDto
        {
            DeletedCount = 2,
            FailedIds = new List<int> { 99999 }
        };

        _imageServiceMock
            .Setup(s => s.BulkDeleteImagesAsync(_testUserId, dto))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.BulkDeleteImages(dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    #endregion
}
