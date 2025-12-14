namespace TrueDope.Api.DTOs.Admin;

public class SystemStatsDto
{
    public UserStats Users { get; set; } = new();
    public SessionStats Sessions { get; set; } = new();
    public RifleStats Rifles { get; set; } = new();
    public AmmunitionStats Ammunition { get; set; } = new();
    public ImageStats Images { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class UserStats
{
    public int Total { get; set; }
    public int ActiveLastThirtyDays { get; set; }
    public int Admins { get; set; }
}

public class SessionStats
{
    public int Total { get; set; }
    public int ThisMonth { get; set; }
}

public class RifleStats
{
    public int Total { get; set; }
}

public class AmmunitionStats
{
    public int Total { get; set; }
    public int Lots { get; set; }
}

public class ImageStats
{
    public int Total { get; set; }
    public long StorageSizeBytes { get; set; }
    public string StorageSizeFormatted { get; set; } = string.Empty;
}
