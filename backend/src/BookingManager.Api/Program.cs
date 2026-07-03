using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using BookingManager.Api.Data;
using BookingManager.Api.Dtos.Common;
using BookingManager.Api.Middleware;
using BookingManager.Api.Models;
using BookingManager.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options => { })
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(options =>
    {
        // DataAnnotations failures use the same { code, message, errors } error
        // shape as every other error in the API.
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value is { Errors.Count: > 0 })
                .ToDictionary(
                    e => e.Key,
                    e => e.Value!.Errors.Select(err => err.ErrorMessage).ToArray());

            return new BadRequestObjectResult(new ErrorResponse
            {
                Code = "VALIDATION_ERROR",
                Message = "One or more fields are invalid.",
                Errors = errors
            });
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Booking Manager API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the accessToken from POST /api/auth/login."
    });
    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", doc), new List<string>() }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

builder.Services.AddScoped<IResourceService, ResourceService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddSingleton<ITokenService, TokenService>();

// --- Authentication & authorization -----------------------------------------
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // 401/403 use the same { code, message } error shape as the rest of the API.
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new ErrorResponse
                {
                    Code = "UNAUTHORIZED",
                    Message = "Authentication is required. Provide a valid bearer token."
                });
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new ErrorResponse
                {
                    Code = "FORBIDDEN",
                    Message = "You do not have permission to perform this action."
                });
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(nameof(UserRole.Admin)));
});

// --- Rate limiting ------------------------------------------------------------
// Policies are tiered by endpoint risk: auth endpoints are brute-force targets,
// booking writes hit the DB constraint path, reads are cheap but DB-heavy in bulk.
// Authenticated callers are partitioned per user, anonymous callers per IP.
static string UserOrIpPartition(HttpContext httpContext) =>
    httpContext.User.Identity?.IsAuthenticated == true
        ? "user:" + (httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown")
        : "ip:" + (httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Code = "RATE_LIMIT_EXCEEDED",
            Message = "Too many requests. Please slow down and try again shortly."
        }, cancellationToken);
    };

    var config = builder.Configuration.GetSection("RateLimiting");

    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        "ip:" + (httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = config.GetValue("Auth:PermitLimit", 5),
            Window = TimeSpan.FromMinutes(1)
        }));

    options.AddPolicy("booking-write", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        UserOrIpPartition(httpContext),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = config.GetValue("BookingWrite:PermitLimit", 10),
            Window = TimeSpan.FromMinutes(1)
        }));

    options.AddPolicy("reads", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        UserOrIpPartition(httpContext),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = config.GetValue("Reads:PermitLimit", 100),
            Window = TimeSpan.FromMinutes(1)
        }));

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            "global:" + (httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = config.GetValue("Global:PermitLimit", 200),
                Window = TimeSpan.FromMinutes(1)
            }));
});

const string FrontendCorsPolicy = "FrontendCorsPolicy";
var frontendOrigin = builder.Configuration["Frontend:Origin"] ?? "http://localhost:3000";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(frontendOrigin).AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
    await DbSeeder.SeedAdminUserAsync(dbContext, app.Configuration);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Behind docker/reverse proxies the client IP arrives in X-Forwarded-For;
// rate limiting and audit logs need the real address.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                       | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors(FrontendCorsPolicy);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;
