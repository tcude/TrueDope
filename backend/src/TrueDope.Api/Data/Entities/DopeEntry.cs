using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrueDope.Api.Data.Entities;

public class DopeEntry
{
    public int Id { get; set; }

    // Parent
    [Required]
    public int RangeSessionId { get; set; }
    public RangeSession RangeSession { get; set; } = null!;

    // The data
    [Required]
    [Range(1, 2500)]
    public int Distance { get; set; }

    [Required]
    [Column(TypeName = "decimal(8,3)")]
    [Range(-50, 50)]
    public decimal ElevationMils { get; set; }

    [Required]
    [Column(TypeName = "decimal(8,3)")]
    [Range(-20, 20)]
    public decimal WindageMils { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Computed conversions
    [NotMapped]
    public decimal ElevationInches => (ElevationMils * Distance * 3.6m) / 100m;

    [NotMapped]
    public decimal WindageInches => (WindageMils * Distance * 3.6m) / 100m;

    // MOA conversions (1 MOA = 0.2909 MIL)
    [NotMapped]
    public decimal ElevationMoa => ElevationMils / 0.2909m;

    [NotMapped]
    public decimal WindageMoa => WindageMils / 0.2909m;
}
