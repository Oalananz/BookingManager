using BookingManager.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingManager.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(200);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(320);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);

            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Resource>(entity =>
        {
            entity.Property(r => r.Name).IsRequired().HasMaxLength(200);
            entity.Property(r => r.Type).IsRequired().HasMaxLength(50);
            entity.Property(r => r.Description).HasMaxLength(2000);
            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

            var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            entity.HasData(
                new Resource
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "Conference Room A",
                    Type = "Room",
                    Description = "Large conference room on the 1st floor with video conferencing.",
                    Capacity = 12,
                    Status = ResourceStatus.Available,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Resource
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "Conference Room B",
                    Type = "Room",
                    Description = "Small meeting room next to the kitchen.",
                    Capacity = 6,
                    Status = ResourceStatus.Available,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Resource
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "Projector",
                    Type = "Equipment",
                    Description = "Portable 4K projector with HDMI and USB-C.",
                    Capacity = null,
                    Status = ResourceStatus.Available,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Resource
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Name = "Meeting Pod 1",
                    Type = "Room",
                    Description = "Soundproof pod for quick 1:1s and calls.",
                    Capacity = 4,
                    Status = ResourceStatus.Available,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Resource
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Name = "Electronics Lab",
                    Type = "Lab",
                    Description = "Shared lab space with oscilloscopes and soldering stations.",
                    Capacity = 20,
                    Status = ResourceStatus.Available,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Resource
                {
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    Name = "MacBook Pro Loaner",
                    Type = "Equipment",
                    Description = "16-inch loaner laptop for travel and demos.",
                    Capacity = null,
                    Status = ResourceStatus.Available,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Resource
                {
                    Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                    Name = "3D Printer",
                    Type = "Equipment",
                    Description = "Prusa MK4 — currently being serviced.",
                    Capacity = null,
                    Status = ResourceStatus.Maintenance,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Resource
                {
                    Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                    Name = "Hot Desk 42",
                    Type = "Workspace",
                    Description = "Standing desk by the window, dual monitors.",
                    Capacity = 1,
                    Status = ResourceStatus.Available,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                }
            );
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.Property(b => b.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasOne(b => b.Resource)
                .WithMany(r => r.Bookings)
                .HasForeignKey(b => b.ResourceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Availability / overlap queries filter by resource + time window.
            entity.HasIndex(
                b => new { b.ResourceId, b.StartDateTime, b.EndDateTime },
                "IX_Bookings_ResourceId_TimeRange");

            // Partial index for the hot path: only Active bookings block a slot.
            entity.HasIndex(
                    b => new { b.ResourceId, b.StartDateTime, b.EndDateTime },
                    "IX_Bookings_Active_ResourceId_TimeRange")
                .HasFilter("\"Status\" = 'Active'");

            // "My bookings" list.
            entity.HasIndex(b => new { b.UserId, b.StartDateTime });

            // Completed is a derived value and must never be persisted.
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Bookings_Status_Persisted",
                "\"Status\" IN ('Active', 'Cancelled')"));
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(a => a.Action).HasConversion<string>().HasMaxLength(50);
            entity.Property(a => a.EntityType).IsRequired().HasMaxLength(50);
            entity.Property(a => a.EntityId).HasMaxLength(100);
            entity.Property(a => a.OldValue).HasColumnType("jsonb");
            entity.Property(a => a.NewValue).HasColumnType("jsonb");
            entity.Property(a => a.IpAddress).HasMaxLength(45);

            entity.HasIndex(a => a.CreatedAt);
            entity.HasIndex(a => new { a.EntityType, a.EntityId });
        });
    }
}
