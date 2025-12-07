using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrueDope.Api.Data.Entities;

public class ChronoSession
{
    public int Id { get; set; }

    // Parent (one-to-one with RangeSession)
    [Required]
    public int RangeSessionId { get; set; }
    public RangeSession RangeSession { get; set; } = null!;

    // Ammunition used (required for chrono data)
    [Required]
    public int AmmunitionId { get; set; }
    public Ammunition Ammunition { get; set; } = null!;

    public int? AmmoLotId { get; set; }
    public AmmoLot? AmmoLot { get; set; }

    // Session conditions
    [Column(TypeName = "decimal(5,1)")]
    [Range(32, 200)]
    public decimal? BarrelTemperature { get; set; }

    // Computed stats (calculated from VelocityReadings, stored for query performance)
    public int NumberOfRounds { get; set; }

    [Column(TypeName = "decimal(6,1)")]
    public decimal? AverageVelocity { get; set; }

    [Column(TypeName = "decimal(6,1)")]
    public decimal? HighVelocity { get; set; }

    [Column(TypeName = "decimal(6,1)")]
    public decimal? LowVelocity { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal? StandardDeviation { get; set; }

    [Column(TypeName = "decimal(6,1)")]
    public decimal? ExtremeSpread { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Child readings
    public ICollection<VelocityReading> VelocityReadings { get; set; } = new List<VelocityReading>();
}
