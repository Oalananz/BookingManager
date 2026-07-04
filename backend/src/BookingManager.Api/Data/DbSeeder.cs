using BookingManager.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookingManager.Api.Data;

/// <summary>
/// Runtime seeding for data that cannot live in a migration: password hashes are
/// salted (non-deterministic) and demo bookings need dates relative to "now".
/// Resources are seeded in the migration itself (deterministic GUIDs).
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

        dbContext.Users.Add(MakeUser("System Administrator", email, password, UserRole.Admin));
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Demo accounts and sample bookings so the app is explorable immediately
    /// after a fresh start. Development environments only; idempotent (skipped
    /// once any non-admin user exists).
    /// </summary>
    public static async Task SeedDemoDataAsync(AppDbContext dbContext)
    {
        if (await dbContext.Users.AnyAsync(u => u.Role == UserRole.User))
        {
            return;
        }

        var alice = MakeUser("Alice Johnson", "alice@bookingmanager.local", "Password1!", UserRole.User);
        var bob = MakeUser("Bob Smith", "bob@bookingmanager.local", "Password1!", UserRole.User);
        dbContext.Users.AddRange(alice, bob);

        var roomA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var roomB = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var projector = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var pod = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var lab = Guid.Parse("55555555-5555-5555-5555-555555555555");

        // Anchor on tomorrow 00:00 UTC so every slot is safely in the future
        // and slots per resource never overlap (the exclusion constraint would
        // reject the seed otherwise).
        var day1 = DateTime.UtcNow.Date.AddDays(1);
        var day2 = day1.AddDays(1);

        var bookings = new List<Booking>
        {
            MakeBooking(roomA, alice, day1.AddHours(9), day1.AddHours(10)),   // standup
            MakeBooking(roomA, bob, day1.AddHours(10), day1.AddHours(12)),    // back-to-back with the standup
            MakeBooking(roomA, alice, day2.AddHours(14), day2.AddHours(15)),
            MakeBooking(roomB, bob, day1.AddHours(13), day1.AddHours(14)),
            MakeBooking(projector, alice, day1.AddHours(9), day1.AddHours(17)), // all-day equipment loan
            MakeBooking(pod, bob, day2.AddHours(11), day2.AddHours(11).AddMinutes(30)),
            MakeBooking(lab, alice, day2.AddHours(8), day2.AddHours(12)),
        };

        // One cancelled booking so the demo shows the full lifecycle (and that
        // a cancelled slot no longer blocks the resource).
        var cancelled = MakeBooking(roomB, alice, day1.AddHours(9), day1.AddHours(10));
        cancelled.Status = BookingStatus.Cancelled;
        cancelled.CancelledAt = DateTime.UtcNow;
        cancelled.CancelledBy = alice.Id;
        bookings.Add(cancelled);

        dbContext.Bookings.AddRange(bookings);
        await dbContext.SaveChangesAsync();
    }

    private static User MakeUser(string fullName, string email, string password, UserRole role)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PasswordHash = string.Empty,
            Role = role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);
        return user;
    }

    private static Booking MakeBooking(Guid resourceId, User user, DateTime startUtc, DateTime endUtc) => new()
    {
        Id = Guid.NewGuid(),
        ResourceId = resourceId,
        UserId = user.Id,
        User = user,
        StartDateTime = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc),
        EndDateTime = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc),
        Status = BookingStatus.Active,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
