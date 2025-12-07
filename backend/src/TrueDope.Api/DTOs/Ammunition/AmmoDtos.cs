using System.ComponentModel.DataAnnotations;

namespace TrueDope.Api.DTOs.Ammunition;

public class AmmoListDto
{
    public int Id { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Caliber { get; set; } = string.Empty;
    public decimal Grain { get; set; }
    public string? BulletType { get; set; }
    public decimal? CostPerRound { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int LotCount { get; set; }
    public int SessionCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AmmoDetailDto
{
    public int Id { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Caliber { get; set; } = string.Empty;
    public decimal Grain { get; set; }
    public string? BulletType { get; set; }
    public decimal? CostPerRound { get; set; }
    public decimal? BallisticCoefficient { get; set; }
    public string? DragModel { get; set; }
    public string? Notes { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public List<AmmoLotDto> Lots { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AmmoLotDto
{
    public int Id { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateTime? PurchaseDate { get; set; }
    public int? InitialQuantity { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal? CostPerRound { get; set; }
    public string? Notes { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAmmoDto
{
    [Required]
    [MaxLength(100)]
    public string Manufacturer { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Caliber { get; set; } = string.Empty;

    [Required]
    [Range(1, 1000)]
    public decimal Grain { get; set; }

    [MaxLength(50)]
    public string? BulletType { get; set; }

    [Range(0, 100)]
    public decimal? CostPerRound { get; set; }

    [Range(0.001, 2.0)]
    public decimal? BallisticCoefficient { get; set; }

    [MaxLength(5)]
    [RegularExpression("^(G1|G7)$", ErrorMessage = "DragModel must be 'G1' or 'G7'")]
    public string? DragModel { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class UpdateAmmoDto
{
    [MaxLength(100)]
    public string? Manufacturer { get; set; }

    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(50)]
    public string? Caliber { get; set; }

    [Range(1, 1000)]
    public decimal? Grain { get; set; }

    [MaxLength(50)]
    public string? BulletType { get; set; }

    [Range(0, 100)]
    public decimal? CostPerRound { get; set; }

    [Range(0.001, 2.0)]
    public decimal? BallisticCoefficient { get; set; }

    [MaxLength(5)]
    [RegularExpression("^(G1|G7)$", ErrorMessage = "DragModel must be 'G1' or 'G7'")]
    public string? DragModel { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class CreateAmmoLotDto
{
    [Required]
    [MaxLength(50)]
    public string LotNumber { get; set; } = string.Empty;

    public DateTime? PurchaseDate { get; set; }

    [Range(1, 100000)]
    public int? InitialQuantity { get; set; }

    [Range(0, 100000)]
    public decimal? PurchasePrice { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class UpdateAmmoLotDto
{
    [MaxLength(50)]
    public string? LotNumber { get; set; }

    public DateTime? PurchaseDate { get; set; }

    [Range(1, 100000)]
    public int? InitialQuantity { get; set; }

    [Range(0, 100000)]
    public decimal? PurchasePrice { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class AmmoFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? Caliber { get; set; }
    public string SortBy { get; set; } = "name";
    public bool SortDesc { get; set; } = false;
}
