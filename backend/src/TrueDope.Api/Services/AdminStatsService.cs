using Microsoft.EntityFrameworkCore;
using TrueDope.Api.Data;
using TrueDope.Api.DTOs.Admin;

namespace TrueDope.Api.Services;

public interface IAdminStatsService
{
    Task<SystemStatsDto> GetSystemStatsAsync();
}

public class AdminStatsService : IAdminStatsService
{
    private readonly ApplicationDbContext _context;
    private readonly IStorageService _storageService;
    private readonly ILogger<AdminStatsService> _logger;

    public AdminStatsService(
        ApplicationDbContext context,
        IStorageService storageService,
        ILogger<AdminStatsService> logger)
    {
        _context = context;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<SystemStatsDto> GetSystemStatsAsync()
    {
        _logger.LogInformation("Generating system statistics");

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Run queries sequentially - EF Core DbContext is not thread-safe
        var userCount = await _context.Users.CountAsync();
        var activeUsers = await _context.Users.CountAsync(u => u.LastLoginAt != null && u.LastLoginAt > thirtyDaysAgo);
        var adminCount = await _context.Users.CountAsync(u => u.IsAdmin);

        var sessionCount = await _context.RangeSessions.CountAsync();
        var sessionsThisMonth = await _context.RangeSessions.CountAsync(s => s.SessionDate >= startOfMonth);

        var rifleCount = await _context.RifleSetups.CountAsync();

        var ammoCount = await _context.Ammunition.CountAsync();
        var lotCount = await _context.AmmoLots.CountAsync();

        var imageCount = await _context.Images.CountAsync();
        var storageSizeBytes = await _context.Images.SumAsync(i => i.FileSize);

        var stats = new SystemStatsDto
        {
            Users = new UserStats
            {
                Total = userCount,
                ActiveLastThirtyDays = activeUsers,
                Admins = adminCount
            },
            Sessions = new SessionStats
            {
                Total = sessionCount,
                ThisMonth = sessionsThisMonth
            },
            Rifles = new RifleStats
            {
                Total = rifleCount
            },
            Ammunition = new AmmunitionStats
            {
                Total = ammoCount,
                Lots = lotCount
            },
            Images = new ImageStats
            {
                Total = imageCount,
                StorageSizeBytes = storageSizeBytes,
                StorageSizeFormatted = FormatFileSize(storageSizeBytes)
            },
            GeneratedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "System stats generated: {Users} users, {Sessions} sessions, {Rifles} rifles, {Images} images ({Storage})",
            stats.Users.Total, stats.Sessions.Total, stats.Rifles.Total, stats.Images.Total, stats.Images.StorageSizeFormatted);

        return stats;
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 0) return "0 B";

        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
