using DogPlatform.Identity.Application.Features.Authentication.Login;
using DogPlatform.Identity.Application.Features.Legal;
using DogPlatform.Identity.Domain.Aggregates.ExternalLogin;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Identity.Application.Features.Authentication.External;

public sealed record ExternalIdentity(
    ExternalAuthProvider Provider,
    string ProviderUserId,
    string? Email,
    bool EmailVerified,
    string? FirstName,
    string? LastName,
    string? PictureUrl);

public enum ExternalValidationFailure
{
    None,
    InvalidToken,
    ExpiredToken,
    ProviderNotConfigured,
    ProviderUnavailable
}

public sealed record ExternalIdentityValidationResult(
    ExternalIdentity? Identity,
    ExternalValidationFailure Failure)
{
    public bool IsSuccess => Identity is not null && Failure == ExternalValidationFailure.None;
    public static ExternalIdentityValidationResult Success(ExternalIdentity identity) => new(identity, ExternalValidationFailure.None);
    public static ExternalIdentityValidationResult Failed(ExternalValidationFailure failure) => new(null, failure);
}

public interface IExternalIdentityValidator
{
    Task<ExternalIdentityValidationResult> ValidateAsync(
        ExternalAuthProvider provider, string credential, string? nonce = null,
        CancellationToken cancellationToken = default);
}

public interface IExternalRegistrationTicketService
{
    string Issue(ExternalIdentity identity, DateTime utcNow);
    ExternalIdentityValidationResult Validate(string ticket, DateTime utcNow);
}

public sealed record ExternalAuthOutcome(
    LoginResponse? Session,
    string? ActionCode,
    string? RegistrationToken,
    IReadOnlyList<string> MissingFields)
{
    public bool IsAuthenticated => Session is not null;
    public static ExternalAuthOutcome Authenticated(LoginResponse session) => new(session, null, null, []);
    public static ExternalAuthOutcome Action(string code, string? ticket, params string[] missingFields) =>
        new(null, code, ticket, missingFields);
}

public sealed record ExternalAuthCommand(
    ExternalAuthProvider Provider,
    string Credential,
    string? Nonce,
    IReadOnlyList<LegalConsentSelection>? LegalConsents = null)
    : IRequest<Result<ExternalAuthOutcome>>;

public sealed record CompleteExternalRegistrationCommand(
    string RegistrationToken,
    string? Email,
    string? FirstName,
    string? LastName,
    IReadOnlyList<LegalConsentSelection>? LegalConsents = null)
    : IRequest<Result<ExternalAuthOutcome>>;

public sealed record LinkExternalLoginCommand(
    Guid UserId,
    ExternalAuthProvider Provider,
    string Credential,
    string? Nonce = null) : IRequest<Result>;

public static class ExternalAuthErrors
{
    public static readonly Error InvalidToken = Error.Unauthorized(
        "EXTERNAL_TOKEN_INVALID", "The external credential is invalid.");
    public static readonly Error ExpiredToken = Error.Unauthorized(
        "EXTERNAL_TOKEN_EXPIRED", "The external credential has expired.");
    public static readonly Error ProviderNotConfigured = Error.Failure(
        "EXTERNAL_PROVIDER_NOT_CONFIGURED", "The external provider is not configured.");
    public static readonly Error ProviderUnavailable = Error.Failure(
        "EXTERNAL_LOGIN_FAILED", "The external provider could not be reached.");
    public static readonly Error AccountLinkRequired = Error.Conflict(
        "EXTERNAL_ACCOUNT_LINK_REQUIRED", "Authenticate with the existing PetLife account before linking this provider.");
    public static readonly Error EmailRequired = Error.Validation(
        "EXTERNAL_EMAIL_REQUIRED", "An email address is required to complete registration.");
    public static readonly Error RegistrationRequired = Error.Validation(
        "EXTERNAL_REGISTRATION_REQUIRED", "Additional profile information is required to complete registration.");
    public static readonly Error RegistrationTicketInvalid = Error.Unauthorized(
        "EXTERNAL_REGISTRATION_TOKEN_INVALID", "The external registration token is invalid or expired.");
    public static readonly Error LoginAlreadyLinked = Error.Conflict(
        "EXTERNAL_LOGIN_ALREADY_LINKED", "This external identity is already linked to an account.");
    public static readonly Error InvalidRequest = Error.Validation(
        "EXTERNAL_REQUEST_INVALID", "The external authentication request is invalid.");
}
