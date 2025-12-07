using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrueDope.Api.Data.Entities;

public class SavedLocation
{
    public int Id { get; set; }

    // Ownership
    [Required]
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    // Location data
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(10,8)")]
    [Range(-90, 90)]
    public decimal Latitude { get; set; }

    [Required]
    [Column(TypeName = "decimal(11,8)")]
    [Range(-180, 180)]
    public decimal Longitude { get; set; }

    [Column(TypeName = "decimal(7,1)")]
    [Range(-1000, 30000)]
    public decimal? Altitude { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<RangeSession> RangeSessions { get; set; } = new List<RangeSession>();
}
