using Adimarip.Application.Access;
using Adimarip.Application.Broadcasts;
using Adimarip.Application.Directory;
using Adimarip.Application.Notices;

namespace Adimarip.Api.Composition;

/// <summary>Корень композиции для сервисов сценариев (слой Application не зависит от DI-контейнера).</summary>
public static class ApplicationServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<AccessResolver>();
        services.AddScoped<AuthService>();
        services.AddScoped<RoleService>();
        services.AddScoped<UserAdminService>();
        services.AddScoped<NoticeService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<AppealService>();
        services.AddScoped<DocumentService>();
        services.AddScoped<DirectoryService>();
        services.AddScoped<BroadcastService>();
        services.AddScoped<BroadcastDispatcher>();
        return services;
    }
}
