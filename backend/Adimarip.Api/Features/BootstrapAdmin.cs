using Adimarip.Data;

namespace Adimarip.Api.Features;

public static class BootstrapAdmin
{
    public static async Task ApplyAsync(IServiceProvider services, IConfiguration config, CancellationToken ct = default)
    {
        var username = config["Bootstrap:Username"]?.Trim();
        var password = config["Bootstrap:Password"];
        if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password)) return;
        if (string.IsNullOrWhiteSpace(username) ||
            !System.Text.RegularExpressions.Regex.IsMatch(username, "^[a-zA-Z0-9_.-]{3,40}$") ||
            string.IsNullOrEmpty(password) || password.Length < 16 || password.Length > 128)
            throw new InvalidOperationException("Bootstrap requires a valid username and password of 16-128 characters.");
        using var scope = services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<UserStore>();
        // INSERT ON CONFLICT DO NOTHING: never reset an existing password on restart.
        await store.CreateAsync(username, "Первоначальный администратор", "AutomationEngineer", null, password, ct);
    }
}
