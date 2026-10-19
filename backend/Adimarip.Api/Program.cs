using Adimarip.Data;
using Adimarip.Api.Features;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRegistrySecurity(builder.Configuration);

// The seminar-2 health endpoint does not query PostgreSQL.
// Domain DbContext is registered when a connection string is provided;
// migrations and CRUD endpoints will be implemented in later seminars.
var databaseConnection = builder.Configuration.GetConnectionString("Default");
if (!string.IsNullOrWhiteSpace(databaseConnection))
{
    builder.Services.AddDbContext<AdimaripDbContext>(options => options.UseNpgsql(databaseConnection));
}

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:8080", "http://localhost:5173" };
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", service = "adimarip-api" }))
    .WithName("Health");

if (!string.IsNullOrWhiteSpace(databaseConnection))
{
    await DataSchema.ApplyAsync(app.Services);
    await DemoSeed.ApplyAsync(app.Services, app.Configuration);
}

app.MapRegistryAuth();
app.Run();
