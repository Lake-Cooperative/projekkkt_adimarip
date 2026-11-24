using System.Security.Claims;
using Adimarip.Application.Access;

namespace Adimarip.Api.Security;

/// <summary>Права текущего пользователя, вычисленные один раз за запрос по данным БД.</summary>
public interface ICurrentAccess
{
    Task<UserAccess?> GetAsync();
}

internal sealed class CurrentAccess(IHttpContextAccessor http, AccessResolver resolver) : ICurrentAccess
{
    private Task<UserAccess?>? _resolved;

    /// <summary>Результат кэшируется на время запроса и привязан к его отмене, а не к токену первого вызова.</summary>
    public Task<UserAccess?> GetAsync() => _resolved ??= ResolveAsync();

    private async Task<UserAccess?> ResolveAsync()
    {
        var context = http.HttpContext;
        var username = context?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return username is null ? null : await resolver.ResolveAsync(username, context!.RequestAborted);
    }
}

/// <summary>
/// Параметр обработчика «кто выполняет действие». Minimal API получает его через BindAsync,
/// поэтому обработчики не разбирают claims вручную.
/// </summary>
public sealed class Actor(UserAccess access)
{
    public UserAccess Access { get; } = access;

    public static async ValueTask<Actor?> BindAsync(HttpContext context)
    {
        var access = await context.RequestServices.GetRequiredService<ICurrentAccess>().GetAsync();
        return access is null ? null : new Actor(access);
    }
}
