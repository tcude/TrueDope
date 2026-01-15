using System.ComponentModel.DataAnnotations;

namespace TrueDope.Api.DTOs.Admin;

/// <summary>
/// Request to clone all data from one user to another
/// </summary>
public class CloneUserDataRequest
{
    /// <summary>
    /// The user ID to copy data FROM
    /// </summary>
    [Required]
    public string SourceUserId { get; set; } = string.Empty;

    /// <summary>
    /// The user ID to copy data TO (all existing data will be deleted first!)
    /// </summary>
    [Required]
    public string TargetUserId { get; set; } = string.Empty;

    /// <summary>
    /// Must be true to proceed - confirms understanding that target user's data will be deleted
    /// </summary>
    public bool ConfirmOverwrite { get; set; } = false;
}

/// <summary>
/// Response after successfully cloning user data
/// </summary>
public class CloneUserDataResponse
{
    public bool Success { get; set; }
    public string SourceUserId { get; set; } = string.Empty;
    public string TargetUserId { get; set; } = string.Empty;
    public CloneStatistics Statistics { get; set; } = new();
    public DateTime CompletedAt { get; set; }
    public long DurationMs { get; set; }
}

/// <summary>
/// Statistics about what was cloned
/// </summary>
public class CloneStatistics
{
    public int RifleSetupsCopied { get; set; }
    public int AmmunitionCopied { get; set; }
    public int AmmoLotsCopied { get; set; }
    public int SavedLocationsCopied { get; set; }
    public int RangeSessionsCopied { get; set; }
    public int DopeEntriesCopied { get; set; }
    public int ChronoSessionsCopied { get; set; }
    public int VelocityReadingsCopied { get; set; }
    public int GroupEntriesCopied { get; set; }
    public int GroupMeasurementsCopied { get; set; }
    public int ImagesCopied { get; set; }
    public long ImageBytesCopied { get; set; }
    public bool UserPreferencesCopied { get; set; }

    // What was deleted from target user
    public int RifleSetupsDeleted { get; set; }
    public int AmmunitionDeleted { get; set; }
    public int AmmoLotsDeleted { get; set; }
    public int SavedLocationsDeleted { get; set; }
    public int RangeSessionsDeleted { get; set; }
    public int ImagesDeleted { get; set; }
}

/// <summary>
/// Preview of what would be cloned (dry run)
/// </summary>
public class ClonePreviewResponse
{
    public string SourceUserId { get; set; } = string.Empty;
    public string TargetUserId { get; set; } = string.Empty;
    public string SourceUserEmail { get; set; } = string.Empty;
    public string TargetUserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Data that will be deleted from target user
    /// </summary>
    public DataCounts TargetDataToDelete { get; set; } = new();

    /// <summary>
    /// Data that will be copied from source user
    /// </summary>
    public DataCounts SourceDataToCopy { get; set; } = new();
}

/// <summary>
/// Counts of various data types
/// </summary>
public class DataCounts
{
    public int RifleSetups { get; set; }
    public int Ammunition { get; set; }
    public int AmmoLots { get; set; }
    public int SavedLocations { get; set; }
    public int RangeSessions { get; set; }
    public int DopeEntries { get; set; }
    public int ChronoSessions { get; set; }
    public int VelocityReadings { get; set; }
    public int GroupEntries { get; set; }
    public int GroupMeasurements { get; set; }
    public int Images { get; set; }
    public bool HasUserPreferences { get; set; }
}
