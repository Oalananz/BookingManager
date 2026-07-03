using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BookingManager.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BookingManager.Tests.Integration;

/// <summary>
/// Full-pipeline tests (routing, auth, rate limiting, error shape) over an
/// in-memory EF provider. The DB exclusion constraint itself is covered by
/// BookingConcurrencyTests against real Postgres.
/// </summary>
public class BookingApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly Dictionary<string, string?> _configOverrides;

    public BookingApiFactory() : this(null)
    {
    }

    protected BookingApiFactory(Dictionary<string, string?>? configOverrides)
    {
        _configOverrides = configOverrides ?? new Dictionary<string, string?>
        {
            // Generous limits so functional tests never trip the limiter.
            ["RateLimiting:Auth:PermitLimit"] = "1000",
            ["RateLimiting:BookingWrite:PermitLimit"] = "1000",
            ["RateLimiting:Reads:PermitLimit"] = "1000",
            ["RateLimiting:Global:PermitLimit"] = "10000"
        };
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(_configOverrides));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<AppDbContext>));
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_dbName));

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.EnsureCreated();
            DbSeeder.SeedAdminUserAsync(dbContext,
                scope.ServiceProvider.GetRequiredService<IConfiguration>()).GetAwaiter().GetResult();
        });
    }
}

public class ApiIntegrationTests : IClassFixture<BookingApiFactory>
{
    private readonly BookingApiFactory _factory;

    public ApiIntegrationTests(BookingApiFactory factory) => _factory = factory;

    private static readonly DateTime Day = DateTime.UtcNow.Date.AddDays(14);

    private static string Iso(DateTime dt) => dt.ToString("yyyy-MM-ddTHH:mm:ssZ");

