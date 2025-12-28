using System.ComponentModel.DataAnnotations;

namespace TrueDope.Api.DTOs.Sessions;

// ==================== Hole Position ====================

public class HolePosition
{
    /// <summary>
    /// X coordinate in inches from POA. Positive = right of aim point.
    /// </summary>
    [Required]
    [Range(-10, 10)]
    public decimal X { get; set; }

    /// <summary>
    /// Y coordinate in inches from POA. Positive = above aim point.
    /// </summary>
    [Required]
    [Range(-10, 10)]
    public decimal Y { get; set; }
}

// ==================== Response DTO ====================

public class GroupMeasurementDto
{
    public int Id { get; set; }
    public int GroupEntryId { get; set; }

    // Hole data
    public List<HolePosition> HolePositions { get; set; } = new();
    public decimal BulletDiameter { get; set; }

    // Core metrics (inches)
    public decimal? ExtremeSpread { get; set; }
    public decimal? MeanRadius { get; set; }
    public decimal? HorizontalSpread { get; set; }
    public decimal? VerticalSpread { get; set; }

    // Statistical metrics
    public decimal? RadialStdDev { get; set; }
    public decimal? HorizontalStdDev { get; set; }
    public decimal? VerticalStdDev { get; set; }
    public decimal? Cep50 { get; set; }

    // POI offset from POA
    public decimal? PoiOffsetX { get; set; }
    public decimal? PoiOffsetY { get; set; }

    // MOA conversions (computed from inches + distance)
    public decimal? ExtremeSpreadMoa { get; set; }
    public decimal? MeanRadiusMoa { get; set; }

    // Metadata
    public string CalibrationMethod { get; set; } = string.Empty;
    public decimal? MeasurementConfidence { get; set; }

    // Images
    public ImageDto? OriginalImage { get; set; }
    public ImageDto? AnnotatedImage { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ==================== Create DTO ====================

public class CreateGroupMeasurementDto
{
    /// <summary>
    /// Array of hole positions relative to POA (0,0). Minimum 2 required.
    /// </summary>
    [Required]
    [MinLength(2, ErrorMessage = "At least 2 hole positions are required")]
    public List<HolePosition> HolePositions { get; set; } = new();

    /// <summary>
    /// Bullet diameter in inches (e.g., 0.308 for .308 Win, 0.224 for .223)
    /// </summary>
    [Required]
    [Range(0.1, 1.0, ErrorMessage = "Bullet diameter must be between 0.1 and 1.0 inches")]
    public decimal BulletDiameter { get; set; }

    /// <summary>
    /// How calibration was performed: manual, fiducial, qrCode, gridDetect
    /// </summary>
    [Required]
    public string CalibrationMethod { get; set; } = "manual";

    /// <summary>
    /// Detection confidence (0-1). Optional for manual entry.
    /// </summary>
    [Range(0, 1)]
    public decimal? MeasurementConfidence { get; set; }
}

// ==================== Update DTO ====================

public class UpdateGroupMeasurementDto
{
    /// <summary>
    /// Updated hole positions. If provided, all metrics will be recalculated.
    /// </summary>
    [MinLength(2, ErrorMessage = "At least 2 hole positions are required")]
    public List<HolePosition>? HolePositions { get; set; }

    /// <summary>
    /// Updated bullet diameter. If provided with HolePositions, triggers recalculation.
    /// </summary>
    [Range(0.1, 1.0, ErrorMessage = "Bullet diameter must be between 0.1 and 1.0 inches")]
    public decimal? BulletDiameter { get; set; }

    /// <summary>
    /// Updated calibration method.
    /// </summary>
    public string? CalibrationMethod { get; set; }

    /// <summary>
    /// Updated confidence value.
    /// </summary>
    [Range(0, 1)]
    public decimal? MeasurementConfidence { get; set; }
}
