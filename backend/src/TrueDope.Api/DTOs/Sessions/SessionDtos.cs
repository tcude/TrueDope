using System.ComponentModel.DataAnnotations;

namespace TrueDope.Api.DTOs.Sessions;

// ==================== List/Summary DTOs ====================

public class SessionListDto
{
    public int Id { get; set; }
    public DateTime SessionDate { get; set; }
    public TimeSpan? SessionTime { get; set; }
    public RifleSummaryDto Rifle { get; set; } = null!;
    public string? LocationName { get; set; }
    public decimal? Temperature { get; set; }
    public bool HasDopeData { get; set; }
    public bool HasChronoData { get; set; }
    public bool HasGroupData { get; set; }
    public int DopeEntryCount { get; set; }
    public int VelocityReadingCount { get; set; }
    public int GroupEntryCount { get; set; }
    public int ImageCount { get; set; }
    public DateTime CreatedAt { get; set; }

    // Chrono summary data for list display
    public string? AmmunitionName { get; set; }
    public decimal? AverageVelocity { get; set; }
    public decimal? StandardDeviation { get; set; }
    public decimal? ExtremeSpread { get; set; }
}

public class RifleSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Caliber { get; set; } = string.Empty;
}

// ==================== Detail DTOs ====================

public class SessionDetailDto
{
    public int Id { get; set; }
    public DateTime SessionDate { get; set; }
    public TimeSpan? SessionTime { get; set; }
    public RifleSummaryDto Rifle { get; set; } = null!;

