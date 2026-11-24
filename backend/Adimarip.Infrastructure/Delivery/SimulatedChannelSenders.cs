using System.Text.RegularExpressions;
using Adimarip.Application.Delivery;
using Adimarip.Domain.Notices;
using Microsoft.Extensions.Logging;

namespace Adimarip.Infrastructure.Delivery;

public sealed class SimulationOptions
{
    public const string Section = "Delivery:Simulation";

    /// <summary>Имитация задержки провайдера, мс.</summary>
    public int LatencyMs { get; set; } = 15;
    /// <summary>Доля временных сбоев (0–1) — чтобы на демо было видно повторы.</summary>
    public double TransientFailureRate { get; set; }
}

/// <summary>
/// Базовый имитатор канала (паттерн Template Method): общий сценарий — задержка,
/// «сбойные» тестовые адреса, случайные временные ошибки; проверку адреса
/// определяет конкретный канал. Внешние SMS/e-mail API не вызываются.
/// </summary>
internal abstract class SimulatedChannelSender(SimulationOptions options, ILogger logger) : IChannelSender
{
    public abstract DeliveryChannel Channel { get; }
    public bool IsSimulated => true;

    public async Task<SendOutcome> SendAsync(OutgoingMessage message, CancellationToken ct)
    {
        if (options.LatencyMs > 0)
            await Task.Delay(options.LatencyMs, ct);

        var addressError = ValidateAddress(message.Address);
        if (addressError is not null)
            return SendOutcome.Permanent(addressError);
        // Адреса со словом «fail» — договорённость для демонстрации отказов.
        if (message.Address.Contains("fail", StringComparison.OrdinalIgnoreCase))
            return SendOutcome.Permanent("Провайдер отклонил адрес (тестовый сбой)");
        if (options.TransientFailureRate > 0 && Random.Shared.NextDouble() < options.TransientFailureRate)
            return SendOutcome.Temporary("Провайдер временно недоступен (имитация)");

        logger.LogDebug("Simulated {Channel} delivery to {Address}", Channel, message.Address);
        return SendOutcome.Success;
    }

    /// <summary>null — адрес корректен, иначе текст ошибки.</summary>
    protected abstract string? ValidateAddress(string address);
}

internal sealed partial class SimulatedEmailSender(SimulationOptions options, ILogger<SimulatedEmailSender> logger)
    : SimulatedChannelSender(options, logger)
{
    public override DeliveryChannel Channel => DeliveryChannel.Email;

    protected override string? ValidateAddress(string address) =>
        EmailPattern().IsMatch(address) ? null : "Некорректный e-mail";

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+$")]
    private static partial Regex EmailPattern();
}

internal sealed partial class SimulatedSmsSender(SimulationOptions options, ILogger<SimulatedSmsSender> logger)
    : SimulatedChannelSender(options, logger)
{
    public override DeliveryChannel Channel => DeliveryChannel.Sms;

    protected override string? ValidateAddress(string address) =>
        PhonePattern().IsMatch(address) ? null : "Некорректный номер телефона";

    [GeneratedRegex(@"^\+?[0-9][0-9\s()-]{9,19}$")]
    internal static partial Regex PhonePattern();
}

internal sealed class SimulatedMaxSender(SimulationOptions options, ILogger<SimulatedMaxSender> logger)
    : SimulatedChannelSender(options, logger)
{
    public override DeliveryChannel Channel => DeliveryChannel.Max;

    // В мессенджере MAX адресат определяется номером телефона.
    protected override string? ValidateAddress(string address) =>
        SimulatedSmsSender.PhonePattern().IsMatch(address) ? null : "Некорректный номер для MAX";
}

internal sealed class SimulatedPortalSender(SimulationOptions options, ILogger<SimulatedPortalSender> logger)
    : SimulatedChannelSender(options, logger)
{
    public override DeliveryChannel Channel => DeliveryChannel.Portal;

    // Личный кабинет на портале есть у каждого работника, адрес — любой непустой идентификатор.
    protected override string? ValidateAddress(string address) =>
        string.IsNullOrWhiteSpace(address) ? "Не указан получатель на портале" : null;
}
