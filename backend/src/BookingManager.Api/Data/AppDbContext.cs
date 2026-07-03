using BookingManager.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingManager.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Resource>(entity =>
        {
            entity.Property(r => r.Name).IsRequired().HasMaxLength(200);
            entity.Property(r => r.Type).IsRequired().HasMaxLength(50);

            entity.HasData(
                new Resource
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "Conference Room A",
                    Type = "Room",
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Resource
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "Conference Room B",
                    Type = "Room",
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Resource
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "Projector",
                    Type = "Equipment",
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.Property(b => b.UserId).IsRequired().HasMaxLength(200);

            entity.Property(b => b.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasOne(b => b.Resource)
                .WithMany(r => r.Bookings)
                .HasForeignKey(b => b.ResourceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(b => new { b.ResourceId, b.StartDateTime, b.EndDateTime });
        });
    }
}
