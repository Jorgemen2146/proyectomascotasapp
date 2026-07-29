using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Pets.Domain.Catalog;

public sealed class Breed : Entity<int>
{
    private Breed(int id, int speciesId, string name)
        : base(id)
    {
        SpeciesId = speciesId;
        Name = name;
    }

    private Breed() { }

    public int SpeciesId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public static Breed Create(int id, int speciesId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Breed(id, speciesId, name);
    }
}
