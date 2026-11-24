using Adimarip.Api.Http;
using Adimarip.Api.Security;
using Adimarip.Application.Access;
using static Adimarip.Domain.Access.Permission;

namespace Adimarip.Api.Endpoints;

/// <summary>Учётные записи и кастомные роли.</summary>
public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/api/admin/users").WithTags("Admin").RequirePermission(UsersManage);
        users.MapGet("/", ListUsersAsync);
        users.MapPost("/", CreateUserAsync);
        users.MapPut("/{username}/role", ChangeRoleAsync);

        app.MapGet("/api/permissions", ListPermissions).WithTags("Admin").RequirePermission(RolesManage, UsersManage);

        var roles = app.MapGroup("/api/roles").WithTags("Admin");
        roles.MapGet("/", ListRolesAsync).RequirePermission(RolesManage, UsersManage);
        roles.MapPost("/", CreateRoleAsync).RequirePermission(RolesManage);
        roles.MapPut("/{name}", UpdateRoleAsync).RequirePermission(RolesManage);
        roles.MapDelete("/{name}", DeleteRoleAsync).RequirePermission(RolesManage);
        return app;
    }

    private static async Task<IResult> ListUsersAsync(UserAdminService service, CancellationToken ct) =>
        Results.Ok(await service.ListAsync(ct));

    private static async Task<IResult> CreateUserAsync(CreateAccountInput input, UserAdminService service, CancellationToken ct) =>
        (await service.CreateAsync(input, ct)).ToCreated(u => $"/api/admin/users/{Uri.EscapeDataString(u.Username)}");

    private static async Task<IResult> ChangeRoleAsync(string username, ChangeRoleInput input, Actor actor,
        UserAdminService service, CancellationToken ct) =>
        (await service.ChangeRoleAsync(actor.Access, username, input, ct)).ToOk();

    private static IResult ListPermissions(RoleService service) => Results.Ok(service.Catalog());

    private static async Task<IResult> ListRolesAsync(RoleService service, CancellationToken ct) =>
        Results.Ok(await service.ListAsync(ct));

    private static async Task<IResult> CreateRoleAsync(RoleInput input, RoleService service, CancellationToken ct) =>
        (await service.CreateAsync(input, ct)).ToCreated(r => $"/api/roles/{r.Name}");

    private static async Task<IResult> UpdateRoleAsync(string name, RoleInput input, Actor actor, RoleService service,
        CancellationToken ct) =>
        (await service.UpdateAsync(actor.Access, name, input, ct)).ToOk();

    private static async Task<IResult> DeleteRoleAsync(string name, RoleService service, CancellationToken ct) =>
        (await service.DeleteAsync(name, ct)).ToNoContent();
}
