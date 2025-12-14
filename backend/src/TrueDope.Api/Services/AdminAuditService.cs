using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TrueDope.Api.Data;
using TrueDope.Api.Data.Entities;
using TrueDope.Api.DTOs.Admin;

namespace TrueDope.Api.Services;

public class AdminAuditService : IAdminAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminAuditService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public AdminAuditService(
        ApplicationDbContext context,
        ILogger<AdminAuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogActionAsync(AdminAuditLogEntry entry)
    {
        var auditLog = new AdminAuditLog
        {
            AdminUserId = entry.AdminUserId,
            ActionType = entry.ActionType,
            TargetUserId = entry.TargetUserId,
            TargetEntityType = entry.TargetEntityType,
            TargetEntityId = entry.TargetEntityId,
            Details = entry.Details != null ? JsonSerializer.Serialize(entry.Details, JsonOptions) : null,
            IpAddress = entry.IpAddress,
            UserAgent = entry.UserAgent,
            Timestamp = DateTime.UtcNow
        };

        _context.AdminAuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Admin audit: {ActionType} by {AdminUserId} targeting {TargetUserId}/{TargetEntityType}:{TargetEntityId}",
            entry.ActionType, entry.AdminUserId, entry.TargetUserId, entry.TargetEntityType, entry.TargetEntityId);
    }

    public async Task<List<AdminAuditLogDto>> GetLogsAsync(AdminAuditFilter filter)
    {
        var query = _context.AdminAuditLogs
            .AsNoTracking()
            .Include(a => a.AdminUser)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filter.AdminUserId))
        {
            query = query.Where(a => a.AdminUserId == filter.AdminUserId);
        }

        if (!string.IsNullOrEmpty(filter.TargetUserId))
        {
            query = query.Where(a => a.TargetUserId == filter.TargetUserId);
        }

        if (!string.IsNullOrEmpty(filter.ActionType))
        {
            query = query.Where(a => a.ActionType == filter.ActionType);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= filter.ToDate.Value);
        }

        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var page = Math.Max(filter.Page, 1);

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AdminAuditLogDto
            {
                Id = a.Id,
                AdminUserId = a.AdminUserId,
                AdminEmail = a.AdminUser.Email,
                ActionType = a.ActionType,
                TargetUserId = a.TargetUserId,
                TargetEntityType = a.TargetEntityType,
                TargetEntityId = a.TargetEntityId,
                Details = a.Details,
                IpAddress = a.IpAddress,
                Timestamp = a.Timestamp
            })
            .ToListAsync();

        // Fetch target user emails separately for those that have TargetUserId
        var targetUserIds = logs
            .Where(l => !string.IsNullOrEmpty(l.TargetUserId))
            .Select(l => l.TargetUserId!)
            .Distinct()
            .ToList();

        if (targetUserIds.Count > 0)
        {
            var targetUsers = await _context.Users
                .AsNoTracking()
                .Where(u => targetUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Email })
                .ToDictionaryAsync(u => u.Id, u => u.Email);

            foreach (var log in logs)
            {
                if (!string.IsNullOrEmpty(log.TargetUserId) && targetUsers.TryGetValue(log.TargetUserId, out var email))
                {
                    log.TargetUserEmail = email;
                }
            }
        }

        return logs;
    }

    public async Task<int> GetLogCountAsync(DateTime? since = null)
    {
        var query = _context.AdminAuditLogs.AsQueryable();

        if (since.HasValue)
        {
            query = query.Where(a => a.Timestamp >= since.Value);
        }

        return await query.CountAsync();
    }
}
