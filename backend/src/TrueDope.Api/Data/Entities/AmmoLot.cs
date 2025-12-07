using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrueDope.Api.Data.Entities;

public class AmmoLot
{
    public int Id { get; set; }

    // Parent
    [Required]
    public int AmmunitionId { get; set; }
    public Ammunition Ammunition { get; set; } = null!;

    // Ownership
    [Required]
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    // Lot info
    [Required]
    [MaxLength(50)]
    public string LotNumber { get; set; } = string.Empty;

    public DateTime? PurchaseDate { get; set; }

    [Range(1, 100000)]
    public int? InitialQuantity { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    [Range(0, 100000)]
    public decimal? PurchasePrice { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<ChronoSession> ChronoSessions { get; set; } = new List<ChronoSession>();
    public ICollection<GroupEntry> GroupEntries { get; set; } = new List<GroupEntry>();

    // Computed
    [NotMapped]
    public string DisplayName => $"{Ammunition?.Manufacturer} {Ammunition?.Name} - Lot: {LotNumber}";

    [NotMapped]
    public decimal? CostPerRound => (InitialQuantity.HasValue && PurchasePrice.HasValue && InitialQuantity > 0)
        ? PurchasePrice.Value / InitialQuantity.Value
        : null;
}
