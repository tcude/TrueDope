using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrueDope.Api.Data.Entities;

public class GroupMeasurement
{
    public int Id { get; set; }

    // Parent relationship (1:1 with GroupEntry)
    [Required]
    public int GroupEntryId { get; set; }
    public GroupEntry GroupEntry { get; set; } = null!;

    // Hole position data (stored as JSONB)
    // Array of {x, y} coordinates in inches relative to POA (0,0)
    // Positive X = right of POA, Positive Y = above POA
    [Required]
    [Column(TypeName = "jsonb")]
    public string HolePositionsJson { get; set; } = "[]";

    // Bullet diameter used for edge-to-edge calculations (inches)
    [Required]
    [Column(TypeName = "decimal(5,4)")]
    [Range(0.1, 1.0)]
    public decimal BulletDiameter { get; set; }

    // ==================== Computed Metrics (inches) ====================
    // CTC = Center-to-Center (what most shooters compare)
    // ETE = Edge-to-Edge (CTC + bullet diameter, actual physical size)

    // Extreme spread: max center-to-center distance
    [Column(TypeName = "decimal(6,4)")]
    public decimal? ExtremeSpreadCtc { get; set; }

    // Extreme spread edge-to-edge (CTC + bullet diameter)
    [Column(TypeName = "decimal(6,4)")]
    public decimal? ExtremeSpreadEte { get; set; }

    // Mean radius: average distance from centroid
    [Column(TypeName = "decimal(6,4)")]
    public decimal? MeanRadius { get; set; }

    // Horizontal spread center-to-center: max X - min X
    [Column(TypeName = "decimal(6,4)")]
    public decimal? HorizontalSpreadCtc { get; set; }

    // Horizontal spread edge-to-edge (CTC + bullet diameter)
    [Column(TypeName = "decimal(6,4)")]
    public decimal? HorizontalSpreadEte { get; set; }

    // Vertical spread center-to-center: max Y - min Y
    [Column(TypeName = "decimal(6,4)")]
    public decimal? VerticalSpreadCtc { get; set; }

    // Vertical spread edge-to-edge (CTC + bullet diameter)
    [Column(TypeName = "decimal(6,4)")]
    public decimal? VerticalSpreadEte { get; set; }

    // Standard deviations
    [Column(TypeName = "decimal(6,5)")]
    public decimal? RadialStdDev { get; set; }

    [Column(TypeName = "decimal(6,5)")]
    public decimal? HorizontalStdDev { get; set; }

    [Column(TypeName = "decimal(6,5)")]
    public decimal? VerticalStdDev { get; set; }

    // CEP50: 50% Circular Error Probable (radius containing 50% of shots)
    [Column(TypeName = "decimal(6,4)")]
    public decimal? Cep50 { get; set; }

    // POI offset from POA (group centroid position)
    // Negative X = left of POA, Negative Y = below POA
    [Column(TypeName = "decimal(6,4)")]
    public decimal? PoiOffsetX { get; set; }

    [Column(TypeName = "decimal(6,4)")]
    public decimal? PoiOffsetY { get; set; }

    // ==================== Calibration Metadata ====================

    [Required]
    public CalibrationMethod CalibrationMethod { get; set; } = CalibrationMethod.Manual;

    // Detection/measurement confidence (0.0 - 1.0)
    // null for manual entry, populated for auto-detection
    [Column(TypeName = "decimal(3,2)")]
    [Range(0, 1)]
    public decimal? MeasurementConfidence { get; set; }

    // ==================== Image References ====================
    // These reference Image entities (stored in MinIO)

    public int? OriginalImageId { get; set; }
    public Image? OriginalImage { get; set; }

    public int? AnnotatedImageId { get; set; }
    public Image? AnnotatedImage { get; set; }

    // ==================== Audit ====================

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum CalibrationMethod
{
    Manual = 0,      // User set reference distance manually
    Fiducial = 1,    // TrueDope target with ArUco markers (future)
    QrCode = 2,      // QR code provided calibration data (future)
    GridDetect = 3   // Grid pattern auto-detected (future)
}
