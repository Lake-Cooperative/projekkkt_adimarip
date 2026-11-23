using Adimarip.Application.Abstractions;
using Adimarip.Application.Access;
using Adimarip.Application.Broadcasts;
using Adimarip.Application.Delivery;
using Adimarip.Infrastructure.Broadcasts;
using Adimarip.Infrastructure.Delivery;
using Adimarip.Infrastructure.Persistence;
using Adimarip.Infrastructure.Persistence.Repositories;
using Adimarip.Infrastructure.Security;
using Adimarip.Infrastructure.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Adimarip.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Регистрирует реализации всех портов слоя Application.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:Default is required.");
        services.AddDbContext<AdimaripDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IHealthProbe, HealthProbe>();
        services.AddScoped<INoticeRepository, NoticeRepository>();
        services.AddScoped<IRecipientRepository, RecipientRepository>();
        services.AddScoped<IOfficeRepository, OfficeRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IBroadcastRepository, BroadcastRepository>();

        services.AddMemoryCache();
        services.AddScoped<IRoleSnapshotProvider, CachedRoleSnapshotProvider>();
        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
        services.AddSingleton<IClock, SystemClock>();

        AddDelivery(services, config);

        services.AddScoped<AccountSeeding>();
        services.AddStartupPipeline(typeof(DatabaseMigrationTask), typeof(BootstrapAdminTask), typeof(DemoDataTask));
        return services;
    }

    private static void AddDelivery(IServiceCollection services, IConfiguration config)
    {
        var delivery = config.GetSection(DeliveryOptions.Section).Get<DeliveryOptions>() ?? new DeliveryOptions();
        // Реального провайдера нет: отправка возможна только как имитация в демо-режиме.
        delivery.SimulationEnabled = config.GetValue<bool>("Demo:Enabled");
        services.AddSingleton(delivery);
        services.AddSingleton(config.GetSection(SimulationOptions.Section).Get<SimulationOptions>() ?? new SimulationOptions());

        services.AddSingleton<IChannelSender, SimulatedEmailSender>();
        services.AddSingleton<IChannelSender, SimulatedSmsSender>();
        services.AddSingleton<IChannelSender, SimulatedPortalSender>();
        services.AddSingleton<IChannelSender, SimulatedMaxSender>();
        services.AddSingleton<INotificationGateway>(sp => new NotificationGateway(
            sp.GetServices<IChannelSender>(), sp.GetRequiredService<DeliveryOptions>(), sp.GetRequiredService<IClock>()));

        services.AddSingleton<IBroadcastQueue, InMemoryBroadcastQueue>();
        services.AddHostedService<BroadcastDispatchWorker>();
    }
}
