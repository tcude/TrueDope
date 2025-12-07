using System.ComponentModel.DataAnnotations;

namespace TrueDope.Api.DTOs.Locations;

public class LocationListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? Altitude { get; set; }
    public string? Description { get; set; }
    public int SessionCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LocationDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? Altitude { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateLocationDto
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
}

public class UpdateLocationDto
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
}
