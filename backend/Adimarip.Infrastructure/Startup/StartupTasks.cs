using Microsoft.Extensions.DependencyInjection;

namespace Adimarip.Infrastructure.Startup;

/// <summary>Шаг подготовки приложения при запуске (миграции, начальные данные).</summary>
public interface IStartupTask
{
    Task ExecuteAsync(CancellationToken ct);
}

/// <summary>Упорядоченный список шагов запуска (паттерн Command): порядок задаётся при регистрации.</summary>
public sealed class StartupPipeline(IReadOnlyList<Type> steps)
{
    public IReadOnlyList<Type> Steps { get; } = steps;
}

public static class StartupTaskRunner
{
    /// <summary>Регистрирует шаги в порядке выполнения.</summary>
    public static IServiceCollection AddStartupPipeline(this IServiceCollection services, params Type[] steps)
    {
        foreach (var step in steps)
            services.AddScoped(step);
        return services.AddSingleton(new StartupPipeline(steps));
    }

    /// <summary>Выполняет шаги по порядку, каждый — в своей области DI (свой DbContext).</summary>
    public static async Task RunStartupTasksAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        foreach (var step in services.GetRequiredService<StartupPipeline>().Steps)
        {
            using var scope = services.CreateScope();
            var task = (IStartupTask)scope.ServiceProvider.GetRequiredService(step);
            await task.ExecuteAsync(ct);
        }
    }
}
