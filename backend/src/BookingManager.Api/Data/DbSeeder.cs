using BookingManager.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookingManager.Api.Data;

/// <summary>
/// Runtime seeding for data that cannot live in a migration: the admin password
/// hash is salted (non-deterministic) and configurable per environment.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAdminUserAsync(AppDbContext dbContext, IConfiguration configuration)
    {
        var email = configuration["SeedAdmin:Email"] ?? "admin@bookingmanager.local";
        var password = configuration["SeedAdmin:Password"] ?? "Admin123!";

        if (await dbContext.Users.AnyAsync(u => u.Email == email))
        {
            return;
        }

        var admin = new User
        {
            Id = Guid.NewGuid(),
            FullName = "System Administrator",
            Email = email,
            PasswordHash = string.Empty,
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        admin.PasswordHash = new PasswordHasher<User>().HashPassword(admin, password);

        dbContext.Users.Add(admin);
        await dbContext.SaveChangesAsync();
    }
}
