using System.Net;
using System.Net.Http.Json;
using DogPlatform.Matching.Application.Clients.Genealogy;
using DogPlatform.Matching.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DogPlatform.Matching.Infrastructure.Clients;

/// <summary>
/// Typed HttpClient consuming GenealogyService v3 real endpoints:
/// GET /api/v1/genealogy/relationship?petId1&amp;petId2
/// GET /api/v1/genealogy/{petId}/statistics
/// Matching never recalculates kinship or inbreeding coefficients itself.
/// </summary>
public sealed class GenealogyMatchingClient : IGenealogyMatchingClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GenealogyMatchingClient> _logger;

    public GenealogyMatchingClient(HttpClient httpClient, ILogger<GenealogyMatchingClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<RelationshipEvaluationResult?> CalculateRelationshipAsync(
        Guid sourcePetId, Guid candidatePetId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/v1/genealogy/relationship?petId1={sourcePetId}&petId2={candidatePetId}",
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<GenealogyRelationshipDto>(
                cancellationToken: cancellationToken);

            if (dto is null)
                return null;

            var parsed = Enum.TryParse<RelationshipTypeSnapshot>(dto.RelationshipType, out var relationshipType);

            return new RelationshipEvaluationResult(
                parsed ? relationshipType : RelationshipTypeSnapshot.UnknownDueToIncompletePedigree,
                dto.IsCloseRelative,
                (double)dto.EstimatedRelationshipCoefficientPercentage / 100.0,
                GenealogyValidationStatus.Validated,
                dto.Warnings ?? []);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Error calculating relationship between PetId1={PetId1} and PetId2={PetId2}",
                sourcePetId,
                candidatePetId);
            return null;
        }
    }

    public async Task<OffspringInbreedingEstimate?> EstimateOffspringInbreedingAsync(
        Guid sourcePetId, Guid candidatePetId, CancellationToken cancellationToken = default)
    {
        // GenealogyService v3 does not expose a dedicated offspring-inbreeding endpoint;
        // the estimated relationship coefficient between the two candidates is used
        // as the proxy for the offspring's expected inbreeding coefficient, consistent
        // with standard pedigree analysis (Wright's coefficient of relationship).
        var relationship = await CalculateRelationshipAsync(sourcePetId, candidatePetId, cancellationToken);

        if (relationship is null)
            return null;

        return new OffspringInbreedingEstimate(
            relationship.EstimatedRelationshipCoefficient,
            relationship.Status,
            relationship.Warnings);
    }

    public async Task<PedigreeStatisticsSummary?> GetPedigreeStatisticsAsync(
        Guid petId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/v1/genealogy/{petId}/statistics", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<GenealogyStatisticsDto>(
                cancellationToken: cancellationToken);

            if (dto is null)
                return null;

            return new PedigreeStatisticsSummary(
                dto.PedigreeCompletenessPercentage,
                GenealogyValidationStatus.Validated,
                dto.Warnings ?? []);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error retrieving pedigree statistics for PetId={PetId}", petId);
            return null;
        }
    }

    // Minimal DTOs mapping GenealogyService v3 response shapes.
    private sealed record GenealogyRelationshipDto(
        string RelationshipType,
        bool IsCloseRelative,
        decimal EstimatedRelationshipCoefficientPercentage,
        IReadOnlyList<string>? Warnings);

    private sealed record GenealogyStatisticsDto(
        decimal PedigreeCompletenessPercentage,
        IReadOnlyList<string>? Warnings);
}
