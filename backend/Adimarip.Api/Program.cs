using Adimarip.Data;
using Adimarip.Api.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRegistrySecurity(builder.Configuration);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0,
            AutoReplenishment = true
        }));
});

// The registry requires PostgreSQL; failing early gives a clear deployment error.
var databaseConnection = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(databaseConnection))
    throw new InvalidOperationException("ConnectionStrings:Default is required.");
builder.Services.AddDbContext<AdimaripDbContext>(options => options.UseNpgsql(databaseConnection));

// Cross-origin requests are for local development. Production traffic is same-origin.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? (builder.Environment.IsDevelopment() ? new[] { "http://localhost:8080", "http://localhost:5173" } : []);
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsProduction())
{
    var jwt = app.Configuration["Jwt:Key"] ?? "";
    if (app.Configuration.GetValue<bool>("Demo:Enabled") ||
        jwt.Contains("LOCAL_DEMO", StringComparison.OrdinalIgnoreCase) ||
        jwt.Contains("change-me", StringComparison.OrdinalIgnoreCase) ||
        jwt.Contains("CHANGE_THIS", StringComparison.OrdinalIgnoreCase) ||
        databaseConnection.Contains("adimarip_dev_password", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Unsafe demo configuration is forbidden in Production.");
}

app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", service = "adimarip-api" }))
    .WithName("Health");

await DataSchema.ApplyAsync(app.Services);
await DemoSeed.ApplyAsync(app.Services, app.Configuration);

app.MapRegistryAuth();
app.MapRegistryDirectories();
app.MapRegistryNotices();
app.MapRegistryStatuses();
app.MapRegistryActions();
app.MapBulkNotifications();
app.MapRegistryArchives();
app.Run();
