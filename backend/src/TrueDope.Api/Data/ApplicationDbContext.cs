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
    public DbSet<RangeSession> RangeSessions => Set<RangeSession>();
    public DbSet<DopeEntry> DopeEntries => Set<DopeEntry>();
    public DbSet<ChronoSession> ChronoSessions => Set<ChronoSession>();
    public DbSet<VelocityReading> VelocityReadings => Set<VelocityReading>();
    public DbSet<GroupEntry> GroupEntries => Set<GroupEntry>();
    public DbSet<Image> Images => Set<Image>();

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
    }
}
