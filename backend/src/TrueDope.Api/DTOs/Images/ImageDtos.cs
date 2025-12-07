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
    public bool IsProcessed { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class ImageUploadResultDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
}

public class UpdateImageDto
{
    [MaxLength(500)]
    public string? Caption { get; set; }
}

public enum ImageParentType
{
    Rifle,
    Session,
    Group
}
