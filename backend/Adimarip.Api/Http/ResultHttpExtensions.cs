using Adimarip.Domain.Common;

namespace Adimarip.Api.Http;

/// <summary>Единое преобразование Result → HTTP: тело ошибки всегда { "error": "..." }.</summary>
public static class ResultHttpExtensions
{
    public static IResult ToOk<T>(this Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result.Error!);

    public static IResult ToNoContent(this Result result) =>
        result.IsSuccess ? Results.NoContent() : ToProblem(result.Error!);

    public static IResult ToCreated<T>(this Result<T> result, Func<T, string> location) =>
        result.IsSuccess ? Results.Created(location(result.Value), result.Value) : ToProblem(result.Error!);

    public static IResult ToAccepted<T>(this Result<T> result, Func<T, string> location) =>
        result.IsSuccess ? Results.Accepted(location(result.Value), result.Value) : ToProblem(result.Error!);

    public static IResult ToProblem(Error error) =>
        Results.Json(new { error = error.Message }, statusCode: StatusCodeOf(error.Kind));

    private static int StatusCodeOf(ErrorKind kind) => kind switch
    {
        ErrorKind.Validation => StatusCodes.Status400BadRequest,
        ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        ErrorKind.NotConfigured => StatusCodes.Status501NotImplemented,
        _ => StatusCodes.Status500InternalServerError
    };
}
