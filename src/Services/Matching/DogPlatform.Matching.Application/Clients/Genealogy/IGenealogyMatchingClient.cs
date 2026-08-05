namespace DogPlatform.Matching.Application.Clients.Genealogy;

/// <summary>
/// Abstraction over GenealogyService v3, consumed by Matching Application handlers.
/// Reuses Genealogy's real endpoints/contracts; Matching does not duplicate
/// kinship or inbreeding calculations.
/// </summary>
public interface IGenealogyMatchingClient
{
    Task<RelationshipEvaluationResult?> CalculateRelationshipAsync(
        Guid sourcePetId, Guid candidatePetId, CancellationToken cancellationToken = default);

    Task<OffspringInbreedingEstimate?> EstimateOffspringInbreedingAsync(
        Guid sourcePetId, Guid candidatePetId, CancellationToken cancellationToken = default);

    Task<PedigreeStatisticsSummary?> GetPedigreeStatisticsAsync(
        Guid petId, CancellationToken cancellationToken = default);
}
