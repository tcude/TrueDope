using Microsoft.AspNetCore.Identity;
using TrueDope.Api.Data.Entities;

namespace TrueDope.Api.Data;

public class DbSeeder
{
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(
        UserManager<User> userManager,
        IConfiguration configuration,
        ILogger<DbSeeder> logger)
    {
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedAdminUserAsync();
    }

    private async Task SeedAdminUserAsync()
    {
        // Check if any users exist
        if (_userManager.Users.Any())
        {
            _logger.LogDebug("Users already exist, skipping admin seeding");
            return;
        }

        // Get admin credentials from configuration
        var adminEmail = _configuration["Admin:Email"] ?? "admin@truedope.io";
        var adminPassword = _configuration["Admin:Password"] ?? "Admin123!";
        var adminFirstName = _configuration["Admin:FirstName"] ?? "Admin";
        var adminLastName = _configuration["Admin:LastName"] ?? "User";

        // Check if admin user already exists
        var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin != null)
        {
            _logger.LogDebug("Admin user already exists");
            return;
        }

        var adminUser = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = adminFirstName,
            LastName = adminLastName,
            EmailConfirmed = true,
            IsAdmin = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(adminUser, adminPassword);

        if (result.Succeeded)
        {
            _logger.LogInformation("Admin user created: {Email}", adminEmail);
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to create admin user: {Errors}", errors);
        }
    }
}
