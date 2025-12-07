using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrueDope.Api.Data.Entities;

public class VelocityReading
{
    public int Id { get; set; }

    // Parent
    [Required]
    public int ChronoSessionId { get; set; }
    public ChronoSession ChronoSession { get; set; } = null!;

    // The data
    [Required]
    [Range(1, 100)]
    public int ShotNumber { get; set; }

    [Required]
    [Column(TypeName = "decimal(6,1)")]
    [Range(500, 5000)]
    public decimal Velocity { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
