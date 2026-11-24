using Adimarip.Domain.Common;
using Adimarip.Domain.Notices;

namespace Adimarip.Domain.Directory;

/// <summary>Сведения для карточки адресата.</summary>
public sealed record RecipientProfile(
    string? ExternalIdentifier,
    string? FullName,
    DateOnly? DateOfBirth,
    string? Email,
    string? Phone,
    int? OfficeId,
    AddressData? Address);

/// <summary>Адресат (он же служащий, если к нему привязана учётная запись).</summary>
public sealed class Recipient
{
    private Recipient() { } // для EF Core

    public int Id { get; private set; }
    public string ExternalIdentifier { get; private set; } = null!;
    public string FullName { get; private set; } = null!;
    public DateOnly DateOfBirth { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public int? OfficeId { get; private set; }

    public Address Address { get; private set; } = null!;
    public RecipientArchive? ArchiveRecord { get; private set; }

    public bool IsArchived => ArchiveRecord is not null;

    public static Result<Recipient> Create(RecipientProfile profile, DateOnly today)
    {
        var identifier = TextRules.Clean(profile.ExternalIdentifier);
        var fullName = TextRules.Clean(profile.FullName);
        var email = TextRules.CleanOptional(profile.Email);
        var phone = TextRules.CleanOptional(profile.Phone);
        if (!TextRules.HasLength(identifier, 2, 100))
            return Error.Validation("Идентификатор адресата: от 2 до 100 символов");
        if (!TextRules.HasLength(fullName, 2, 250) || profile.DateOfBirth is not { } birthDate || birthDate > today ||
            !TextRules.FitsOptional(email, 320) || !TextRules.FitsOptional(phone, 50))
            return Error.Validation("Проверьте идентификатор, ФИО, дату рождения и контакты");

        var address = new Address(profile.Address ?? AddressData.Empty);
        if (!address.FitsLimits())
            return Error.Validation("Слишком длинное значение в адресе");

        return new Recipient
        {
            ExternalIdentifier = identifier,
            FullName = fullName,
            DateOfBirth = birthDate,
            Email = email,
            Phone = phone,
            OfficeId = profile.OfficeId,
            Address = address
        };
    }

    public Result Archive(string? reason, string actor, DateTimeOffset now)
    {
        var cleanReason = TextRules.Clean(reason);
        if (!TextRules.HasLength(cleanReason, 3, 1000))
            return Error.Validation("Укажите причину списания (3–1000 символов)");
        if (IsArchived)
            return Error.Conflict("Адресат уже списан");
        ArchiveRecord = new RecipientArchive(Id, cleanReason, actor, now);
        return Result.Success();
    }

    /// <summary>Привязывает работника к подразделению (используется для адресных рассылок).</summary>
    public void AssignOffice(int? officeId) => OfficeId = officeId;

    /// <summary>Фамилия — первое слово ФИО («Фамилия Имя Отчество»).</summary>
    public static string SurnameOf(string fullName) =>
        fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? fullName;
}
