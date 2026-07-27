namespace DogPlatform.Contracts.IntegrationEvents.Matching;

public sealed class MatchFoundIntegrationEvent : IntegrationEvent
{
    public MatchFoundIntegrationEvent(Guid matchId, Guid petAId, Guid petBId)
    {
        MatchId = matchId;
        PetAId = petAId;
        PetBId = petBId;
    }

    public Guid MatchId { get; }
    public Guid PetAId { get; }
    public Guid PetBId { get; }
}
