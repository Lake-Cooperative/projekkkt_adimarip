namespace Adimarip.Domain.Common;

/// <summary>Категория ошибки. API однозначно переводит её в HTTP-код.</summary>
public enum ErrorKind
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    NotConfigured
}

/// <summary>Бизнес-ошибка с понятным пользователю текстом.</summary>
public sealed record Error(ErrorKind Kind, string Message)
{
    public static Error Validation(string message) => new(ErrorKind.Validation, message);
    public static Error NotFound(string message) => new(ErrorKind.NotFound, message);
    public static Error Conflict(string message) => new(ErrorKind.Conflict, message);
    public static Error Unauthorized(string message) => new(ErrorKind.Unauthorized, message);
    public static Error Forbidden(string message = "Недостаточно прав") => new(ErrorKind.Forbidden, message);
    public static Error NotConfigured(string message) => new(ErrorKind.NotConfigured, message);
}

/// <summary>
/// Результат операции без исключений для ожидаемых ошибок (паттерн Result).
/// Исключения остаются только для действительно исключительных ситуаций.
/// </summary>
public class Result
{
    protected Result(Error? error) => Error = error;

    public Error? Error { get; }
    public bool IsSuccess => Error is null;
    public bool IsFailure => !IsSuccess;

    public static Result Success() => new(null);
    public static Result Failure(Error error) => new(error);
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, Error? error) : base(error) => _value = value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Нельзя прочитать значение неуспешного результата: {Error!.Message}");

    public static Result<T> Success(T value) => new(value, null);
    public static new Result<T> Failure(Error error) => new(default, error);

    public Result<TOut> Map<TOut>(Func<T, TOut> map) =>
        IsSuccess ? Result<TOut>.Success(map(Value)) : Result<TOut>.Failure(Error!);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
