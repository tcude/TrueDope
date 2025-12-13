using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrueDope.Api.Configuration;
using TrueDope.Api.Data;
using TrueDope.Api.Data.Entities;
using TrueDope.Api.DTOs.Images;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using ImageMagick;

namespace TrueDope.Api.Services;

public class ImageService : IImageService
{
    private readonly ApplicationDbContext _context;
    private readonly IStorageService _storageService;
    private readonly ILogger<ImageService> _logger;
    private readonly ImageSettings _settings;

    private const string ImageBucket = "truedope-images";

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/heic",
        "image/heif"
    };

    public ImageService(
        ApplicationDbContext context,
        IStorageService storageService,
        ILogger<ImageService> logger,
        IOptions<ImageSettings> settings)
    {
        _context = context;
        _storageService = storageService;
        _logger = logger;
        _settings = settings.Value;
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
            throw new ArgumentException($"Content type '{contentType}' is not allowed. Supported formats: JPEG, PNG, HEIC");

        // Validate parent exists and belongs to user
        await ValidateParentAsync(userId, parentType, parentId);

        // Check image count limit
        var currentCount = await GetImageCountForEntityAsync(userId, parentType, parentId);
        if (currentCount >= _settings.MaxImagesPerEntity)
            throw new ArgumentException($"Maximum of {_settings.MaxImagesPerEntity} images per {parentType.ToString().ToLower()} allowed");

        // Generate unique file names
        var fileExtension = IsHeicFormat(contentType) ? ".jpg" : Path.GetExtension(fileName).ToLowerInvariant();
        var uniqueId = Guid.NewGuid();
        var uniqueFileName = $"{userId}/{parentType.ToString().ToLower()}/{parentId}/{uniqueId}{fileExtension}";
        var thumbnailFileName = $"{userId}/{parentType.ToString().ToLower()}/{parentId}/{uniqueId}_thumb.jpg";

        // Read file into memory for processing
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        var fileSize = memoryStream.Length;

        // Validate file size
        if (fileSize > _settings.MaxFileSizeBytes)
            throw new ArgumentException($"File size exceeds maximum of {_settings.MaxFileSizeBytes / 1024 / 1024}MB");

        memoryStream.Position = 0;

        // Process and upload image
        var (processedStream, processedContentType) = await ProcessImageAsync(memoryStream, contentType);
        await _storageService.UploadFileAsync(ImageBucket, uniqueFileName, processedStream, processedContentType);

        // Generate and upload thumbnail
        processedStream.Position = 0;
        await GenerateAndUploadThumbnailAsync(processedStream, thumbnailFileName);

        // Calculate display order (next in sequence)
        var displayOrder = currentCount;

        // Create database record
        var image = new Data.Entities.Image
        {
            UserId = userId,
            FileName = uniqueFileName,
            OriginalFileName = fileName,
            ContentType = processedContentType,
            FileSize = fileSize,
            ThumbnailFileName = thumbnailFileName,
            DisplayOrder = displayOrder,
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

        // Return API proxy URLs instead of pre-signed URLs
        // This ensures images are accessible regardless of MinIO network configuration
        return new ImageUploadResultDto
        {
            Id = image.Id,
            Url = $"/api/images/{image.Id}",
            ThumbnailUrl = $"/api/images/{image.Id}/thumbnail",
            OriginalFileName = fileName,
            DisplayOrder = displayOrder
        };
    }

    public async Task<BulkUploadResultDto> BulkUploadImagesAsync(
        string userId,
        ImageParentType parentType,
        int parentId,
        IEnumerable<(Stream Stream, string FileName, string ContentType)> files)
    {
        var result = new BulkUploadResultDto();
        var fileList = files.ToList();

        // Check total count won't exceed limit
        var currentCount = await GetImageCountForEntityAsync(userId, parentType, parentId);
        var remainingSlots = _settings.MaxImagesPerEntity - currentCount;

        if (fileList.Count > remainingSlots)
        {
            result.Errors.Add($"Can only upload {remainingSlots} more images (limit: {_settings.MaxImagesPerEntity})");
            // Only process up to the limit
            fileList = fileList.Take(remainingSlots).ToList();
        }

        foreach (var (stream, fileName, contentType) in fileList)
        {
            try
            {
                var uploadResult = await UploadImageAsync(userId, parentType, parentId, stream, fileName, contentType);
                result.Uploaded.Add(uploadResult);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to upload file {FileName}", fileName);
                result.Errors.Add($"{fileName}: {ex.Message}");
            }
        }

        return result;
    }

    public async Task<ImageDetailDto?> GetImageDetailAsync(string userId, int imageId)
    {
        var image = await _context.Images
            .FirstOrDefaultAsync(i => i.Id == imageId && i.UserId == userId);

        if (image == null)
            return null;

        // Return API proxy URLs instead of pre-signed URLs
        return new ImageDetailDto
        {
            Id = image.Id,
            Url = $"/api/images/{image.Id}",
            ThumbnailUrl = $"/api/images/{image.Id}/thumbnail",
            OriginalFileName = image.OriginalFileName,
            ContentType = image.ContentType,
            FileSize = image.FileSize,
            Caption = image.Caption,
            DisplayOrder = image.DisplayOrder,
            IsProcessed = image.IsProcessed,
            UploadedAt = image.UploadedAt
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

    public async Task<List<ImageDetailDto>> GetImagesForEntityAsync(string userId, ImageParentType parentType, int parentId)
    {
        var query = _context.Images
            .Where(i => i.UserId == userId);

        query = parentType switch
        {
            ImageParentType.Rifle => query.Where(i => i.RifleSetupId == parentId),
            ImageParentType.Session => query.Where(i => i.RangeSessionId == parentId),
            ImageParentType.Group => query.Where(i => i.GroupEntryId == parentId),
            _ => throw new ArgumentException($"Invalid parent type: {parentType}")
        };

        var images = await query
            .OrderBy(i => i.DisplayOrder)
            .ThenBy(i => i.UploadedAt)
            .ToListAsync();

        // Return API proxy URLs instead of pre-signed URLs
        return images.Select(image => new ImageDetailDto
        {
            Id = image.Id,
            Url = $"/api/images/{image.Id}",
            ThumbnailUrl = $"/api/images/{image.Id}/thumbnail",
            OriginalFileName = image.OriginalFileName,
            ContentType = image.ContentType,
            FileSize = image.FileSize,
            Caption = image.Caption,
            DisplayOrder = image.DisplayOrder,
            IsProcessed = image.IsProcessed,
            UploadedAt = image.UploadedAt
        }).ToList();
    }

    public async Task<bool> UpdateImageAsync(string userId, int imageId, UpdateImageDto dto)
    {
        var image = await _context.Images
            .FirstOrDefaultAsync(i => i.Id == imageId && i.UserId == userId);

        if (image == null)
            return false;

        if (dto.Caption != null)
            image.Caption = dto.Caption;

        if (dto.DisplayOrder.HasValue)
            image.DisplayOrder = dto.DisplayOrder.Value;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReorderImagesAsync(string userId, ReorderImagesDto dto)
    {
        // Determine parent type and id
        ImageParentType? parentType = null;
        int? parentId = null;

        if (dto.RangeSessionId.HasValue)
        {
            parentType = ImageParentType.Session;
            parentId = dto.RangeSessionId.Value;
        }
        else if (dto.RifleSetupId.HasValue)
        {
            parentType = ImageParentType.Rifle;
            parentId = dto.RifleSetupId.Value;
        }
        else if (dto.GroupEntryId.HasValue)
        {
            parentType = ImageParentType.Group;
            parentId = dto.GroupEntryId.Value;
        }

        if (!parentType.HasValue || !parentId.HasValue)
            throw new ArgumentException("Must specify either rangeSessionId, rifleSetupId, or groupEntryId");

        // Validate parent exists and belongs to user
        await ValidateParentAsync(userId, parentType.Value, parentId.Value);

        // Get all images for this entity
        var images = await GetImagesQueryForParent(parentType.Value, parentId.Value)
            .Where(i => i.UserId == userId)
            .ToListAsync();

        // Validate all imageIds belong to this entity
        var imageIdSet = images.Select(i => i.Id).ToHashSet();
        if (!dto.ImageIds.All(id => imageIdSet.Contains(id)))
            throw new ArgumentException("Some image IDs do not belong to this entity");

        // Update display order
        for (int i = 0; i < dto.ImageIds.Count; i++)
        {
            var image = images.First(img => img.Id == dto.ImageIds[i]);
            image.DisplayOrder = i;
        }

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

    public async Task<BulkDeleteResultDto> BulkDeleteImagesAsync(string userId, BulkDeleteDto dto)
    {
        var result = new BulkDeleteResultDto();

        foreach (var imageId in dto.ImageIds)
        {
            var deleted = await DeleteImageAsync(userId, imageId);
            if (deleted)
                result.DeletedCount++;
            else
                result.FailedIds.Add(imageId);
        }

        return result;
    }

    public async Task<int> GetImageCountForEntityAsync(string userId, ImageParentType parentType, int parentId)
    {
        var query = _context.Images.Where(i => i.UserId == userId);

        return parentType switch
        {
            ImageParentType.Rifle => await query.CountAsync(i => i.RifleSetupId == parentId),
            ImageParentType.Session => await query.CountAsync(i => i.RangeSessionId == parentId),
            ImageParentType.Group => await query.CountAsync(i => i.GroupEntryId == parentId),
            _ => 0
        };
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

    private IQueryable<Data.Entities.Image> GetImagesQueryForParent(ImageParentType parentType, int parentId)
    {
        return parentType switch
        {
            ImageParentType.Rifle => _context.Images.Where(i => i.RifleSetupId == parentId),
            ImageParentType.Session => _context.Images.Where(i => i.RangeSessionId == parentId),
            ImageParentType.Group => _context.Images.Where(i => i.GroupEntryId == parentId),
            _ => throw new ArgumentException($"Invalid parent type: {parentType}")
        };
    }

    private static bool IsHeicFormat(string contentType)
    {
        return contentType.Equals("image/heic", StringComparison.OrdinalIgnoreCase) ||
               contentType.Equals("image/heif", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<(MemoryStream Stream, string ContentType)> ProcessImageAsync(Stream sourceStream, string contentType)
    {
        var outputStream = new MemoryStream();

        try
        {
            // Handle HEIC/HEIF format using Magick.NET
            if (IsHeicFormat(contentType))
            {
                return await ConvertHeicToJpegAsync(sourceStream);
            }

            // Use ImageSharp for standard formats (JPEG, PNG)
            using var image = await SixLabors.ImageSharp.Image.LoadAsync(sourceStream);

            // Resize if larger than max dimension
            if (image.Width > _settings.FullImageMaxDimension || image.Height > _settings.FullImageMaxDimension)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(_settings.FullImageMaxDimension, _settings.FullImageMaxDimension),
                    Mode = ResizeMode.Max
                }));
            }

            // Save as JPEG for consistent output
            await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = _settings.JpegQuality });
            outputStream.Position = 0;

            return (outputStream, "image/jpeg");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process image");
            throw new InvalidOperationException("Failed to process image. Please ensure the file is a valid image.", ex);
        }
    }

    private async Task<(MemoryStream Stream, string ContentType)> ConvertHeicToJpegAsync(Stream sourceStream)
    {
        var outputStream = new MemoryStream();

        try
        {
            // Copy to memory stream first for Magick.NET
            using var inputStream = new MemoryStream();
            await sourceStream.CopyToAsync(inputStream);
            inputStream.Position = 0;

            // Use Magick.NET to convert HEIC to JPEG
            using var magickImage = new MagickImage(inputStream);

            // Auto-orient based on EXIF data (important for photos from phones)
            magickImage.AutoOrient();

            // Resize if larger than max dimension
            if (magickImage.Width > _settings.FullImageMaxDimension || magickImage.Height > _settings.FullImageMaxDimension)
            {
                var geometry = new MagickGeometry((uint)_settings.FullImageMaxDimension, (uint)_settings.FullImageMaxDimension)
                {
                    IgnoreAspectRatio = false,
                    Greater = true // Only resize if image is larger
                };
                magickImage.Resize(geometry);
            }

            // Set JPEG quality
            magickImage.Quality = (uint)_settings.JpegQuality;
            magickImage.Format = MagickFormat.Jpeg;

            // Write to output stream
            await magickImage.WriteAsync(outputStream);
            outputStream.Position = 0;

            _logger.LogInformation("Successfully converted HEIC image to JPEG");

            return (outputStream, "image/jpeg");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert HEIC image");
            throw new InvalidOperationException("Failed to convert HEIC image. The file may be corrupted or unsupported.", ex);
        }
    }

    private async Task GenerateAndUploadThumbnailAsync(Stream sourceStream, string thumbnailFileName)
    {
        try
        {
            using var image = await SixLabors.ImageSharp.Image.LoadAsync(sourceStream);

            // Resize to thumbnail size, maintaining aspect ratio and center cropping to square
            var size = _settings.ThumbnailSize;
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(size, size),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center
            }));

            using var thumbnailStream = new MemoryStream();
            await image.SaveAsJpegAsync(thumbnailStream, new JpegEncoder { Quality = _settings.ThumbnailQuality });
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
