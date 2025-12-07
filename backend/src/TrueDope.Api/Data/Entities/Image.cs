using System.ComponentModel.DataAnnotations;

namespace TrueDope.Api.Data.Entities;

public class Image
{
    public int Id { get; set; }

    // Ownership
    [Required]
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    // File info
    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;  // MinIO object key

    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    [Required]
    public long FileSize { get; set; }

    [MaxLength(500)]
    public string? ThumbnailFileName { get; set; }  // MinIO key for thumbnail

    [MaxLength(500)]
    public string? Caption { get; set; }

    // Processing status
    public bool IsProcessed { get; set; } = false;

    // Polymorphic parents (only ONE should be set)
    public int? RifleSetupId { get; set; }
    public RifleSetup? RifleSetup { get; set; }

    public int? RangeSessionId { get; set; }
    public RangeSession? RangeSession { get; set; }

    public int? GroupEntryId { get; set; }
    public GroupEntry? GroupEntry { get; set; }

    // Audit
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
