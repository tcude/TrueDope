using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using TrueDope.Api.Configuration;
using TrueDope.Api.Data;
using TrueDope.Api.DTOs.Admin;

namespace TrueDope.Api.Services;

public interface IImageMaintenanceService
{
    Task<ImageMaintenanceStatsDto> GetImageStatsAsync();
    Task<ThumbnailJobStatusDto> StartThumbnailRegenerationAsync();
    Task<ThumbnailJobStatusDto?> GetThumbnailJobStatusAsync(string jobId);
    Task<List<OrphanedImageDto>> GetOrphanedImagesAsync();
    Task<OrphanCleanupResultDto> DeleteOrphanedImagesAsync();
}

public class ImageMaintenanceService : IImageMaintenanceService
{
    private readonly ApplicationDbContext _context;
    private readonly IStorageService _storageService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ImageMaintenanceService> _logger;
    private readonly ImageSettings _settings;

    private const string ImageBucket = "truedope-images";

    // In-memory job tracking (for single-instance deployment)
    // For multi-instance, consider using Redis or database
    private static readonly ConcurrentDictionary<string, ThumbnailJobStatusDto> _jobs = new();

    public ImageMaintenanceService(
        ApplicationDbContext context,
        IStorageService storageService,
        IServiceScopeFactory scopeFactory,
        ILogger<ImageMaintenanceService> logger,
        IOptions<ImageSettings> settings)
    {
        _context = context;
        _storageService = storageService;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<ImageMaintenanceStatsDto> GetImageStatsAsync()
    {
        _logger.LogInformation("Getting image maintenance statistics");

        // Get counts from database
        var totalImages = await _context.Images.CountAsync();
        var missingThumbnails = await _context.Images
            .CountAsync(i => string.IsNullOrEmpty(i.ThumbnailFileName) || !i.IsProcessed);

        // Get storage size from MinIO
        var storageSizeBytes = await _storageService.GetBucketSizeAsync(ImageBucket);

        // Get orphaned file count
        var orphanedFiles = await GetOrphanedImagesAsync();

        return new ImageMaintenanceStatsDto
        {
            TotalImages = totalImages,
            StorageSizeBytes = storageSizeBytes,
            StorageSizeFormatted = FormatFileSize(storageSizeBytes),
            MissingThumbnails = missingThumbnails,
            OrphanedFileCount = orphanedFiles.Count
        };
    }

    public async Task<ThumbnailJobStatusDto> StartThumbnailRegenerationAsync()
    {
        // Check if there's already a running job
        var runningJob = _jobs.Values.FirstOrDefault(j => j.Status == ThumbnailJobState.Running);
        if (runningJob != null)
        {
            _logger.LogWarning("Thumbnail regeneration already in progress: {JobId}", runningJob.JobId);
            return runningJob;
        }

        var jobId = Guid.NewGuid().ToString("N")[..8];
        var totalImages = await _context.Images.CountAsync();

        var jobStatus = new ThumbnailJobStatusDto
        {
            JobId = jobId,
            Status = ThumbnailJobState.Running,
            TotalImages = totalImages,
            ProcessedImages = 0,
            FailedImages = 0,
            StartedAt = DateTime.UtcNow
        };

        _jobs[jobId] = jobStatus;

        _logger.LogInformation("Starting thumbnail regeneration job {JobId} for {TotalImages} images", jobId, totalImages);

        // Start background processing
        _ = Task.Run(() => ProcessThumbnailRegenerationAsync(jobId));

        return jobStatus;
    }

    public Task<ThumbnailJobStatusDto?> GetThumbnailJobStatusAsync(string jobId)
    {
        _jobs.TryGetValue(jobId, out var status);
        return Task.FromResult(status);
    }

    public async Task<List<OrphanedImageDto>> GetOrphanedImagesAsync()
    {
        _logger.LogInformation("Scanning for orphaned images");

        // Get all objects from MinIO
        var storageObjects = await _storageService.ListObjectsAsync(ImageBucket);
        var storageObjectNames = storageObjects.ToDictionary(o => o.ObjectName, o => o);

        // Get all referenced file names from database
        var referencedFiles = await _context.Images
            .Select(i => new { i.FileName, i.ThumbnailFileName })
            .ToListAsync();

        var referencedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in referencedFiles)
        {
            if (!string.IsNullOrEmpty(file.FileName))
                referencedSet.Add(file.FileName);
            if (!string.IsNullOrEmpty(file.ThumbnailFileName))
                referencedSet.Add(file.ThumbnailFileName);
        }

        // Find orphaned files (in storage but not in database)
        var orphanedFiles = storageObjectNames
            .Where(kvp => !referencedSet.Contains(kvp.Key))
            .Select(kvp => new OrphanedImageDto
            {
                ObjectName = kvp.Key,
                Size = kvp.Value.Size,
                SizeFormatted = FormatFileSize(kvp.Value.Size),
                LastModified = kvp.Value.LastModified
            })
            .OrderByDescending(o => o.Size)
            .ToList();

        _logger.LogInformation("Found {Count} orphaned files totaling {Size}",
            orphanedFiles.Count, FormatFileSize(orphanedFiles.Sum(o => o.Size)));

        return orphanedFiles;
    }

