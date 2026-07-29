using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Pets.Domain.Aggregates.Pet;

public sealed class PetPhoto : Entity<Guid>
{
    private PetPhoto(
        Guid id,
        Guid petId,
        string url,
        bool isMain,
        DateTime createdAt)
        : base(id)
    {
        PetId = petId;
        Url = url;
        IsMain = isMain;
        CreatedAt = createdAt;
    }

    private PetPhoto() { }

    public Guid PetId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public bool IsMain { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static PetPhoto Create(
        Guid id,
        Guid petId,
        string url,
        bool isMain,
        DateTime createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        return new PetPhoto(id, petId, url, isMain, createdAt);
    }
}
