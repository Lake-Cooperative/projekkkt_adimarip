using Adimarip.Api.Http;
using Adimarip.Api.Security;
using Adimarip.Application.Directory;
using static Adimarip.Domain.Access.Permission;

namespace Adimarip.Api.Endpoints;

/// <summary>Справочники: подразделения и адресаты.</summary>
public static class DirectoryEndpoints
{
    public static IEndpointRouteBuilder MapDirectoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/offices", ListOfficesAsync)
            .WithTags("Directory")
            .RequirePermission(NoticesReadAll, NoticesReadOwn, BroadcastsSend, UsersManage);

        var recipients = app.MapGroup("/api/recipients").WithTags("Directory");
        recipients.MapGet("/", SearchRecipientsAsync).RequirePermission(RecipientsRead, NoticesReadOwn);
        recipients.MapPost("/", CreateRecipientAsync).RequirePermission(RecipientsCreate);
        recipients.MapPost("/{id:int}/archive", ArchiveRecipientAsync).RequirePermission(RecipientsArchive);
        return app;
    }

    private static async Task<IResult> ListOfficesAsync(Actor actor, DirectoryService service, CancellationToken ct) =>
        (await service.ListOfficesAsync(actor.Access, ct)).ToOk();

    private static async Task<IResult> SearchRecipientsAsync(string? search, Actor actor, DirectoryService service,
        CancellationToken ct) =>
        (await service.SearchRecipientsAsync(actor.Access, search, ct)).ToOk();

    private static async Task<IResult> CreateRecipientAsync(RecipientInput input, DirectoryService service, CancellationToken ct) =>
        (await service.CreateRecipientAsync(input, ct)).ToCreated(r => $"/api/recipients/{r.Id}");

    private static async Task<IResult> ArchiveRecipientAsync(int id, ArchiveRecipientInput input, Actor actor,
        DirectoryService service, CancellationToken ct) =>
        (await service.ArchiveRecipientAsync(actor.Access, id, input, ct)).ToOk();
}
