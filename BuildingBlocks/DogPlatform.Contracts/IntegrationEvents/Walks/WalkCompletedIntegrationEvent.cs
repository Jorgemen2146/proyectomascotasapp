namespace DogPlatform.Contracts.IntegrationEvents.Walks;

public sealed class WalkCompletedIntegrationEvent : IntegrationEvent
{
    public WalkCompletedIntegrationEvent(Guid walkId, Guid petId, Guid walkerId, DateTime completedAt)
    {
        WalkId = walkId;
        PetId = petId;
        WalkerId = walkerId;
        CompletedAt = completedAt;
    }

    public Guid WalkId { get; }
    public Guid PetId { get; }
    public Guid WalkerId { get; }
    public DateTime CompletedAt { get; }
}
