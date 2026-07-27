namespace DogPlatform.Contracts.IntegrationEvents.Pets;

public sealed class PetCreatedIntegrationEvent : IntegrationEvent
{
    public PetCreatedIntegrationEvent(Guid petId, string name, string breed, Guid ownerId)
    {
        PetId = petId;
        Name = name;
        Breed = breed;
        OwnerId = ownerId;
    }

    public Guid PetId { get; }
    public string Name { get; }
    public string Breed { get; }
    public Guid OwnerId { get; }
}
