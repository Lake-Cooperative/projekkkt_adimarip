using Adimarip.Domain.Common;

namespace Adimarip.Domain.Directory;

public sealed record AddressData(string? PostalCode, string? Region, string? City, string? Street, string? House, string? Apartment)
{
    public static AddressData Empty { get; } = new(null, null, null, null, null, null);
}

public sealed class Address
{
    private Address() { } // для EF Core

    internal Address(AddressData data)
    {
        PostalCode = TextRules.CleanOptional(data.PostalCode);
        Region = TextRules.CleanOptional(data.Region);
        City = TextRules.CleanOptional(data.City);
        Street = TextRules.CleanOptional(data.Street);
        House = TextRules.CleanOptional(data.House);
        Apartment = TextRules.CleanOptional(data.Apartment);
    }

    public int Id { get; private set; }
    public int RecipientId { get; private set; }
    public string? PostalCode { get; private set; }
    public string? Region { get; private set; }
    public string? City { get; private set; }
    public string? Street { get; private set; }
    public string? House { get; private set; }
    public string? Apartment { get; private set; }

    internal bool FitsLimits() =>
        TextRules.FitsOptional(PostalCode, 20) && TextRules.FitsOptional(Region, 150) && TextRules.FitsOptional(City, 150) &&
        TextRules.FitsOptional(Street, 200) && TextRules.FitsOptional(House, 30) && TextRules.FitsOptional(Apartment, 30);
}
