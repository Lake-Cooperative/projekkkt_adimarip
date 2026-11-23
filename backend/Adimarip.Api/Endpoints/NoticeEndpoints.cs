using Adimarip.Api.Http;
using Adimarip.Api.Security;
using Adimarip.Application.Notices;
using static Adimarip.Domain.Access.Permission;

namespace Adimarip.Api.Endpoints;

/// <summary>Реестр оповещений: поиск, карточка, статусы, уведомления, обращения, документы.</summary>
public static class NoticeEndpoints
{
    public static IEndpointRouteBuilder MapNoticeEndpoints(this IEndpointRouteBuilder app)
    {
        var notices = app.MapGroup("/api/notices").WithTags("Notices");

        notices.MapGet("/", SearchAsync).RequirePermission(NoticesReadAll, NoticesReadOwn);
        notices.MapGet("/{id:int}", GetAsync).RequirePermission(NoticesReadAll, NoticesReadOwn);
        notices.MapPost("/", CreateAsync).RequirePermission(NoticesCreate);
        notices.MapPatch("/{id:int}/status", ChangeStatusAsync).RequirePermission(NoticesChangeStatus);
        notices.MapPost("/{id:int}/acknowledge", AcknowledgeAsync).RequirePermission(NoticesAcknowledgeOwn);
        notices.MapPost("/{id:int}/notifications", NotifyAsync).RequirePermission(NotificationsSend);
        notices.MapPost("/notifications/bulk", NotifyBulkAsync).RequirePermission(NotificationsSend);
        notices.MapPost("/{id:int}/appeals", FileAppealAsync).RequirePermission(AppealsCreate);
        notices.MapPost("/{id:int}/documents", AttachDocumentAsync).RequirePermission(DocumentsAttach);
        return app;
    }

    private static async Task<IResult> SearchAsync(string? status, int? officeId, string? search, int? page, int? pageSize,
        Actor actor, NoticeService service, CancellationToken ct) =>
        (await service.SearchAsync(actor.Access, new NoticeSearchInput(status, officeId, search, page, pageSize), ct)).ToOk();

    private static async Task<IResult> GetAsync(int id, Actor actor, NoticeService service, CancellationToken ct) =>
        (await service.GetAsync(actor.Access, id, ct)).ToOk();

    private static async Task<IResult> CreateAsync(CreateNoticeInput input, Actor actor, NoticeService service, CancellationToken ct) =>
        (await service.CreateAsync(actor.Access, input, ct)).ToCreated(n => $"/api/notices/{n.Id}");

    private static async Task<IResult> ChangeStatusAsync(int id, ChangeStatusInput input, Actor actor, NoticeService service,
        CancellationToken ct) =>
        (await service.ChangeStatusAsync(actor.Access, id, input, ct)).ToOk();

    private static async Task<IResult> AcknowledgeAsync(int id, Actor actor, NoticeService service, CancellationToken ct) =>
        (await service.AcknowledgeAsync(actor.Access, id, ct)).ToOk();

    private static async Task<IResult> NotifyAsync(int id, NotifyInput input, Actor actor, NotificationService service,
        CancellationToken ct) =>
        (await service.SendAsync(actor.Access, id, input, ct)).ToCreated(_ => $"/api/notices/{id}");

    private static async Task<IResult> NotifyBulkAsync(BulkNotifyInput input, Actor actor, NotificationService service,
        CancellationToken ct) =>
        (await service.SendBulkAsync(actor.Access, input, ct)).ToOk();

    private static async Task<IResult> FileAppealAsync(int id, AppealInput input, Actor actor, AppealService service,
        CancellationToken ct) =>
        (await service.FileAsync(actor.Access, id, input, ct)).ToCreated(_ => $"/api/notices/{id}");

    private static async Task<IResult> AttachDocumentAsync(int id, DocumentInput input, Actor actor, DocumentService service,
        CancellationToken ct) =>
        (await service.AttachAsync(actor.Access, id, input, ct)).ToCreated(_ => $"/api/notices/{id}");
}
