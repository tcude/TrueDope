using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrueDope.Api.Data.Entities;

/// <summary>
/// Admin-managed shared/pre-seeded locations (popular shooting ranges)
/// </summary>
public class SharedLocation
{
    public int Id { get; set; }

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

    // Additional metadata for discovery
    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(50)]
    public string? State { get; set; }

    [MaxLength(50)]
    public string Country { get; set; } = "USA";

    [MaxLength(255)]
    public string? Website { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    // Admin fields
    public bool IsActive { get; set; } = true;

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Created by admin
    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;
    public User CreatedByUser { get; set; } = null!;
}
