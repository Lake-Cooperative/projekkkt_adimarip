using Adimarip.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Health remains independent of PostgreSQL availability.
// No migrations or CRUD endpoints are executed during this seminar.
var databaseConnection = builder.Configuration.GetConnectionString("Default");
if (!string.IsNullOrWhiteSpace(databaseConnection))
{
    builder.Services.AddDbContext<AdimaripDbContext>(options => options.UseNpgsql(databaseConnection));
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", service = "adimarip-api" }))
    .WithName("Health");

app.Run();