    private async Task<HttpClient> AuthenticatedClientAsync(string email, string password = "Password1!")
    {
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Integration User",
            email,
            password
        });

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("data").GetProperty("accessToken").GetString()!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@bookingmanager.local",
            password = "Admin123!"
        });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("data").GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<Guid> FirstResourceIdAsync(HttpClient client)
    {
        var body = await client.GetFromJsonAsync<JsonElement>("/api/resources");
        return body.GetProperty("data")[0].GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Bookings_WithoutToken_Returns401WithErrorEnvelope()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/bookings");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("UNAUTHORIZED", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task CreateBooking_Valid_Returns201WithDataEnvelope()
    {
        var client = await AuthenticatedClientAsync("create@test.local");
        var resourceId = await FirstResourceIdAsync(client);

        var response = await client.PostAsJsonAsync("/api/bookings", new
        {
            resourceId,
            startDateTime = Iso(Day.AddHours(9)),
            endDateTime = Iso(Day.AddHours(10))
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Active", body.GetProperty("data").GetProperty("status").GetString());
    }

    [Fact]
    public async Task CreateBooking_Overlap_Returns409WithBookingConflictCode()
    {
        var client = await AuthenticatedClientAsync("conflict@test.local");
        var resourceId = await FirstResourceIdAsync(client);

        var first = await client.PostAsJsonAsync("/api/bookings", new
        {
            resourceId,
            startDateTime = Iso(Day.AddHours(11)),
            endDateTime = Iso(Day.AddHours(12))
        });
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/bookings", new
        {
            resourceId,
            startDateTime = Iso(Day.AddHours(11).AddMinutes(30)),
            endDateTime = Iso(Day.AddHours(12).AddMinutes(30))
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var body = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("BOOKING_CONFLICT", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task CreateBooking_EndBeforeStart_Returns400WithValidationErrorCode()
    {
        var client = await AuthenticatedClientAsync("validation@test.local");
        var resourceId = await FirstResourceIdAsync(client);

        var response = await client.PostAsJsonAsync("/api/bookings", new
        {
            resourceId,
            startDateTime = Iso(Day.AddHours(10)),
            endDateTime = Iso(Day.AddHours(9))
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetBooking_AnotherUsersBooking_Returns404()
    {
        var owner = await AuthenticatedClientAsync("owner@test.local");
        var resourceId = await FirstResourceIdAsync(owner);
        var created = await owner.PostAsJsonAsync("/api/bookings", new
        {
            resourceId,
            startDateTime = Iso(Day.AddHours(15)),
            endDateTime = Iso(Day.AddHours(16))
        });
        created.EnsureSuccessStatusCode();
        var bookingId = (await created.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var attacker = await AuthenticatedClientAsync("attacker@test.local");
        var response = await attacker.GetAsync($"/api/bookings/{bookingId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoints_AsNormalUser_Returns403()
    {
        var client = await AuthenticatedClientAsync("notadmin@test.local");

        var response = await client.GetAsync("/api/admin/bookings");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("FORBIDDEN", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task AdminBookings_SeesOtherUsersBookings()
    {
        var user = await AuthenticatedClientAsync("visible@test.local");
        var resourceId = await FirstResourceIdAsync(user);
        var created = await user.PostAsJsonAsync("/api/bookings", new
        {
            resourceId,
            startDateTime = Iso(Day.AddHours(17)),
            endDateTime = Iso(Day.AddHours(18))
        });
        created.EnsureSuccessStatusCode();

        var admin = await AdminClientAsync();
        var body = await admin.GetFromJsonAsync<JsonElement>("/api/admin/bookings?pageSize=100");

        Assert.True(body.GetProperty("meta").GetProperty("totalCount").GetInt64() >= 1);
    }

    [Fact]
    public async Task AdminAuditLogs_RecordsBookingCreated()
    {
        var user = await AuthenticatedClientAsync("audited@test.local");
        var resourceId = await FirstResourceIdAsync(user);
        var created = await user.PostAsJsonAsync("/api/bookings", new
        {
            resourceId,
            startDateTime = Iso(Day.AddHours(19)),
            endDateTime = Iso(Day.AddHours(20))
        });
        created.EnsureSuccessStatusCode();

        var admin = await AdminClientAsync();
        var body = await admin.GetFromJsonAsync<JsonElement>(
            "/api/admin/audit-logs?action=BookingCreated&pageSize=100");

        Assert.True(body.GetProperty("data").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task ResourceWrites_AsNormalUser_Returns403()
    {
        var client = await AuthenticatedClientAsync("resourcewrite@test.local");

        var response = await client.PostAsJsonAsync("/api/resources", new
        {
            name = "Sneaky Room",
            type = "Room"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Availability_ReturnsSlots()
    {
        var client = await AuthenticatedClientAsync("availability@test.local");
        var resourceId = await FirstResourceIdAsync(client);

        var body = await client.GetFromJsonAsync<JsonElement>(
            $"/api/resources/{resourceId}/availability?from={Iso(Day.AddDays(1).AddHours(8))}&to={Iso(Day.AddDays(1).AddHours(18))}&durationMinutes=60");

        Assert.True(body.GetProperty("data").GetProperty("slots").GetArrayLength() >= 1);
    }
}

public class RateLimitingTests
{
    // Dedicated factory: tiny auth budget, isolated from the other tests.
    private sealed class TightAuthLimitFactory() : BookingApiFactory(new Dictionary<string, string?>
    {
        ["RateLimiting:Auth:PermitLimit"] = "3",
        ["RateLimiting:Global:PermitLimit"] = "10000"
    });

    [Fact]
    public async Task Login_OverAuthLimit_Returns429WithRateLimitCode()
    {
        using var factory = new TightAuthLimitFactory();
        var client = factory.CreateClient();

        HttpResponseMessage? last = null;
        for (var i = 0; i < 4; i++)
        {
            last = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = "hammer@test.local",
                password = "WrongPassword!"
            });
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
        var body = await last.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("RATE_LIMIT_EXCEEDED", body.GetProperty("code").GetString());
    }
}
