using System.Data;
using Adimarip.Application.Abstractions;
using Adimarip.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Adimarip.Infrastructure.Persistence;

/// <summary>Единица работы поверх DbContext: переводит ошибки EF/Npgsql в исключение уровня приложения.</summary>
internal sealed class UnitOfWork(AdimaripDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new PersistenceConflictException("Запись изменена параллельно", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new PersistenceConflictException("Данные не прошли ограничения БД", ex);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            throw new PersistenceConflictException("Конфликт сериализуемой транзакции", ex);
        }
    }

    public void DiscardChanges() => db.ChangeTracker.Clear();

    public async Task<TResult> InTransactionAsync<TResult>(Func<Task<TResult>> action, TransactionMode mode, CancellationToken ct)
        where TResult : Result
    {
        if (db.Database.CurrentTransaction is not null)
            return await action(); // уже внутри транзакции — вложенную не открываем

        var isolation = mode == TransactionMode.Serializable ? IsolationLevel.Serializable : IsolationLevel.ReadCommitted;
        await using var transaction = await db.Database.BeginTransactionAsync(isolation, ct);
        try
        {
            var result = await action();
            if (result.IsSuccess) await transaction.CommitAsync(ct);
            else await transaction.RollbackAsync(ct);
            return result;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            throw new PersistenceConflictException("Конфликт сериализуемой транзакции", ex);
        }
    }
}
