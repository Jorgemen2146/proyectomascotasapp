using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Pets.Domain.Catalog;

public sealed class Species : Entity<int>
{
    private Species(int id, string name)
        : base(id)
    {
        Name = name;
    }

    private Species() { }

    public string Name { get; private set; } = string.Empty;

    public static Species Create(int id, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Species(id, name);
    }
}
