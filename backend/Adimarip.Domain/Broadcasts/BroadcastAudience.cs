using Adimarip.Domain.Common;

namespace Adimarip.Domain.Broadcasts;

/// <summary>Спецификация аудитории рассылки (паттерн Specification): сама по себе не знает про БД.</summary>
public sealed record BroadcastAudience(AudienceKind Kind, IReadOnlyList<int> OfficeIds, IReadOnlyList<int> RecipientIds)
{
    public const int MaxExplicitItems = 1000;

    public static Result<BroadcastAudience> Create(string? kind, IEnumerable<int>? officeIds, IEnumerable<int>? recipientIds)
    {
        if (!EnumNames.TryParse<AudienceKind>(kind, out var parsed))
            return Error.Validation("Укажите аудиторию: All, Offices или Recipients");
        var offices = (officeIds ?? []).Distinct().ToArray();
        var recipients = (recipientIds ?? []).Distinct().ToArray();
        if (offices.Any(id => id < 1) || recipients.Any(id => id < 1) ||
            offices.Length > MaxExplicitItems || recipients.Length > MaxExplicitItems)
            return Error.Validation("Некорректный список подразделений или адресатов");
        return parsed switch
        {
            AudienceKind.Offices when offices.Length == 0 => Error.Validation("Выберите хотя бы одно подразделение"),
            AudienceKind.Recipients when recipients.Length == 0 => Error.Validation("Выберите хотя бы одного адресата"),
            AudienceKind.All => new BroadcastAudience(AudienceKind.All, [], []),
            AudienceKind.Offices => new BroadcastAudience(AudienceKind.Offices, offices, []),
            _ => new BroadcastAudience(AudienceKind.Recipients, [], recipients)
        };
    }

    public string Describe() => Kind switch
    {
        AudienceKind.All => "Все действующие адресаты",
        AudienceKind.Offices => $"Подразделения: {string.Join(", ", OfficeIds)}",
        _ => $"Выбранные адресаты: {RecipientIds.Count}"
    };
}
