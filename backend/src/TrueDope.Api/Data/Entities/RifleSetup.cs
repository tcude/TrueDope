using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrueDope.Api.Data.Entities;

public class RifleSetup
{
    public int Id { get; set; }

    // Ownership
    [Required]
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    // Rifle info
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Manufacturer { get; set; }

    [MaxLength(100)]
    public string? Model { get; set; }

    [Required]
    [MaxLength(50)]
    public string Caliber { get; set; } = string.Empty;

    [Column(TypeName = "decimal(4,1)")]
    [Range(1, 50)]
    public decimal? BarrelLength { get; set; }

    [MaxLength(20)]
    public string? TwistRate { get; set; }

    // Optic info
    [MaxLength(100)]
    public string? ScopeMake { get; set; }

    [MaxLength(100)]
    public string? ScopeModel { get; set; }

    [Column(TypeName = "decimal(4,2)")]
    [Range(0, 5)]
    public decimal? ScopeHeight { get; set; }

    // Zero info
    [Range(25, 1000)]
    public int ZeroDistance { get; set; } = 100;

    public decimal? ZeroElevationClicks { get; set; }
    public decimal? ZeroWindageClicks { get; set; }

    // Ballistic data
    [Column(TypeName = "decimal(6,1)")]
    [Range(500, 5000)]
    public decimal? MuzzleVelocity { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    [Range(0.001, 2.0)]
    public decimal? BallisticCoefficient { get; set; }

    [MaxLength(5)]
    public string? DragModel { get; set; }  // "G1" or "G7"

    [MaxLength(2000)]
    public string? Notes { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<RangeSession> RangeSessions { get; set; } = new List<RangeSession>();
    public ICollection<Image> Images { get; set; } = new List<Image>();
}
