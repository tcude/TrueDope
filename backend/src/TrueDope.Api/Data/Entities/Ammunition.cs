using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrueDope.Api.Data.Entities;

public class Ammunition
{
    public int Id { get; set; }

    // Ownership
    [Required]
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    // Identification
    [Required]
    [MaxLength(100)]
    public string Manufacturer { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Caliber { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(6,1)")]
    [Range(1, 1000)]
    public decimal Grain { get; set; }

    // Additional info
    [MaxLength(50)]
    public string? BulletType { get; set; }

    [Column(TypeName = "decimal(8,4)")]
    [Range(0, 100)]
    public decimal? CostPerRound { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    [Range(0.001, 2.0)]
    public decimal? BallisticCoefficient { get; set; }

    [MaxLength(5)]
    public string? DragModel { get; set; }  // "G1" or "G7"

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<AmmoLot> AmmoLots { get; set; } = new List<AmmoLot>();
    public ICollection<ChronoSession> ChronoSessions { get; set; } = new List<ChronoSession>();
    public ICollection<GroupEntry> GroupEntries { get; set; } = new List<GroupEntry>();
    public ICollection<DopeEntry> DopeEntries { get; set; } = new List<DopeEntry>();

    // Computed
    [NotMapped]
    public string DisplayName => $"{Manufacturer} {Name} ({Caliber} - {Grain}gr)";
}
