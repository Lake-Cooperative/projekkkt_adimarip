namespace Adimarip.Data.Entities;

public sealed class Address
{
    public int Id { get; set; }
    public int RecipientId { get; set; }
    public string? PostalCode { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
    public string? Street { get; set; }
    public string? House { get; set; }
    public string? Apartment { get; set; }

    public Recipient Recipient { get; set; } = null!;
}