    // Location
    public LocationSummaryDto? SavedLocation { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? LocationName { get; set; }

    // Conditions
    public decimal? Temperature { get; set; }
    public int? Humidity { get; set; }
    public decimal? WindSpeed { get; set; }
    public int? WindDirection { get; set; }
    public string? WindDirectionCardinal { get; set; }
    public decimal? Pressure { get; set; }
    public decimal? DensityAltitude { get; set; }

    public string? Notes { get; set; }

    // Child data
    public List<DopeEntryDto> DopeEntries { get; set; } = new();
    public ChronoSessionDto? ChronoSession { get; set; }
    public List<GroupEntryDto> GroupEntries { get; set; } = new();
    public List<ImageDto> Images { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class LocationSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ==================== DOPE DTOs ====================

public class DopeEntryDto
{
    public int Id { get; set; }
    public int Distance { get; set; }
    public decimal ElevationMils { get; set; }
    public decimal WindageMils { get; set; }
    public decimal ElevationInches { get; set; }
    public decimal WindageInches { get; set; }
    public decimal ElevationMoa { get; set; }
    public decimal WindageMoa { get; set; }
    public string? Notes { get; set; }
    public AmmoSummaryDto? Ammunition { get; set; }
    public AmmoLotSummaryDto? AmmoLot { get; set; }
}

public class CreateDopeEntryDto
{
    [Required]
    [Range(1, 2500)]
    public int Distance { get; set; }

    [Required]
    [Range(-50, 50)]
    public decimal ElevationMils { get; set; }

    [Required]
    [Range(-20, 20)]
    public decimal WindageMils { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public int? AmmunitionId { get; set; }
    public int? AmmoLotId { get; set; }
}

public class UpdateDopeEntryDto
{
    [Range(-50, 50)]
    public decimal? ElevationMils { get; set; }

    [Range(-20, 20)]
    public decimal? WindageMils { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public int? AmmunitionId { get; set; }
    public int? AmmoLotId { get; set; }
}

// ==================== Chrono DTOs ====================

public class ChronoSessionDto
{
    public int Id { get; set; }
    public AmmoSummaryDto Ammunition { get; set; } = null!;
    public AmmoLotSummaryDto? AmmoLot { get; set; }
    public decimal? BarrelTemperature { get; set; }
    public int NumberOfRounds { get; set; }
    public decimal? AverageVelocity { get; set; }
    public decimal? HighVelocity { get; set; }
    public decimal? LowVelocity { get; set; }
    public decimal? StandardDeviation { get; set; }
    public decimal? ExtremeSpread { get; set; }
    public string? Notes { get; set; }
    public List<VelocityReadingDto> VelocityReadings { get; set; } = new();
}

public class AmmoSummaryDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Caliber { get; set; } = string.Empty;
    public decimal Grain { get; set; }
}

public class AmmoLotSummaryDto
{
    public int Id { get; set; }
    public string LotNumber { get; set; } = string.Empty;
}

public class CreateChronoSessionDto
{
    [Required]
    public int AmmunitionId { get; set; }

    public int? AmmoLotId { get; set; }

    [Range(32, 200)]
    public decimal? BarrelTemperature { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public List<CreateVelocityReadingDto> VelocityReadings { get; set; } = new();
}

public class UpdateChronoSessionDto
{
    public int? AmmunitionId { get; set; }
    public int? AmmoLotId { get; set; }

    [Range(32, 200)]
    public decimal? BarrelTemperature { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

// ==================== Velocity Reading DTOs ====================

public class VelocityReadingDto
{
    public int Id { get; set; }
    public int ShotNumber { get; set; }
    public decimal Velocity { get; set; }
}

public class CreateVelocityReadingDto
{
    [Required]
    [Range(1, 100)]
    public int ShotNumber { get; set; }

    [Required]
    [Range(500, 5000)]
    public decimal Velocity { get; set; }
}

// ==================== Group DTOs ====================

public class GroupEntryDto
{
    public int Id { get; set; }
    public int GroupNumber { get; set; }
    public int Distance { get; set; }
    public int NumberOfShots { get; set; }
    public decimal? GroupSizeMoa { get; set; }
    public decimal? MeanRadiusMoa { get; set; }
    public decimal? GroupSizeInches { get; set; }
    public AmmoSummaryDto? Ammunition { get; set; }
    public AmmoLotSummaryDto? AmmoLot { get; set; }
    public string? Notes { get; set; }
    public List<ImageDto> Images { get; set; } = new();
}

public class CreateGroupEntryDto
{
    [Required]
    [Range(1, 20)]
    public int GroupNumber { get; set; }

    [Required]
    [Range(25, 2500)]
    public int Distance { get; set; }

    [Required]
    [Range(1, 25)]
    public int NumberOfShots { get; set; }

    [Range(0.01, 20)]
    public decimal? GroupSizeMoa { get; set; }

    [Range(0.01, 10)]
    public decimal? MeanRadiusMoa { get; set; }

    public int? AmmunitionId { get; set; }
    public int? AmmoLotId { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class UpdateGroupEntryDto
{
    [Range(25, 2500)]
    public int? Distance { get; set; }

    [Range(1, 25)]
    public int? NumberOfShots { get; set; }

    [Range(0.01, 20)]
    public decimal? GroupSizeMoa { get; set; }

    [Range(0.01, 10)]
    public decimal? MeanRadiusMoa { get; set; }

    public int? AmmunitionId { get; set; }
    public int? AmmoLotId { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

// ==================== Image DTO (shared) ====================

public class ImageDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
}

// ==================== Create/Update Session DTOs ====================

public class CreateSessionDto
{
    [Required]
    public DateTime SessionDate { get; set; }

    public TimeSpan? SessionTime { get; set; }

    [Required]
    public int RifleSetupId { get; set; }

    // Location (optional)
    public int? SavedLocationId { get; set; }

    [Range(-90, 90)]
    public decimal? Latitude { get; set; }

    [Range(-180, 180)]
    public decimal? Longitude { get; set; }

    [MaxLength(100)]
    public string? LocationName { get; set; }

    // Conditions
    [Range(-50, 120)]
    public decimal? Temperature { get; set; }

    [Range(0, 100)]
    public int? Humidity { get; set; }

    [Range(0, 100)]
    public decimal? WindSpeed { get; set; }

    [Range(0, 359)]
    public int? WindDirection { get; set; }

    [Range(20, 35)]
    public decimal? Pressure { get; set; }

    public decimal? DensityAltitude { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    // Child data (optional)
    public List<CreateDopeEntryDto>? DopeEntries { get; set; }
    public CreateChronoSessionDto? ChronoSession { get; set; }
    public List<CreateGroupEntryDto>? GroupEntries { get; set; }
}

public class UpdateSessionDto
{
    public DateTime? SessionDate { get; set; }
    public TimeSpan? SessionTime { get; set; }
    public int? RifleSetupId { get; set; }

    // Location
    public int? SavedLocationId { get; set; }

    [Range(-90, 90)]
    public decimal? Latitude { get; set; }

    [Range(-180, 180)]
    public decimal? Longitude { get; set; }

    [MaxLength(100)]
    public string? LocationName { get; set; }

    // Conditions
    [Range(-50, 120)]
    public decimal? Temperature { get; set; }

    [Range(0, 100)]
    public int? Humidity { get; set; }

    [Range(0, 100)]
    public decimal? WindSpeed { get; set; }

    [Range(0, 359)]
    public int? WindDirection { get; set; }

    [Range(20, 35)]
    public decimal? Pressure { get; set; }

    public decimal? DensityAltitude { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

// ==================== Filter DTO ====================

public class SessionFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int? RifleId { get; set; }
    public int? AmmoId { get; set; }
    public bool? HasDopeData { get; set; }
    public bool? HasChronoData { get; set; }
    public bool? HasGroupData { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string SortBy { get; set; } = "sessionDate";
    public bool SortDesc { get; set; } = true;
}
