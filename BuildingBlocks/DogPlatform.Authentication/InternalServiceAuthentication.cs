using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DogPlatform.Authentication;

public static class InternalServiceDefaults
{
    public const string AuthenticationScheme = "InternalService";
    public const string HeaderName = "X-DogPlatform-Internal-Key";
}

public sealed class InternalServiceAuthenticationOptions : AuthenticationSchemeOptions
{
    public string ApiKey { get; set; } = string.Empty;
}

public sealed class InternalServiceAuthenticationHandler(
    IOptionsMonitor<InternalServiceAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<InternalServiceAuthenticationOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrWhiteSpace(Options.ApiKey))
            return Task.FromResult(AuthenticateResult.Fail("Internal service authentication is not configured."));

        if (!Request.Headers.TryGetValue(InternalServiceDefaults.HeaderName, out var values))
            return Task.FromResult(AuthenticateResult.NoResult());

        var supplied = values.ToString();
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        var expectedBytes = Encoding.UTF8.GetBytes(Options.ApiKey);
        if (!CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedBytes))
            return Task.FromResult(AuthenticateResult.Fail("Invalid internal service credential."));

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "dogplatform-internal-service")],
            InternalServiceDefaults.AuthenticationScheme);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity),
                InternalServiceDefaults.AuthenticationScheme)));
    }
}

public static class InternalServiceAuthenticationExtensions
{
    public static AuthenticationBuilder AddInternalService(
        this AuthenticationBuilder builder,
        Action<InternalServiceAuthenticationOptions> configureOptions) =>
        builder.AddScheme<InternalServiceAuthenticationOptions, InternalServiceAuthenticationHandler>(
            InternalServiceDefaults.AuthenticationScheme, configureOptions);
}
