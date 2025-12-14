namespace TrueDope.Api.Data.Entities;

/// <summary>
/// Audit log entry for tracking admin actions
/// </summary>
public class AdminAuditLog
{
    public int Id { get; set; }

    /// <summary>
    /// The admin user who performed the action
    /// </summary>
    public string AdminUserId { get; set; } = null!;
    public User AdminUser { get; set; } = null!;

    /// <summary>
    /// Type of action performed (e.g., "UserUpdated", "UserDisabled", "PasswordReset")
    /// </summary>
    public string ActionType { get; set; } = null!;

    /// <summary>
    /// The target user ID (if action affects a user)
    /// </summary>
    public string? TargetUserId { get; set; }

    /// <summary>
    /// The target entity type (e.g., "User", "SharedLocation")
    /// </summary>
    public string? TargetEntityType { get; set; }

    /// <summary>
    /// The target entity ID (for non-user entities)
    /// </summary>
    public int? TargetEntityId { get; set; }

    /// <summary>
    /// JSON-serialized details of the action (e.g., what was changed)
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// IP address of the request
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the request
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Timestamp of the action
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
