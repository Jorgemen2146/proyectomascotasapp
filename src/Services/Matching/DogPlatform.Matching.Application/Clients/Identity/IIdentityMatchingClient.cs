namespace DogPlatform.Matching.Application.Clients.Identity;

public sealed record MatchingOwnerContact(string DisplayName, string? PhoneNumber);

public interface IIdentityMatchingClient
{
    Task<MatchingOwnerContact?> GetMatchingContactAsync(Guid userId,
        CancellationToken cancellationToken = default);
}
