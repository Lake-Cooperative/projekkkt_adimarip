using Adimarip.Domain.Access;
using Microsoft.AspNetCore.Authorization;

namespace Adimarip.Api.Security;

/// <summary>Требование «есть хотя бы одно из прав».</summary>
public sealed class PermissionRequirement(IReadOnlyList<Permission> anyOf) : IAuthorizationRequirement
{
    public IReadOnlyList<Permission> AnyOf { get; } = anyOf;
}

internal sealed class PermissionAuthorizationHandler(ICurrentAccess current) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var access = await current.GetAsync();
        if (access is not null && requirement.AnyOf.Any(access.Has))
            context.Succeed(requirement);
    }
}

public static class PermissionEndpointExtensions
{
    /// <summary>Доступ к маршруту при наличии любого из перечисленных прав (401 без входа, 403 без прав).</summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, params Permission[] anyOf)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(anyOf)));
}
