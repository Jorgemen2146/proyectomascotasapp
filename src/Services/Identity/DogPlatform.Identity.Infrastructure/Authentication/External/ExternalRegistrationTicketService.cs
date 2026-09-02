using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DogPlatform.Identity.Application.Features.Authentication.External;
using DogPlatform.Identity.Domain.Aggregates.ExternalLogin;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DogPlatform.Identity.Infrastructure.Authentication.External;

internal sealed class ExternalRegistrationTicketService(
    IOptions<ExternalRegistrationOptions> options) : IExternalRegistrationTicketService
{
    private const string Issuer = "DogPlatform.Identity.ExternalRegistration";

    public string Issue(ExternalIdentity identity, DateTime utcNow)
    {
        var settings = options.Value;
        EnsureConfigured(settings);
        var claims = new List<Claim>
        {
            new("provider", identity.Provider.ToString()),
            new(JwtRegisteredClaimNames.Sub, identity.ProviderUserId),
            new("email_verified", identity.EmailVerified.ToString().ToLowerInvariant()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        Add(claims, JwtRegisteredClaimNames.Email, identity.Email);
        Add(claims, "given_name", identity.FirstName);
        Add(claims, "family_name", identity.LastName);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.TicketSecret)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(Issuer, Issuer, claims, utcNow,
            utcNow.AddMinutes(settings.TicketLifetimeMinutes), credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ExternalIdentityValidationResult Validate(string ticket, DateTime utcNow)
    {
        try
        {
            var settings = options.Value;
            EnsureConfigured(settings);
            var principal = new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(
                ticket,
                new TokenValidationParameters
                {
                    ValidIssuer = Issuer,
                    ValidAudience = Issuer,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.Zero,
                    LifetimeValidator = (notBefore, expires, _, _) =>
                        (!notBefore.HasValue || notBefore.Value <= utcNow)
                        && expires.HasValue && expires.Value > utcNow,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(settings.TicketSecret))
                }, out _);
            if (!Enum.TryParse<ExternalAuthProvider>(principal.FindFirst("provider")?.Value,
                    true, out var provider))
                return ExternalIdentityValidationResult.Failed(ExternalValidationFailure.InvalidToken);
            var subject = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrWhiteSpace(subject))
                return ExternalIdentityValidationResult.Failed(ExternalValidationFailure.InvalidToken);
            return ExternalIdentityValidationResult.Success(new ExternalIdentity(
                provider, subject, principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value,
                bool.TryParse(principal.FindFirst("email_verified")?.Value, out var verified) && verified,
                principal.FindFirst("given_name")?.Value, principal.FindFirst("family_name")?.Value,
                null));
        }
        catch
        {
            return ExternalIdentityValidationResult.Failed(ExternalValidationFailure.InvalidToken);
        }
    }

    private static void Add(ICollection<Claim> claims, string type, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) claims.Add(new Claim(type, value));
    }

    private static void EnsureConfigured(ExternalRegistrationOptions settings)
    {
        if (Encoding.UTF8.GetByteCount(settings.TicketSecret) < 32)
            throw new InvalidOperationException(
                "ExternalAuth:Registration:TicketSecret must contain at least 32 bytes.");
    }
}
