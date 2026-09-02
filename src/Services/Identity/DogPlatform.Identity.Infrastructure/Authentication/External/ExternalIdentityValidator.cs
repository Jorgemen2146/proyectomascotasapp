using DogPlatform.Identity.Application.Features.Authentication.External;
using DogPlatform.Identity.Domain.Aggregates.ExternalLogin;

namespace DogPlatform.Identity.Infrastructure.Authentication.External;

internal sealed class ExternalIdentityValidator(IEnumerable<IProviderIdentityValidator> validators)
    : IExternalIdentityValidator
{
    private readonly IReadOnlyDictionary<ExternalAuthProvider, IProviderIdentityValidator> _validators =
        validators.ToDictionary(x => x.Provider);

    public Task<ExternalIdentityValidationResult> ValidateAsync(
        ExternalAuthProvider provider, string credential, string? nonce = null,
        CancellationToken cancellationToken = default) =>
        _validators.TryGetValue(provider, out var validator)
            ? validator.ValidateAsync(credential, nonce, cancellationToken)
            : Task.FromResult(ExternalIdentityValidationResult.Failed(
                ExternalValidationFailure.ProviderNotConfigured));
}
