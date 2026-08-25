using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using DogPlatform.Genealogy.Application.Features.Relationships;
using DogPlatform.Genealogy.Domain.Relationships;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DogPlatform.Genealogy.Infrastructure.Services;

public sealed class GenealogyPetService(HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor) : IGenealogyPetService
{
    public async Task<GenealogyPetContext?> GetOwnedPetAsync(Guid petId, Guid ownerUserId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            "api/v1/pets/mine?pageNumber=1&pageSize=100");
        var authorization = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization) &&
            AuthenticationHeaderValue.TryParse(authorization, out var header))
            request.Headers.Authorization = header;
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        var page = await response.Content.ReadFromJsonAsync<PetsPage>(cancellationToken);
        var pet = page?.Items.FirstOrDefault(item => item.Id == petId);
        return pet is null ? null : new GenealogyPetContext(pet.Id, ownerUserId, pet.Name,
            pet.SpeciesId, pet.BreedName, pet.Sex, pet.BirthDate, pet.MainPhotoUrl);
    }

    public async Task<IReadOnlyDictionary<Guid, GenealogyPetContext>> GetPetContextsAsync(
        IReadOnlyCollection<Guid> petIds, CancellationToken cancellationToken = default)
    {
        if (petIds.Count == 0) return new Dictionary<Guid, GenealogyPetContext>();
        var contexts = await httpClient.GetFromJsonAsync<PetInternalContext[]>(
            "api/v1/internal/pets/vaccination-context", cancellationToken) ?? [];
        var wanted = petIds.ToHashSet();
        return contexts.Where(item => wanted.Contains(item.PetId)).ToDictionary(item => item.PetId,
            item => new GenealogyPetContext(item.PetId, item.UserId, item.PetName,
                item.SpeciesId, null, "Unknown", item.BirthDate, null));
    }

    private sealed record PetsPage(IReadOnlyCollection<PetItem> Items);
    private sealed record PetItem(Guid Id, string Name, int SpeciesId, string BreedName,
        string Sex, DateTime? BirthDate, string? MainPhotoUrl);
    private sealed record PetInternalContext(Guid UserId, Guid PetId, int SpeciesId,
        DateTime? BirthDate, string PetName);
}

public sealed class InvitationTokenService : IInvitationTokenService
{
    public string GenerateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public string HashToken(string token) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

public sealed class DevelopmentGenealogyInvitationEmailSender(
    ILogger<DevelopmentGenealogyInvitationEmailSender> logger) : IGenealogyInvitationEmailSender
{
    public Task SendAsync(RelationshipInvitation invitation, string token,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Genealogy email provider is not configured; InvitationId={InvitationId} remains available for in-app sharing.",
            invitation.Id);
        return Task.CompletedTask;
    }
}

public sealed class DevelopmentGenealogyNotificationPublisher(
    ILogger<DevelopmentGenealogyNotificationPublisher> logger) : IGenealogyNotificationPublisher
{
    public Task PublishAsync(string eventType, Guid userId, Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Genealogy notification integration is not configured; EventType={EventType} InvitationId={InvitationId}.",
            eventType, invitationId);
        return Task.CompletedTask;
    }
}
