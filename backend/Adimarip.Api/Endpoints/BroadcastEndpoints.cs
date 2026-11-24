using Adimarip.Api.Http;
using Adimarip.Api.Security;
using Adimarip.Application.Broadcasts;
using static Adimarip.Domain.Access.Permission;

namespace Adimarip.Api.Endpoints;

/// <summary>Массовое оповещение работников.</summary>
public static class BroadcastEndpoints
{
    public static IEndpointRouteBuilder MapBroadcastEndpoints(this IEndpointRouteBuilder app)
    {
        var broadcasts = app.MapGroup("/api/broadcasts").WithTags("Broadcasts");
        broadcasts.MapPost("/", CreateAsync).RequirePermission(BroadcastsSend);
        broadcasts.MapPost("/preview", PreviewAsync).RequirePermission(BroadcastsSend);
        broadcasts.MapGet("/", ListAsync).RequirePermission(BroadcastsView);
        broadcasts.MapGet("/{id:int}", GetAsync).RequirePermission(BroadcastsView);
        return app;
    }

    /// <summary>202 Accepted: рассылка зарегистрирована и поставлена в очередь, ход — в GET /api/broadcasts/{id}.</summary>
    private static async Task<IResult> CreateAsync(BroadcastInput input, Actor actor, BroadcastService service, CancellationToken ct) =>
        (await service.CreateAsync(actor.Access, input, ct)).ToAccepted(b => $"/api/broadcasts/{b.Id}");

    private static async Task<IResult> PreviewAsync(AudienceInput input, BroadcastService service, CancellationToken ct) =>
        (await service.PreviewAsync(input, ct)).ToOk();

    private static async Task<IResult> ListAsync(BroadcastService service, CancellationToken ct) =>
        Results.Ok(await service.ListAsync(ct));

    private static async Task<IResult> GetAsync(int id, BroadcastService service, CancellationToken ct) =>
        (await service.GetAsync(id, ct)).ToOk();
}
