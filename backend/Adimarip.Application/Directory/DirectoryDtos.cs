using Adimarip.Domain.Directory;

namespace Adimarip.Application.Directory;

public sealed record OfficeItem(int Id, string Name);

public sealed record RecipientListItem(int Id, string ExternalIdentifier, string FullName, DateOnly DateOfBirth,
    string? Email, string? Phone, int? OfficeId, string? OfficeName);

public sealed record AddressInput(string? PostalCode, string? Region, string? City, string? Street, string? House, string? Apartment)
{
    public AddressData ToData() => new(PostalCode, Region, City, Street, House, Apartment);
}

public sealed record RecipientInput(string? ExternalIdentifier, string? FullName, DateOnly? DateOfBirth,
    string? Email, string? Phone, AddressInput? Address, int? OfficeId = null);

public sealed record RecipientCreated(int Id, string FullName, string ExternalIdentifier);

public sealed record ArchiveRecipientInput(string? Reason);

public sealed record RecipientArchived(int RecipientId, DateTimeOffset ArchivedAt, string Reason);
