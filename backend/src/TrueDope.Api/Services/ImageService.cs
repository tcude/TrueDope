using Microsoft.EntityFrameworkCore;
using TrueDope.Api.Data;
using TrueDope.Api.Data.Entities;
using TrueDope.Api.DTOs.Images;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace TrueDope.Api.Services;

public class ImageService : IImageService
{
    private readonly ApplicationDbContext _context;
    private readonly IStorageService _storageService;
    private readonly ILogger<ImageService> _logger;

    private const string ImageBucket = "truedope-images";
    private const int ThumbnailWidth = 300;
    private const int ThumbnailHeight = 300;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "image/heic",
        "image/heif"
    };

    public ImageService(ApplicationDbContext context, IStorageService storageService, ILogger<ImageService> logger)
    {
        _context = context;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<ImageUploadResultDto> UploadImageAsync(
        string userId,
        ImageParentType parentType,
        int parentId,
        Stream fileStream,
        string fileName,
        string contentType)
    {
        // Validate content type
        if (!AllowedContentTypes.Contains(contentType))
            throw new ArgumentException($"Content type '{contentType}' is not allowed");

        // Validate parent exists and belongs to user
        await ValidateParentAsync(userId, parentType, parentId);

        // Generate unique file name
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        var uniqueFileName = $"{userId}/{parentType.ToString().ToLower()}/{parentId}/{Guid.NewGuid()}{fileExtension}";
        var thumbnailFileName = $"{userId}/{parentType.ToString().ToLower()}/{parentId}/thumb_{Guid.NewGuid()}.jpg";

        // Read file into memory for processing
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        var fileSize = memoryStream.Length;
        memoryStream.Position = 0;

        // Upload original
        await _storageService.UploadFileAsync(ImageBucket, uniqueFileName, memoryStream, contentType);

        // Generate and upload thumbnail
        memoryStream.Position = 0;
        await GenerateAndUploadThumbnailAsync(memoryStream, thumbnailFileName);

        // Create database record
        var image = new Data.Entities.Image
        {
            UserId = userId,
            FileName = uniqueFileName,
            OriginalFileName = fileName,
            ContentType = contentType,
            FileSize = fileSize,
            ThumbnailFileName = thumbnailFileName,
            IsProcessed = true
        };

        // Set parent based on type
        switch (parentType)
        {
            case ImageParentType.Rifle:
                image.RifleSetupId = parentId;
                break;
            case ImageParentType.Session:
                image.RangeSessionId = parentId;
                break;
            case ImageParentType.Group:
                image.GroupEntryId = parentId;
                break;
        }

        _context.Images.Add(image);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Uploaded image {ImageId} for user {UserId}", image.Id, userId);

        return new ImageUploadResultDto
        {
            Id = image.Id,
            Url = $"/api/images/{image.Id}",
            ThumbnailUrl = $"/api/images/{image.Id}/thumbnail"
        };
    }

    public async Task<(Stream Stream, string ContentType, string FileName)?> GetImageAsync(string userId, int imageId)
    {
        var image = await _context.Images
            .FirstOrDefaultAsync(i => i.Id == imageId && i.UserId == userId);

        if (image == null)
            return null;

        var stream = await _storageService.GetFileAsync(ImageBucket, image.FileName);
        if (stream == null)
            return null;

        return (stream, image.ContentType, image.OriginalFileName);
    }

    public async Task<(Stream Stream, string ContentType, string FileName)?> GetThumbnailAsync(string userId, int imageId)
    {
        var image = await _context.Images
            .FirstOrDefaultAsync(i => i.Id == imageId && i.UserId == userId);

        if (image == null || string.IsNullOrEmpty(image.ThumbnailFileName))
            return null;

        var stream = await _storageService.GetFileAsync(ImageBucket, image.ThumbnailFileName);
        if (stream == null)
        {
            // Fall back to original if thumbnail doesn't exist
            stream = await _storageService.GetFileAsync(ImageBucket, image.FileName);
            if (stream == null)
                return null;
            return (stream, image.ContentType, image.OriginalFileName);
        }

        return (stream, "image/jpeg", $"thumb_{image.OriginalFileName}");
    }

    public async Task<bool> UpdateImageAsync(string userId, int imageId, UpdateImageDto dto)
    {
        var image = await _context.Images
            .FirstOrDefaultAsync(i => i.Id == imageId && i.UserId == userId);

        if (image == null)
            return false;

        if (dto.Caption != null)
            image.Caption = dto.Caption;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteImageAsync(string userId, int imageId)
    {
        var image = await _context.Images
            .FirstOrDefaultAsync(i => i.Id == imageId && i.UserId == userId);

        if (image == null)
            return false;

        // Delete from storage
        await _storageService.DeleteFileAsync(ImageBucket, image.FileName);
        if (!string.IsNullOrEmpty(image.ThumbnailFileName))
            await _storageService.DeleteFileAsync(ImageBucket, image.ThumbnailFileName);

        // Delete from database
        _context.Images.Remove(image);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted image {ImageId} for user {UserId}", imageId, userId);

        return true;
    }

    private async Task ValidateParentAsync(string userId, ImageParentType parentType, int parentId)
    {
        var exists = parentType switch
        {
            ImageParentType.Rifle => await _context.RifleSetups
                .AnyAsync(r => r.Id == parentId && r.UserId == userId),
            ImageParentType.Session => await _context.RangeSessions
                .AnyAsync(s => s.Id == parentId && s.UserId == userId),
            ImageParentType.Group => await _context.GroupEntries
                .Include(g => g.RangeSession)
                .AnyAsync(g => g.Id == parentId && g.RangeSession.UserId == userId),
            _ => false
        };

        if (!exists)
            throw new ArgumentException($"{parentType} not found or does not belong to user");
    }

    private async Task GenerateAndUploadThumbnailAsync(Stream sourceStream, string thumbnailFileName)
    {
        try
        {
            using var image = await SixLabors.ImageSharp.Image.LoadAsync(sourceStream);

            // Resize maintaining aspect ratio
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(ThumbnailWidth, ThumbnailHeight),
                Mode = ResizeMode.Max
            }));

            using var thumbnailStream = new MemoryStream();
            await image.SaveAsJpegAsync(thumbnailStream, new JpegEncoder { Quality = 80 });
            thumbnailStream.Position = 0;

            await _storageService.UploadFileAsync(ImageBucket, thumbnailFileName, thumbnailStream, "image/jpeg");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate thumbnail for {FileName}", thumbnailFileName);
            // Don't fail the upload if thumbnail generation fails
        }
    }
}