    public async Task<OrphanCleanupResultDto> DeleteOrphanedImagesAsync()
    {
        _logger.LogInformation("Starting orphaned image cleanup");

        var orphanedFiles = await GetOrphanedImagesAsync();
        var result = new OrphanCleanupResultDto();
        long totalFreedBytes = 0;

        foreach (var orphan in orphanedFiles)
        {
            try
            {
                await _storageService.DeleteFileAsync(ImageBucket, orphan.ObjectName);
                result.DeletedCount++;
                totalFreedBytes += orphan.Size;
                _logger.LogInformation("Deleted orphaned file: {ObjectName}", orphan.ObjectName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete orphaned file: {ObjectName}", orphan.ObjectName);
                result.Errors.Add($"Failed to delete {orphan.ObjectName}: {ex.Message}");
            }
        }

        result.FreedBytes = totalFreedBytes;
        result.FreedSizeFormatted = FormatFileSize(totalFreedBytes);

        _logger.LogInformation("Orphan cleanup completed: deleted {Count} files, freed {Size}",
            result.DeletedCount, result.FreedSizeFormatted);

        return result;
    }

    private async Task ProcessThumbnailRegenerationAsync(string jobId)
    {
        try
        {
            // Create a new scope for database operations
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
            var imageSettings = scope.ServiceProvider.GetRequiredService<IOptions<ImageSettings>>().Value;

            var images = await context.Images
                .OrderBy(i => i.Id)
                .ToListAsync();

            foreach (var image in images)
            {
                try
                {
                    await RegenerateThumbnailForImageAsync(image, storageService, imageSettings);

                    if (_jobs.TryGetValue(jobId, out var status))
                    {
                        status.ProcessedImages++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to regenerate thumbnail for image {ImageId}", image.Id);

                    if (_jobs.TryGetValue(jobId, out var status))
                    {
                        status.FailedImages++;
                        status.ProcessedImages++;
                    }
                }
            }

            // Mark job as completed
            if (_jobs.TryGetValue(jobId, out var finalStatus))
            {
                finalStatus.Status = ThumbnailJobState.Completed;
                finalStatus.CompletedAt = DateTime.UtcNow;
            }

            _logger.LogInformation("Thumbnail regeneration job {JobId} completed", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Thumbnail regeneration job {JobId} failed", jobId);

            if (_jobs.TryGetValue(jobId, out var status))
            {
                status.Status = ThumbnailJobState.Failed;
                status.CompletedAt = DateTime.UtcNow;
                status.ErrorMessage = ex.Message;
            }
        }
    }

    private async Task RegenerateThumbnailForImageAsync(
        Data.Entities.Image image,
        IStorageService storageService,
        ImageSettings settings)
    {
        // Get the original image from storage
        var imageStream = await storageService.GetFileAsync(ImageBucket, image.FileName);
        if (imageStream == null)
        {
            _logger.LogWarning("Original image not found in storage: {FileName}", image.FileName);
            return;
        }

        try
        {
            // Determine thumbnail file name
            var thumbnailFileName = image.ThumbnailFileName;
            if (string.IsNullOrEmpty(thumbnailFileName))
            {
                // Generate thumbnail name based on original
                var directory = Path.GetDirectoryName(image.FileName) ?? "";
                var nameWithoutExt = Path.GetFileNameWithoutExtension(image.FileName);
                thumbnailFileName = Path.Combine(directory, $"{nameWithoutExt}_thumb.jpg").Replace("\\", "/");
            }

            // Generate thumbnail
            using var loadedImage = await Image.LoadAsync(imageStream);
            var size = settings.ThumbnailSize;

            loadedImage.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(size, size),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center
            }));

            using var thumbnailStream = new MemoryStream();
            await loadedImage.SaveAsJpegAsync(thumbnailStream, new JpegEncoder { Quality = settings.ThumbnailQuality });
            thumbnailStream.Position = 0;

            // Upload thumbnail
            await storageService.UploadFileAsync(ImageBucket, thumbnailFileName, thumbnailStream, "image/jpeg");

            // Update database if thumbnail name changed
            if (image.ThumbnailFileName != thumbnailFileName || !image.IsProcessed)
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var dbImage = await context.Images.FindAsync(image.Id);
                if (dbImage != null)
                {
                    dbImage.ThumbnailFileName = thumbnailFileName;
                    dbImage.IsProcessed = true;
                    await context.SaveChangesAsync();
                }
            }

            _logger.LogDebug("Regenerated thumbnail for image {ImageId}", image.Id);
        }
        finally
        {
            imageStream.Dispose();
        }
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 0) return "0 B";

        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
