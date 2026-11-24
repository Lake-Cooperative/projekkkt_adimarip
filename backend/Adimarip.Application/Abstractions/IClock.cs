namespace Adimarip.Application.Abstractions;

/// <summary>Источник времени. Подменяется в тестах, чтобы проверять правила дат детерминированно.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
    DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
