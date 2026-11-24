namespace Adimarip.Api.Hosting;

/// <summary>Запрещает запуск Production с демо-режимом, демо-секретом JWT или dev-паролем БД.</summary>
public static class ProductionSafetyGuard
{
    private static readonly string[] UnsafeJwtMarkers = ["LOCAL_DEMO", "change-me", "CHANGE_THIS"];

    public static void Verify(IHostEnvironment environment, IConfiguration config)
    {
        if (!environment.IsProduction()) return;
        var jwt = config["Jwt:Key"] ?? "";
        var connection = config.GetConnectionString("Default") ?? "";
        if (config.GetValue<bool>("Demo:Enabled") ||
            UnsafeJwtMarkers.Any(marker => jwt.Contains(marker, StringComparison.OrdinalIgnoreCase)) ||
            connection.Contains("adimarip_dev_password", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Unsafe demo configuration is forbidden in Production.");
    }
}
