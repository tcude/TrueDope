using TrueDope.Api.Data.Entities;
using TrueDope.Api.DTOs.Admin;

namespace TrueDope.Api.Services;

public interface IAdminAuditService
{
    /// <summary>
    /// Log an admin action
    /// </summary>
    Task LogActionAsync(AdminAuditLogEntry entry);

    /// <summary>
    /// Get audit logs with filtering and pagination
    /// </summary>
    Task<List<AdminAuditLogDto>> GetLogsAsync(AdminAuditFilter filter);

    /// <summary>
    /// Get audit log count for statistics
    /// </summary>
    Task<int> GetLogCountAsync(DateTime? since = null);
}

/// <summary>
/// Entry for logging admin actions
/// </summary>
public class AdminAuditLogEntry
{
    public required string AdminUserId { get; set; }
    public required string ActionType { get; set; }
    public string? TargetUserId { get; set; }
    public string? TargetEntityType { get; set; }
    public int? TargetEntityId { get; set; }
    public object? Details { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>
/// Filter for querying audit logs
/// </summary>
public class AdminAuditFilter
{
    public string? AdminUserId { get; set; }
    public string? TargetUserId { get; set; }
    public string? ActionType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
