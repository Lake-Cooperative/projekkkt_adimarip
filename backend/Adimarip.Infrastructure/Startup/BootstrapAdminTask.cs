using Adimarip.Application.Abstractions;
using Adimarip.Domain.Access;
using Microsoft.Extensions.Configuration;

namespace Adimarip.Infrastructure.Startup;

/// <summary>Первый инженер автоматизации из переменных окружения (для prod-профиля без демо-данных).</summary>
internal sealed class BootstrapAdminTask(IConfiguration config, AccountSeeding accounts, IUnitOfWork unitOfWork) : IStartupTask
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var username = config["Bootstrap:Username"]?.Trim();
        var password = config["Bootstrap:Password"];
        if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password)) return;
        if (!AppUser.IsValidUsername(username) || password is null || password.Length is < 16 or > 128)
            throw new InvalidOperationException("Bootstrap requires a valid username and password of 16-128 characters.");

        await accounts.EnsureAsync(username!, "Первоначальный администратор", SystemRoles.AutomationEngineer, null, password, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
