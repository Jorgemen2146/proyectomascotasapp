namespace DogPlatform.Identity.API.Requests.Authentication;

public sealed record GoogleExternalAuthRequest(
    string IdToken,
    IReadOnlyList<LegalConsentRequest>? LegalConsents = null);

public sealed record FacebookExternalAuthRequest(
    string AccessToken,
    IReadOnlyList<LegalConsentRequest>? LegalConsents = null);

public sealed record AppleExternalAuthRequest(
    string IdToken,
    string Nonce,
    IReadOnlyList<LegalConsentRequest>? LegalConsents = null);

public sealed record CompleteExternalRegistrationRequest(
    string RegistrationToken,
    string? Email,
    string? FirstName,
    string? LastName,
    IReadOnlyList<LegalConsentRequest>? LegalConsents = null);

public sealed record LinkExternalLoginRequest(string Credential, string? Nonce = null);
