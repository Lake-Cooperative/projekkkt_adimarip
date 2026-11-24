using Microsoft.Extensions.Configuration;

namespace Adimarip.Infrastructure.Persistence;

/// <summary>
/// Строка подключения к PostgreSQL: из ConnectionStrings:Default или из DATABASE_URL
/// (формат postgres://user:password@host:port/database, его выдают облачные хостинги).
/// </summary>
public static class PostgresConnection
{
    public static string Resolve(IConfiguration config)
    {
        var explicitString = config.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(explicitString)) return explicitString;

        var url = config["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(url)) return FromUrl(url);

        throw new InvalidOperationException("Укажите ConnectionStrings__Default или DATABASE_URL.");
    }

    public static string FromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("postgres" or "postgresql"))
            throw new InvalidOperationException("DATABASE_URL должен иметь вид postgres://user:password@host:port/database");

        var credentials = uri.UserInfo.Split(':', 2);
        var parts = new Dictionary<string, string>
        {
            ["Host"] = uri.Host,
            ["Port"] = (uri.Port > 0 ? uri.Port : 5432).ToString(),
            ["Database"] = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            ["Username"] = Uri.UnescapeDataString(credentials[0]),
            ["Password"] = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : "",
            // Облачные базы обычно требуют TLS; Prefer шифрует, если сервер поддерживает.
            ["SSL Mode"] = "Prefer"
        };
        return string.Join(';', parts.Select(p => $"{p.Key}={Quote(p.Value)}"));
    }

    private static string Quote(string value) =>
        value.IndexOfAny([';', '"', '\'', '=', ' ']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}
