using Adimarip.Application.Abstractions;

namespace Adimarip.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", () => Results.Ok(new { status = "ok", service = "adimarip-api" }))
            .WithName("Health");

        // В отличие от liveness, readiness проверяет соединение с PostgreSQL (для деплоя и CI).
        app.MapGet("/api/ready", async (IHealthProbe probe, CancellationToken ct) =>
                await probe.IsDatabaseAvailableAsync(ct)
                    ? Results.Ok(new { status = "ready", database = "connected" })
                    : Results.StatusCode(StatusCodes.Status503ServiceUnavailable))
            .WithName("Readiness");
        return app;
    }
}
