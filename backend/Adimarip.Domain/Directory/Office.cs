namespace Adimarip.Domain.Directory;

/// <summary>Подразделение.</summary>
public sealed class Office
{
    private Office() { } // для EF Core

    public Office(string name) => Name = name;

    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
}
