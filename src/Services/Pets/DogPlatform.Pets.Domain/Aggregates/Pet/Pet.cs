using DogPlatform.Pets.Domain.ValueObjects;
using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Pets.Domain.Aggregates.Pet;

public sealed class Pet : AggregateRoot<Guid>
{
    private readonly List<PetPhoto> _photos = [];

    private Pet(
        Guid id,
        Guid ownerId,
        int breedId,
        string name,
        DateTime? birthDate,
        Gender gender,
        decimal? weight,
        string? color,
        string? pedigreeNumber,
        bool isSterilized,
        string? description,
        DateTime createdAt)
        : base(id)
    {
        OwnerId = ownerId;
        BreedId = breedId;
        Name = name;
        BirthDate = birthDate;
        Gender = gender;
        Weight = weight;
        Color = color;
        PedigreeNumber = pedigreeNumber;
        IsSterilized = isSterilized;
        Description = description;
        CreatedAt = createdAt;
    }

    private Pet() { }

    public Guid OwnerId { get; private set; }
    public int BreedId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime? BirthDate { get; private set; }
    public Gender Gender { get; private set; } = null!;
    public decimal? Weight { get; private set; }
    public string? Color { get; private set; }
    public string? PedigreeNumber { get; private set; }
    public bool IsSterilized { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyCollection<PetPhoto> Photos => _photos.AsReadOnly();

    public static Result<Pet> Create(
        Guid id,
        Guid ownerId,
        int breedId,
        string name,
        DateTime? birthDate,
        Gender gender,
        decimal? weight,
        string? color,
        string? pedigreeNumber,
        bool isSterilized,
        string? description,
        DateTime createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return Result.Success(new Pet(
            id,
            ownerId,
            breedId,
            name,
            birthDate,
            gender,
            weight,
            color,
            pedigreeNumber,
            isSterilized,
            description,
            createdAt));
    }

    public void Update(
        string name,
        DateTime? birthDate,
        Gender gender,
        decimal? weight,
        string? color,
        string? pedigreeNumber,
        bool isSterilized,
        string? description,
        DateTime utcNow)
    {
        Name = name;
        BirthDate = birthDate;
        Gender = gender;
        Weight = weight;
        Color = color;
        PedigreeNumber = pedigreeNumber;
        IsSterilized = isSterilized;
        Description = description;
        UpdatedAt = utcNow;
    }

    public void AddPhoto(PetPhoto photo)
    {
        ArgumentNullException.ThrowIfNull(photo);
        _photos.Add(photo);
    }
}
