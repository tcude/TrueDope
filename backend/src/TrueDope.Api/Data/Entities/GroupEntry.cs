using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrueDope.Api.Data.Entities;

public class GroupEntry
{
    public int Id { get; set; }

    // Parent
    [Required]
    public int RangeSessionId { get; set; }
    public RangeSession RangeSession { get; set; } = null!;

    // Ammunition (optional - may be same as chrono or different)
    public int? AmmunitionId { get; set; }
    public Ammunition? Ammunition { get; set; }

    public int? AmmoLotId { get; set; }
    public AmmoLot? AmmoLot { get; set; }

    // Group info
    [Required]
    [Range(1, 20)]
    public int GroupNumber { get; set; }

    [Required]
    [Range(25, 2500)]
    public int Distance { get; set; }

    [Required]
    [Range(1, 25)]
    public int NumberOfShots { get; set; }

    // Measurements (optional - user may have one or both)
    [Column(TypeName = "decimal(6,3)")]
    [Range(0.01, 20)]
    public decimal? GroupSizeMoa { get; set; }

    [Column(TypeName = "decimal(6,3)")]
    [Range(0.01, 10)]
    public decimal? MeanRadiusMoa { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Images of this specific group
    public ICollection<Image> Images { get; set; } = new List<Image>();

    // Optional 1:1 detailed measurement data
    public GroupMeasurement? Measurement { get; set; }

    // Computed: Group size in inches
    [NotMapped]
    public decimal? GroupSizeInches => GroupSizeMoa.HasValue
        ? (GroupSizeMoa.Value * Distance * 1.047m) / 100m
        : null;
}
