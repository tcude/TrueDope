using System.ComponentModel.DataAnnotations;

namespace TrueDope.Api.DTOs.Images;

public class ImageDetailDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? Caption { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsProcessed { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class ImageSummaryDto
{
    public int Id { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public int DisplayOrder { get; set; }
}

public class ImageUploadResultDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public class BulkUploadResultDto
{
    public List<ImageUploadResultDto> Uploaded { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class UpdateImageDto
{
    [MaxLength(500)]
    public string? Caption { get; set; }

    public int? DisplayOrder { get; set; }
}

public class BulkDeleteDto
{
    [Required]
    public List<int> ImageIds { get; set; } = new();
}

public class BulkDeleteResultDto
{
    public int DeletedCount { get; set; }
    public List<int> FailedIds { get; set; } = new();
}

public class ReorderImagesDto
{
    public int? RangeSessionId { get; set; }
    public int? RifleSetupId { get; set; }
    public int? GroupEntryId { get; set; }

    [Required]
    public List<int> ImageIds { get; set; } = new();
}

public enum ImageParentType
{
    Rifle,
    Session,
    Group
}
