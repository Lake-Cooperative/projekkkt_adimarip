using Adimarip.Api.Http;
using Adimarip.Api.Security;
using Adimarip.Application.Access;

namespace Adimarip.Api.Endpoints;

/// <summary>Вход и сведения о текущем пользователе.</summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Auth");
        auth.MapPost("/login", LoginAsync).AllowAnonymous().RequireRateLimiting(SecurityRegistration.LoginRateLimit);
        // Актуальные права: интерфейс перечитывает их, если роль изменили, пока пользователь в системе.
        auth.MapGet("/me", GetCurrentUser).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> LoginAsync(LoginInput input, AuthService service, JwtTokenIssuer tokens, CancellationToken ct)
    {
        var login = await service.LoginAsync(input, ct);
        return login.IsSuccess ? Results.Ok(tokens.Issue(login.Value)) : ResultHttpExtensions.ToProblem(login.Error!);
    }

    private static IResult GetCurrentUser(Actor actor) => Results.Ok(CurrentUserResponse.From(actor.Access));
}

/// <summary>Ответ GET /api/auth/me — те же поля, что в ответе входа, кроме токена.</summary>
public sealed record CurrentUserResponse(string Username, string DisplayName, string Role, string RoleDisplayName,
    int? RecipientId, IReadOnlyList<string> Permissions)
{
    public static CurrentUserResponse From(UserAccess access) => new(access.Username, access.DisplayName, access.RoleName,
        access.RoleDisplayName, access.RecipientId, access.PermissionCodes);
}
