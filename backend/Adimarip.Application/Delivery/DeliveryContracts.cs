using Adimarip.Domain.Notices;

namespace Adimarip.Application.Delivery;

/// <summary>Сообщение для отправки по одному каналу.</summary>
public sealed record OutgoingMessage(DeliveryChannel Channel, string Address, string Subject, string Body, bool Urgent = false);

/// <summary>Ответ канала на одну попытку. Transient — сбой временный, попытку имеет смысл повторить.</summary>
public sealed record SendOutcome(bool Delivered, bool Transient, string? Error)
{
    public static SendOutcome Success { get; } = new(true, false, null);
    public static SendOutcome Permanent(string error) => new(false, false, error);
    public static SendOutcome Temporary(string error) => new(false, true, error);
}

public sealed record AttemptRecord(bool Delivered, DateTimeOffset At, string? Error);

/// <summary>Итог доставки сообщения с журналом всех попыток.</summary>
public sealed record DeliveryReport(IReadOnlyList<AttemptRecord> Attempts, bool Simulated)
{
    public bool Delivered => Attempts.Count > 0 && Attempts[^1].Delivered;
    public string? LastError => Attempts.Count > 0 ? Attempts[^1].Error : null;
}

/// <summary>Стратегия отправки для конкретного канала (e-mail, SMS, портал, MAX).</summary>
public interface IChannelSender
{
    DeliveryChannel Channel { get; }
    bool IsSimulated { get; }
    Task<SendOutcome> SendAsync(OutgoingMessage message, CancellationToken ct);
}

/// <summary>Единая точка отправки: выбор канала + политика повторов.</summary>
public interface INotificationGateway
{
    /// <summary>false — провайдер не настроен (вне демо-режима реальной отправки нет).</summary>
    bool IsConfigured { get; }
    Task<DeliveryReport> DeliverAsync(OutgoingMessage message, CancellationToken ct);
}

public sealed class DeliveryOptions
{
    public const string Section = "Delivery";

    /// <summary>Разрешена ли демонстрационная (имитационная) отправка.</summary>
    public bool SimulationEnabled { get; set; }
    public int MaxAttempts { get; set; } = 3;
    public int RetryBaseDelayMs { get; set; } = 50;
    /// <summary>Сколько сообщений рассылки отправляется одновременно.</summary>
    public int MaxParallelism { get; set; } = 16;
    /// <summary>Сколько оповещений рассылки обрабатывается за одну транзакцию.</summary>
    public int BatchSize { get; set; } = 100;
    /// <summary>Сколько рассылок обрабатывается одновременно (срочная не ждёт окончания плановой).</summary>
    public int Consumers { get; set; } = 2;
}
