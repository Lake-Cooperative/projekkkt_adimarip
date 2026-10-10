namespace Adimarip.Data.Entities;

public sealed class Office
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    public ICollection<Notice> Notices { get; set; } = new List<Notice>();
}
