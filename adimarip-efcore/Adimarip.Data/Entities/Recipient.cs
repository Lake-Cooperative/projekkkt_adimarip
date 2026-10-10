namespace Adimarip.Data.Entities;

public sealed class Recipient
{
    public int Id { get; set; }
    public string ExternalIdentifier { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public DateOnly DateOfBirth { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public Address Address { get; set; } = null!;
    public ICollection<Notice> Notices { get; set; } = new List<Notice>();
}
