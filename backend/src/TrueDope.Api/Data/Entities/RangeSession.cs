using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrueDope.Api.Data.Entities;

public class RangeSession
{
    public int Id { get; set; }

    // Ownership
    [Required]
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    // Required: When and with what rifle
    [Required]
    public DateTime SessionDate { get; set; }
    public TimeSpan? SessionTime { get; set; }

    [Required]
    public int RifleSetupId { get; set; }
    public RifleSetup RifleSetup { get; set; } = null!;

    // Location (optional, either saved or manual)
    public int? SavedLocationId { get; set; }
    public SavedLocation? SavedLocation { get; set; }

    [Column(TypeName = "decimal(10,8)")]
    [Range(-90, 90)]
    public decimal? Latitude { get; set; }

    [Column(TypeName = "decimal(11,8)")]
    [Range(-180, 180)]
    public decimal? Longitude { get; set; }

    [MaxLength(100)]
    public string? LocationName { get; set; }

    // Conditions (all optional - can auto-fetch or manual entry)
    [Column(TypeName = "decimal(5,2)")]
    [Range(-50, 120)]
    public decimal? Temperature { get; set; }

    [Range(0, 100)]
    public int? Humidity { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    [Range(0, 100)]
    public decimal? WindSpeed { get; set; }

    [Range(0, 359)]
    public int? WindDirection { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    [Range(20, 35)]
    public decimal? Pressure { get; set; }

    [Column(TypeName = "decimal(7,1)")]
    public decimal? DensityAltitude { get; set; }

    // Notes
    [MaxLength(2000)]
    public string? Notes { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Child collections (optional - presence determines "session type")
    public ICollection<DopeEntry> DopeEntries { get; set; } = new List<DopeEntry>();
    public ChronoSession? ChronoSession { get; set; }
    public ICollection<GroupEntry> GroupEntries { get; set; } = new List<GroupEntry>();
    public ICollection<Image> Images { get; set; } = new List<Image>();

    // Computed properties
    [NotMapped]
    public string WindDirectionCardinal => WindDirection switch
    {
        >= 0 and < 23 => "N",
        >= 23 and < 68 => "NE",
        >= 68 and < 113 => "E",
        >= 113 and < 158 => "SE",
        >= 158 and < 203 => "S",
        >= 203 and < 248 => "SW",
        >= 248 and < 293 => "W",
        >= 293 and < 338 => "NW",
        >= 338 => "N",
        _ => ""
    };

    [NotMapped]
    public bool HasDopeData => DopeEntries.Any();

    [NotMapped]
    public bool HasChronoData => ChronoSession != null;

    [NotMapped]
    public bool HasGroupData => GroupEntries.Any();
}
