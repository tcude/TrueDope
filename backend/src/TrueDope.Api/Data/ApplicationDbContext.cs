using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TrueDope.Api.Data.Entities;

namespace TrueDope.Api.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Core entities
    public DbSet<RifleSetup> RifleSetups => Set<RifleSetup>();
    public DbSet<Ammunition> Ammunition => Set<Ammunition>();
    public DbSet<AmmoLot> AmmoLots => Set<AmmoLot>();
    public DbSet<SavedLocation> SavedLocations => Set<SavedLocation>();
    public DbSet<SharedLocation> SharedLocations => Set<SharedLocation>();
    public DbSet<RangeSession> RangeSessions => Set<RangeSession>();
    public DbSet<DopeEntry> DopeEntries => Set<DopeEntry>();
    public DbSet<ChronoSession> ChronoSessions => Set<ChronoSession>();
    public DbSet<VelocityReading> VelocityReadings => Set<VelocityReading>();
    public DbSet<GroupEntry> GroupEntries => Set<GroupEntry>();
    public DbSet<GroupMeasurement> GroupMeasurements => Set<GroupMeasurement>();
    public DbSet<Image> Images => Set<Image>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Rename Identity tables to remove "AspNet" prefix
        builder.Entity<User>().ToTable("Users");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
        builder.Entity<IdentityRole>().ToTable("Roles");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");

        // Configure User entity
        builder.Entity<User>(entity =>
        {
            entity.Property(u => u.FirstName).HasMaxLength(100);
            entity.Property(u => u.LastName).HasMaxLength(100);
            entity.Property(u => u.PasswordResetToken).HasMaxLength(256);
        });

        // =====================
        // UserPreferences (1:1 with User)
        // =====================
        builder.Entity<UserPreferences>(entity =>
        {
            entity.HasKey(p => p.UserId);

            entity.HasOne(p => p.User)
                .WithOne(u => u.Preferences)
                .HasForeignKey<UserPreferences>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Store enums as strings for readability in DB
            entity.Property(p => p.DistanceUnit).HasConversion<string>().HasMaxLength(20);
            entity.Property(p => p.AdjustmentUnit).HasConversion<string>().HasMaxLength(20);
            entity.Property(p => p.TemperatureUnit).HasConversion<string>().HasMaxLength(20);
            entity.Property(p => p.PressureUnit).HasConversion<string>().HasMaxLength(20);
            entity.Property(p => p.VelocityUnit).HasConversion<string>().HasMaxLength(20);
            entity.Property(p => p.Theme).HasConversion<string>().HasMaxLength(20);
        });

        // =====================
        // RifleSetup
        // =====================
        builder.Entity<RifleSetup>(entity =>
        {
            entity.HasOne(r => r.User)
                .WithMany(u => u.RifleSetups)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(r => r.UserId);
        });

        // =====================
        // Ammunition
        // =====================
        builder.Entity<Ammunition>(entity =>
        {
            entity.HasOne(a => a.User)
                .WithMany(u => u.Ammunition)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(a => a.UserId);
        });

        // =====================
        // AmmoLot
        // =====================
        builder.Entity<AmmoLot>(entity =>
        {
            entity.HasOne(l => l.Ammunition)
                .WithMany(a => a.AmmoLots)
                .HasForeignKey(l => l.AmmunitionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(l => l.User)
                .WithMany(u => u.AmmoLots)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Unique constraint: no duplicate lot numbers per ammo
            entity.HasIndex(l => new { l.AmmunitionId, l.LotNumber }).IsUnique();
        });

        // =====================
        // SavedLocation
        // =====================
        builder.Entity<SavedLocation>(entity =>
        {
            entity.HasOne(l => l.User)
                .WithMany(u => u.SavedLocations)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(l => l.UserId);
        });

        // =====================
        // SharedLocation
        // =====================
        builder.Entity<SharedLocation>(entity =>
        {
            entity.HasOne(l => l.CreatedByUser)
                .WithMany()
                .HasForeignKey(l => l.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(l => l.IsActive);
            entity.HasIndex(l => l.State);
            entity.HasIndex(l => new { l.Latitude, l.Longitude });
        });

        // =====================
        // RangeSession
        // =====================
        builder.Entity<RangeSession>(entity =>
        {
            entity.HasOne(s => s.User)
                .WithMany(u => u.RangeSessions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.RifleSetup)
                .WithMany(r => r.RangeSessions)
                .HasForeignKey(s => s.RifleSetupId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.SavedLocation)
                .WithMany(l => l.RangeSessions)
                .HasForeignKey(s => s.SavedLocationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(s => s.UserId);
            entity.HasIndex(s => s.SessionDate);
            entity.HasIndex(s => s.RifleSetupId);
        });

        // =====================
        // DopeEntry
        // =====================
        builder.Entity<DopeEntry>(entity =>
        {
            entity.HasOne(d => d.RangeSession)
                .WithMany(s => s.DopeEntries)
                .HasForeignKey(d => d.RangeSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: one entry per distance per session
            entity.HasIndex(d => new { d.RangeSessionId, d.Distance }).IsUnique();
            entity.HasIndex(d => d.RangeSessionId);
        });

        // =====================
        // ChronoSession (1:1 with RangeSession)
        // =====================
        builder.Entity<ChronoSession>(entity =>
        {
            entity.HasOne(c => c.RangeSession)
                .WithOne(s => s.ChronoSession)
                .HasForeignKey<ChronoSession>(c => c.RangeSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Ammunition)
                .WithMany(a => a.ChronoSessions)
                .HasForeignKey(c => c.AmmunitionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.AmmoLot)
                .WithMany(l => l.ChronoSessions)
                .HasForeignKey(c => c.AmmoLotId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(c => c.AmmunitionId);
        });

        // =====================
        // VelocityReading
        // =====================
        builder.Entity<VelocityReading>(entity =>
        {
            entity.HasOne(v => v.ChronoSession)
                .WithMany(c => c.VelocityReadings)
                .HasForeignKey(v => v.ChronoSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: one reading per shot number per session
            entity.HasIndex(v => new { v.ChronoSessionId, v.ShotNumber }).IsUnique();
            entity.HasIndex(v => v.ChronoSessionId);
        });

        // =====================
        // GroupEntry
        // =====================
        builder.Entity<GroupEntry>(entity =>
        {
            entity.HasOne(g => g.RangeSession)
                .WithMany(s => s.GroupEntries)
                .HasForeignKey(g => g.RangeSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(g => g.Ammunition)
                .WithMany(a => a.GroupEntries)
                .HasForeignKey(g => g.AmmunitionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(g => g.AmmoLot)
                .WithMany(l => l.GroupEntries)
                .HasForeignKey(g => g.AmmoLotId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(g => g.RangeSessionId);
        });

        // =====================
        // GroupMeasurement (1:1 with GroupEntry)
        // =====================
        builder.Entity<GroupMeasurement>(entity =>
        {
            entity.HasOne(m => m.GroupEntry)
                .WithOne(g => g.Measurement)
                .HasForeignKey<GroupMeasurement>(m => m.GroupEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.OriginalImage)
                .WithMany()
                .HasForeignKey(m => m.OriginalImageId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(m => m.AnnotatedImage)
                .WithMany()
                .HasForeignKey(m => m.AnnotatedImageId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ensure 1:1 relationship
            entity.HasIndex(m => m.GroupEntryId).IsUnique();

            // Store enum as string
            entity.Property(m => m.CalibrationMethod)
                .HasConversion<string>()
                .HasMaxLength(20);
        });

        // =====================
        // Image (Polymorphic)
        // =====================
        builder.Entity<Image>(entity =>
        {
            entity.HasOne(i => i.User)
                .WithMany(u => u.Images)
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.RifleSetup)
                .WithMany(r => r.Images)
                .HasForeignKey(i => i.RifleSetupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.RangeSession)
                .WithMany(s => s.Images)
                .HasForeignKey(i => i.RangeSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.GroupEntry)
                .WithMany(g => g.Images)
                .HasForeignKey(i => i.GroupEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(i => i.UserId);
        });

        // =====================
        // AdminAuditLog
        // =====================
        builder.Entity<AdminAuditLog>(entity =>
        {
            entity.HasOne(a => a.AdminUser)
                .WithMany()
                .HasForeignKey(a => a.AdminUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(a => a.ActionType).HasMaxLength(100).IsRequired();
            entity.Property(a => a.TargetEntityType).HasMaxLength(100);
            entity.Property(a => a.IpAddress).HasMaxLength(50);
            entity.Property(a => a.UserAgent).HasMaxLength(500);

            entity.HasIndex(a => a.AdminUserId);
            entity.HasIndex(a => a.Timestamp);
            entity.HasIndex(a => a.ActionType);
            entity.HasIndex(a => a.TargetUserId);
        });
    }
}
