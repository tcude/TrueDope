namespace TrueDope.Api.DTOs.Admin;

public class ImageMaintenanceStatsDto
{
    public int TotalImages { get; set; }
    public long StorageSizeBytes { get; set; }
    public string StorageSizeFormatted { get; set; } = string.Empty;
    public int MissingThumbnails { get; set; }
    public int OrphanedFileCount { get; set; }
}

public class OrphanedImageDto
{
    public string ObjectName { get; set; } = string.Empty;
    public long Size { get; set; }
    public string SizeFormatted { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
}

public class ThumbnailJobStatusDto
{
    public string JobId { get; set; } = string.Empty;
    public ThumbnailJobState Status { get; set; }
    public int TotalImages { get; set; }
    public int ProcessedImages { get; set; }
    public int FailedImages { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum ThumbnailJobState
{
    Pending,
    Running,
    Completed,
    Failed
}

public class OrphanCleanupResultDto
{
    public int DeletedCount { get; set; }
    public long FreedBytes { get; set; }
    public string FreedSizeFormatted { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}
