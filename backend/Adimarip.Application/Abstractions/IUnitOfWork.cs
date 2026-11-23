using Adimarip.Domain.Common;

namespace Adimarip.Application.Abstractions;

/// <summary>
/// Единица работы: все изменения сценария сохраняются одной транзакцией.
/// Нарушение ограничений БД и конфликты параллельной записи превращаются
/// в <see cref="PersistenceConflictException"/>, чтобы сервисы не знали про EF Core и Npgsql.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);

    /// <summary>Сбрасывает все несохранённые изменения (после конфликта параллельной записи).</summary>
    void DiscardChanges();

    /// <summary>
    /// Выполняет сценарий в явной транзакции: фиксирует её при успешном результате
    /// и откатывает при неуспешном или исключении.
    /// </summary>
    Task<TResult> InTransactionAsync<TResult>(Func<Task<TResult>> action, TransactionMode mode, CancellationToken ct)
        where TResult : Result;
}

public enum TransactionMode
{
    ReadCommitted,

    /// <summary>Для конкурентных изменений одной записи (смена статуса).</summary>
    Serializable
}

public sealed class PersistenceConflictException(string message, Exception? inner = null) : Exception(message, inner);

public static class UnitOfWorkExtensions
{
    /// <summary>Сохраняет изменения; конфликт с БД возвращается как ошибка 409 с понятным текстом.</summary>
    public static async Task<Result> SaveAsync(this IUnitOfWork unitOfWork, string conflictMessage, CancellationToken ct)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (PersistenceConflictException)
        {
            return Error.Conflict(conflictMessage);
        }
    }

    /// <summary>То же для сценария в транзакции: конфликт фиксации превращается в ошибку 409.</summary>
    public static async Task<Result<T>> InTransactionAsync<T>(this IUnitOfWork unitOfWork,
        Func<Task<Result<T>>> action, TransactionMode mode, string conflictMessage, CancellationToken ct)
    {
        try
        {
            return await unitOfWork.InTransactionAsync(action, mode, ct);
        }
        catch (PersistenceConflictException)
        {
            return Error.Conflict(conflictMessage);
        }
    }
}
