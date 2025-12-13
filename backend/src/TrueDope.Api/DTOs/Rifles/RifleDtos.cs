using System.ComponentModel.DataAnnotations;

namespace TrueDope.Api.DTOs.Rifles;

public class RifleListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string Caliber { get; set; } = string.Empty;
    public int ZeroDistance { get; set; }
    public int SessionCount { get; set; }
    public int ImageCount { get; set; }
    public DateTime? LastSessionDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RifleDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string Caliber { get; set; } = string.Empty;
    public decimal? BarrelLength { get; set; }
    public string? TwistRate { get; set; }

    // Optic
    public string? ScopeMake { get; set; }
    public string? ScopeModel { get; set; }
    public decimal? ScopeHeight { get; set; }

    // Zero
    public int ZeroDistance { get; set; }
    public decimal? ZeroElevationClicks { get; set; }
    public decimal? ZeroWindageClicks { get; set; }

    // Ballistics
    public decimal? MuzzleVelocity { get; set; }
    public decimal? BallisticCoefficient { get; set; }
    public string? DragModel { get; set; }

    public string? Notes { get; set; }

    public List<RifleImageDto> Images { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class RifleImageDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
}

public class CreateRifleDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Manufacturer { get; set; }

    [MaxLength(100)]
    public string? Model { get; set; }

    [Required]
    [MaxLength(50)]
    public string Caliber { get; set; } = string.Empty;

    [Range(1, 50)]
    public decimal? BarrelLength { get; set; }

    [MaxLength(20)]
    public string? TwistRate { get; set; }

    // Optic
    [MaxLength(100)]
    public string? ScopeMake { get; set; }

    [MaxLength(100)]
    public string? ScopeModel { get; set; }

    [Range(0, 5)]
    public decimal? ScopeHeight { get; set; }

    // Zero
    [Range(25, 1000)]
    public int ZeroDistance { get; set; } = 100;

    public decimal? ZeroElevationClicks { get; set; }
    public decimal? ZeroWindageClicks { get; set; }

    // Ballistics
    [Range(500, 5000)]
    public decimal? MuzzleVelocity { get; set; }

    [Range(0.001, 2.0)]
    public decimal? BallisticCoefficient { get; set; }

    [MaxLength(5)]
    [RegularExpression("^(G1|G7)$", ErrorMessage = "DragModel must be 'G1' or 'G7'")]
    public string? DragModel { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public class UpdateRifleDto
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(100)]
    public string? Manufacturer { get; set; }

    [MaxLength(100)]
    public string? Model { get; set; }

    [MaxLength(50)]
    public string? Caliber { get; set; }

    [Range(1, 50)]
    public decimal? BarrelLength { get; set; }

    [MaxLength(20)]
    public string? TwistRate { get; set; }

    // Optic
    [MaxLength(100)]
    public string? ScopeMake { get; set; }

    [MaxLength(100)]
    public string? ScopeModel { get; set; }

    [Range(0, 5)]
    public decimal? ScopeHeight { get; set; }

    // Zero
    [Range(25, 1000)]
    public int? ZeroDistance { get; set; }

    public decimal? ZeroElevationClicks { get; set; }
    public decimal? ZeroWindageClicks { get; set; }

    // Ballistics
    [Range(500, 5000)]
    public decimal? MuzzleVelocity { get; set; }

    [Range(0.001, 2.0)]
    public decimal? BallisticCoefficient { get; set; }

    [MaxLength(5)]
    [RegularExpression("^(G1|G7)$", ErrorMessage = "DragModel must be 'G1' or 'G7'")]
    public string? DragModel { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public class RifleFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string SortBy { get; set; } = "name";
    public bool SortDesc { get; set; } = false;
}
