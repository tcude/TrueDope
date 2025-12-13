using System.ComponentModel.DataAnnotations;

namespace TrueDope.Api.DTOs.SharedLocations;

/// <summary>
/// Shared location for list views (all users)
/// </summary>
public class SharedLocationListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? Altitude { get; set; }
    public string? Description { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string Country { get; set; } = "USA";
    public string? Website { get; set; }
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Shared location for admin views (includes IsActive and audit info)
/// </summary>
public class SharedLocationAdminDto : SharedLocationListDto
{
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
}

/// <summary>
/// Request to create a new shared location (admin only)
/// </summary>
public class CreateSharedLocationDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(-90, 90)]
    public decimal Latitude { get; set; }

    [Required]
    [Range(-180, 180)]
    public decimal Longitude { get; set; }

    [Range(-1000, 30000)]
    public decimal? Altitude { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(50)]
    public string? State { get; set; }

    [MaxLength(50)]
    public string Country { get; set; } = "USA";

    [MaxLength(255)]
    public string? Website { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Request to update a shared location (admin only)
/// </summary>
public class UpdateSharedLocationDto
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [Range(-90, 90)]
    public decimal? Latitude { get; set; }

    [Range(-180, 180)]
    public decimal? Longitude { get; set; }

    [Range(-1000, 30000)]
    public decimal? Altitude { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(50)]
    public string? State { get; set; }

    [MaxLength(50)]
    public string? Country { get; set; }

    [MaxLength(255)]
    public string? Website { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public bool? IsActive { get; set; }
}
