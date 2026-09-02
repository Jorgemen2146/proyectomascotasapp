using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DogPlatform.Identity.Application.Features.Authentication.External;
using DogPlatform.Identity.Domain.Aggregates.ExternalLogin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace DogPlatform.Identity.Infrastructure.Authentication.External;

internal interface IProviderIdentityValidator
{
    ExternalAuthProvider Provider { get; }
    Task<ExternalIdentityValidationResult> ValidateAsync(
        string credential, string? nonce, CancellationToken cancellationToken);
}

internal abstract class OidcExternalIdentityValidator(
    string metadataAddress,
    ILogger logger) : IProviderIdentityValidator
{
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _configuration = new(
        metadataAddress, new OpenIdConnectConfigurationRetriever(), new HttpDocumentRetriever
        {
            RequireHttps = true
        });

    public abstract ExternalAuthProvider Provider { get; }
    protected abstract IReadOnlyCollection<string> Audiences { get; }
    protected abstract IReadOnlyCollection<string> Issuers { get; }

    public async Task<ExternalIdentityValidationResult> ValidateAsync(
        string credential, string? nonce, CancellationToken cancellationToken)
    {
        if (Audiences.Count == 0)
            return ExternalIdentityValidationResult.Failed(ExternalValidationFailure.ProviderNotConfigured);
        var errorId = Guid.NewGuid();
        try
        {
            var configuration = await _configuration.GetConfigurationAsync(cancellationToken);
            var principal = ValidateJwt(credential, configuration.SigningKeys,
                Issuers, Audiences);
            var result = Map(principal, nonce);
            logger.LogInformation("External authentication validation completed. Provider={Provider} Success={Success} ErrorId={ErrorId}",
                Provider, result.IsSuccess, errorId);
            return result;
        }
        catch (SecurityTokenExpiredException)
        {
            logger.LogWarning("External authentication token expired. Provider={Provider} ErrorId={ErrorId}", Provider, errorId);
            return ExternalIdentityValidationResult.Failed(ExternalValidationFailure.ExpiredToken);
        }
        catch (SecurityTokenException)
        {
            logger.LogWarning("External authentication token rejected. Provider={Provider} ErrorId={ErrorId}", Provider, errorId);
            return ExternalIdentityValidationResult.Failed(ExternalValidationFailure.InvalidToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "External provider unavailable. Provider={Provider} ErrorId={ErrorId}", Provider, errorId);
            return ExternalIdentityValidationResult.Failed(ExternalValidationFailure.ProviderUnavailable);
        }
    }

    internal static ClaimsPrincipal ValidateJwt(string credential,
        IEnumerable<SecurityKey> signingKeys, IEnumerable<string> issuers,
        IEnumerable<string> audiences)
    {
        var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = signingKeys,
                ValidateIssuer = true,
                ValidIssuers = issuers,
                ValidateAudience = true,
                ValidAudiences = audiences,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        return new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(
            credential, parameters, out _);
    }

    protected abstract ExternalIdentityValidationResult Map(ClaimsPrincipal principal, string? nonce);

    protected static string? Claim(ClaimsPrincipal principal, string name) =>
        principal.FindFirst(name)?.Value;
    protected static bool BooleanClaim(ClaimsPrincipal principal, string name) =>
        bool.TryParse(Claim(principal, name), out var value) && value;
}

internal sealed class GoogleIdentityValidator(
    IOptions<GoogleExternalAuthOptions> options,
    ILogger<GoogleIdentityValidator> logger)
    : OidcExternalIdentityValidator(
        "https://accounts.google.com/.well-known/openid-configuration", logger)
{
    public override ExternalAuthProvider Provider => ExternalAuthProvider.Google;
    protected override IReadOnlyCollection<string> Audiences => options.Value.ClientIds;
    protected override IReadOnlyCollection<string> Issuers =>
        ["https://accounts.google.com", "accounts.google.com"];

    protected override ExternalIdentityValidationResult Map(ClaimsPrincipal principal, string? nonce)
    {
        var subject = Claim(principal, JwtRegisteredClaimNames.Sub)
                      ?? Claim(principal, ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(subject))
            return ExternalIdentityValidationResult.Failed(ExternalValidationFailure.InvalidToken);
        return ExternalIdentityValidationResult.Success(new ExternalIdentity(
            Provider, subject, Claim(principal, JwtRegisteredClaimNames.Email),
            BooleanClaim(principal, "email_verified"), Claim(principal, "given_name"),
            Claim(principal, "family_name"), Claim(principal, "picture")));
    }
}

internal sealed class AppleIdentityValidator(
    IOptions<AppleExternalAuthOptions> options,
    ILogger<AppleIdentityValidator> logger)
    : OidcExternalIdentityValidator(
        "https://appleid.apple.com/.well-known/openid-configuration", logger)
{
    public override ExternalAuthProvider Provider => ExternalAuthProvider.Apple;
    protected override IReadOnlyCollection<string> Audiences => options.Value.ClientIds;
    protected override IReadOnlyCollection<string> Issuers => ["https://appleid.apple.com"];

    protected override ExternalIdentityValidationResult Map(ClaimsPrincipal principal, string? nonce)
    {
        var subject = Claim(principal, JwtRegisteredClaimNames.Sub)
                      ?? Claim(principal, ClaimTypes.NameIdentifier);
        var tokenNonce = Claim(principal, "nonce");
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(nonce)
            || !string.Equals(tokenNonce, nonce, StringComparison.Ordinal))
            return ExternalIdentityValidationResult.Failed(ExternalValidationFailure.InvalidToken);
        return ExternalIdentityValidationResult.Success(new ExternalIdentity(
            Provider, subject, Claim(principal, JwtRegisteredClaimNames.Email),
            BooleanClaim(principal, "email_verified"), null, null, null));
    }
}
