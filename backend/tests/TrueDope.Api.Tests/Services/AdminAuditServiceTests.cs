using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TrueDope.Api.Data;
using TrueDope.Api.Data.Entities;
using TrueDope.Api.Services;

namespace TrueDope.Api.Tests.Services;

public class AdminAuditServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AdminAuditService _service;
    private readonly User _adminUser;

    public AdminAuditServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<AdminAuditService>>();
        _service = new AdminAuditService(_context, logger.Object);

        // Create an admin user for testing
        _adminUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "admin@test.com",
            UserName = "admin@test.com",
            FirstName = "Test",
            LastName = "Admin",
            IsAdmin = true
        };
        _context.Users.Add(_adminUser);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task LogActionAsync_CreatesAuditLogEntry()
    {
        // Arrange
        var entry = new AdminAuditLogEntry
        {
            AdminUserId = _adminUser.Id,
            ActionType = "TestAction",
            TargetUserId = "target-user-id",
            IpAddress = "192.168.1.1",
            UserAgent = "Test Browser"
        };

        // Act
        await _service.LogActionAsync(entry);

        // Assert
        var log = await _context.AdminAuditLogs.FirstOrDefaultAsync();
        log.Should().NotBeNull();
        log!.AdminUserId.Should().Be(_adminUser.Id);
        log.ActionType.Should().Be("TestAction");
        log.TargetUserId.Should().Be("target-user-id");
        log.IpAddress.Should().Be("192.168.1.1");
        log.UserAgent.Should().Be("Test Browser");
    }

    [Fact]
    public async Task LogActionAsync_SerializesDetailsAsJson()
    {
        // Arrange
        var entry = new AdminAuditLogEntry
        {
            AdminUserId = _adminUser.Id,
            ActionType = "UserUpdated",
            Details = new { FirstName = "John", LastName = "Doe", IsAdmin = true }
        };

        // Act
        await _service.LogActionAsync(entry);

        // Assert
        var log = await _context.AdminAuditLogs.FirstOrDefaultAsync();
        log.Should().NotBeNull();
        log!.Details.Should().Contain("firstName");
        log.Details.Should().Contain("John");
        log.Details.Should().Contain("lastName");
        log.Details.Should().Contain("Doe");
    }

    [Fact]
    public async Task LogActionAsync_SetsTimestampToUtcNow()
    {
        // Arrange
        var beforeTime = DateTime.UtcNow.AddSeconds(-1);
        var entry = new AdminAuditLogEntry
        {
            AdminUserId = _adminUser.Id,
            ActionType = "TestAction"
        };

        // Act
        await _service.LogActionAsync(entry);

        // Assert
        var afterTime = DateTime.UtcNow.AddSeconds(1);
        var log = await _context.AdminAuditLogs.FirstOrDefaultAsync();
        log.Should().NotBeNull();
        log!.Timestamp.Should().BeAfter(beforeTime);
        log.Timestamp.Should().BeBefore(afterTime);
    }

    [Fact]
    public async Task GetLogsAsync_ReturnsLogsInDescendingTimestampOrder()
    {
        // Arrange
        _context.AdminAuditLogs.AddRange(
            new AdminAuditLog
            {
                AdminUserId = _adminUser.Id,
                ActionType = "First",
                Timestamp = DateTime.UtcNow.AddHours(-2)
            },
            new AdminAuditLog
            {
                AdminUserId = _adminUser.Id,
                ActionType = "Second",
                Timestamp = DateTime.UtcNow.AddHours(-1)
            },
            new AdminAuditLog
            {
                AdminUserId = _adminUser.Id,
                ActionType = "Third",
                Timestamp = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var logs = await _service.GetLogsAsync(new AdminAuditFilter());

        // Assert
        logs.Should().HaveCount(3);
        logs[0].ActionType.Should().Be("Third");
        logs[1].ActionType.Should().Be("Second");
        logs[2].ActionType.Should().Be("First");
    }

    [Fact]
    public async Task GetLogsAsync_FiltersBy_AdminUserId()
    {
        // Arrange
        var otherAdmin = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "other@test.com",
            UserName = "other@test.com",
            IsAdmin = true
        };
        _context.Users.Add(otherAdmin);

        _context.AdminAuditLogs.AddRange(
            new AdminAuditLog { AdminUserId = _adminUser.Id, ActionType = "Action1" },
            new AdminAuditLog { AdminUserId = otherAdmin.Id, ActionType = "Action2" }
        );
        await _context.SaveChangesAsync();

        // Act
        var logs = await _service.GetLogsAsync(new AdminAuditFilter { AdminUserId = _adminUser.Id });

        // Assert
        logs.Should().HaveCount(1);
        logs[0].ActionType.Should().Be("Action1");
    }

    [Fact]
    public async Task GetLogsAsync_FiltersBy_ActionType()
    {
        // Arrange
        _context.AdminAuditLogs.AddRange(
            new AdminAuditLog { AdminUserId = _adminUser.Id, ActionType = "UserUpdated" },
            new AdminAuditLog { AdminUserId = _adminUser.Id, ActionType = "UserDisabled" },
            new AdminAuditLog { AdminUserId = _adminUser.Id, ActionType = "UserUpdated" }
        );
        await _context.SaveChangesAsync();

        // Act
        var logs = await _service.GetLogsAsync(new AdminAuditFilter { ActionType = "UserUpdated" });

        // Assert
        logs.Should().HaveCount(2);
        logs.Should().AllSatisfy(l => l.ActionType.Should().Be("UserUpdated"));
    }

    [Fact]
    public async Task GetLogsAsync_FiltersBy_DateRange()
    {
        // Arrange
        _context.AdminAuditLogs.AddRange(
            new AdminAuditLog
            {
                AdminUserId = _adminUser.Id,
                ActionType = "Old",
                Timestamp = DateTime.UtcNow.AddDays(-10)
            },
            new AdminAuditLog
            {
                AdminUserId = _adminUser.Id,
                ActionType = "InRange",
                Timestamp = DateTime.UtcNow.AddDays(-3)
            },
            new AdminAuditLog
            {
                AdminUserId = _adminUser.Id,
                ActionType = "Recent",
                Timestamp = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var logs = await _service.GetLogsAsync(new AdminAuditFilter
        {
            FromDate = DateTime.UtcNow.AddDays(-5),
            ToDate = DateTime.UtcNow.AddDays(-1)
        });

        // Assert
        logs.Should().HaveCount(1);
        logs[0].ActionType.Should().Be("InRange");
    }

    [Fact]
    public async Task GetLogsAsync_Paginates_Correctly()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            _context.AdminAuditLogs.Add(new AdminAuditLog
            {
                AdminUserId = _adminUser.Id,
                ActionType = $"Action{i}",
                Timestamp = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var page1 = await _service.GetLogsAsync(new AdminAuditFilter { Page = 1, PageSize = 3 });
        var page2 = await _service.GetLogsAsync(new AdminAuditFilter { Page = 2, PageSize = 3 });

        // Assert
        page1.Should().HaveCount(3);
        page2.Should().HaveCount(3);
        page1[0].ActionType.Should().Be("Action0");
        page2[0].ActionType.Should().Be("Action3");
    }

    [Fact]
    public async Task GetLogsAsync_IncludesAdminEmail()
    {
        // Arrange
        _context.AdminAuditLogs.Add(new AdminAuditLog
        {
            AdminUserId = _adminUser.Id,
            ActionType = "TestAction"
        });
        await _context.SaveChangesAsync();

        // Act
        var logs = await _service.GetLogsAsync(new AdminAuditFilter());

        // Assert
        logs.Should().HaveCount(1);
        logs[0].AdminEmail.Should().Be(_adminUser.Email);
    }

    [Fact]
    public async Task GetLogCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        _context.AdminAuditLogs.AddRange(
            new AdminAuditLog { AdminUserId = _adminUser.Id, ActionType = "Action1" },
            new AdminAuditLog { AdminUserId = _adminUser.Id, ActionType = "Action2" },
            new AdminAuditLog { AdminUserId = _adminUser.Id, ActionType = "Action3" }
        );
        await _context.SaveChangesAsync();

        // Act
        var count = await _service.GetLogCountAsync();

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task GetLogCountAsync_FiltersBySinceDate()
    {
        // Arrange
        _context.AdminAuditLogs.AddRange(
            new AdminAuditLog
            {
                AdminUserId = _adminUser.Id,
                ActionType = "Old",
                Timestamp = DateTime.UtcNow.AddDays(-10)
            },
            new AdminAuditLog
            {
                AdminUserId = _adminUser.Id,
                ActionType = "Recent1",
                Timestamp = DateTime.UtcNow.AddDays(-2)
            },
            new AdminAuditLog
            {
                AdminUserId = _adminUser.Id,
                ActionType = "Recent2",
                Timestamp = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var count = await _service.GetLogCountAsync(since: DateTime.UtcNow.AddDays(-5));

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task LogActionAsync_WithEntityTarget_SetsEntityFields()
    {
        // Arrange
        var entry = new AdminAuditLogEntry
        {
            AdminUserId = _adminUser.Id,
            ActionType = "SharedLocationCreated",
            TargetEntityType = "SharedLocation",
            TargetEntityId = 42
        };

        // Act
        await _service.LogActionAsync(entry);

        // Assert
        var log = await _context.AdminAuditLogs.FirstOrDefaultAsync();
        log.Should().NotBeNull();
        log!.TargetEntityType.Should().Be("SharedLocation");
        log.TargetEntityId.Should().Be(42);
    }
}
