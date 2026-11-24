using Adimarip.Api.Composition;
using Adimarip.Api.Endpoints;
using Adimarip.Api.Hosting;
using Adimarip.Api.Security;
using Adimarip.Infrastructure;
using Adimarip.Infrastructure.Startup;

var builder = WebApplication.CreateBuilder(args);

// Облачные хостинги передают порт в переменной PORT.
if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } port)
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRegistrySecurity(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();

// Для локальной разработки фронтенд на другом порту.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? (builder.Environment.IsDevelopment() ? ["http://localhost:8080", "http://localhost:5173"] : []);
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
ProductionSafetyGuard.Verify(app.Environment, app.Configuration);

// В сборке «одним контейнером» (корневой Dockerfile) API раздаёт и интерфейс из wwwroot.
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await app.Services.RunStartupTasksAsync();

app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapNoticeEndpoints();
app.MapDirectoryEndpoints();
app.MapAdminEndpoints();
app.MapBroadcastEndpoints();
// Путь интерфейса (не /api и не файл) отдаёт index.html; без wwwroot маршрут просто не срабатывает.
app.MapFallbackToFile("{*path:nonfile:regex(^(?!api/).*$)}", "index.html");
app.Run();
