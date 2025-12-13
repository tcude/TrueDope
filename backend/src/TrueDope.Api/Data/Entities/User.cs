using Microsoft.AspNetCore.Identity;

namespace TrueDope.Api.Data.Entities;

public class User : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public bool IsAdmin { get; set; } = false;

    // Password reset fields (stored in DB for durability)
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    // Navigation properties for owned entities
    public ICollection<RifleSetup> RifleSetups { get; set; } = new List<RifleSetup>();
    public ICollection<Ammunition> Ammunition { get; set; } = new List<Ammunition>();
    public ICollection<AmmoLot> AmmoLots { get; set; } = new List<AmmoLot>();
    public ICollection<SavedLocation> SavedLocations { get; set; } = new List<SavedLocation>();
    public ICollection<RangeSession> RangeSessions { get; set; } = new List<RangeSession>();
    public ICollection<Image> Images { get; set; } = new List<Image>();

    // User preferences (1:1 relationship)
    public UserPreferences? Preferences { get; set; }
}
