using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrueDope.Api.Data.Entities;

public class UserPreferences
{
    [Key]
    public string UserId { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    // Unit Preferences
    public DistanceUnit DistanceUnit { get; set; } = DistanceUnit.Yards;
    public AdjustmentUnit AdjustmentUnit { get; set; } = AdjustmentUnit.MIL;
    public TemperatureUnit TemperatureUnit { get; set; } = TemperatureUnit.Fahrenheit;
    public PressureUnit PressureUnit { get; set; } = PressureUnit.InHg;
    public VelocityUnit VelocityUnit { get; set; } = VelocityUnit.FPS;

    // Theme
    public ThemePreference Theme { get; set; } = ThemePreference.System;

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
