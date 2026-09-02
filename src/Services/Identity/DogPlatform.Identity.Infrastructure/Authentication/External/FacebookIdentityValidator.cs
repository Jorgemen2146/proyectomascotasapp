using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DogPlatform.Identity.Application.Features.Authentication.External;
using DogPlatform.Identity.Domain.Aggregates.ExternalLogin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DogPlatform.Identity.Infrastructure.Authentication.External;

internal sealed class FacebookIdentityValidator(
    HttpClient httpClient,
    IOptions<FacebookExternalAuthOptions> options,
    ILogger<FacebookIdentityValidator> logger) : IProviderIdentityValidator
{
    public ExternalAuthProvider Provider => ExternalAuthProvider.Facebook;

    public async Task<ExternalIdentityValidationResult> ValidateAsync(
        string credential, string? nonce, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.AppId) || string.IsNullOrWhiteSpace(settings.AppSecret))
            return ExternalIdentityValidationResult.Failed(ExternalValidationFailure.ProviderNotConfigured);
        var errorId = Guid.NewGuid();
        try
        {
            using var debugRequest = new HttpRequestMessage(HttpMethod.Post,
                $"{settings.GraphApiVersion}/debug_token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["input_token"] = credential,
                    ["access_token"] = $"{settings.AppId}|{settings.AppSecret}"
                })
            };
            using var debugResponse = await httpClient.SendAsync(debugRequest, cancellationToken);
            if (!debugResponse.IsSuccessStatusCode)
                return Rejected(errorId);
            var debug = await debugResponse.Content.ReadFromJsonAsync<FacebookDebugResponse>(
                cancellationToken: cancellationToken);
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (debug?.Data is not { IsValid: true } data
                || !string.Equals(data.AppId, settings.AppId, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(data.UserId)
                || data.ExpiresAt <= now)
                return Rejected(errorId);

            using var profileRequest = new HttpRequestMessage(HttpMethod.Get,
                $"{settings.GraphApiVersion}/me?fields=id,email,first_name,last_name,picture");
            profileRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential);
            using var profileResponse = await httpClient.SendAsync(profileRequest, cancellationToken);
            if (!profileResponse.IsSuccessStatusCode) return Rejected(errorId);
            var profile = await profileResponse.Content.ReadFromJsonAsync<FacebookProfile>(
                cancellationToken: cancellationToken);
            if (profile is null || !string.Equals(profile.Id, data.UserId, StringComparison.Ordinal))
                return Rejected(errorId);

            logger.LogInformation("External authentication validation completed. Provider=Facebook Success=true ErrorId={ErrorId}", errorId);
            return ExternalIdentityValidationResult.Success(new ExternalIdentity(
                Provider, profile.Id, profile.Email, false, profile.FirstName,
                profile.LastName, profile.Picture?.Data?.Url));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "External provider unavailable. Provider=Facebook ErrorId={ErrorId}", errorId);
            return ExternalIdentityValidationResult.Failed(ExternalValidationFailure.ProviderUnavailable);
        }
    }

    private ExternalIdentityValidationResult Rejected(Guid errorId)
    {
        logger.LogWarning("External authentication token rejected. Provider=Facebook ErrorId={ErrorId}", errorId);
        return ExternalIdentityValidationResult.Failed(ExternalValidationFailure.InvalidToken);
    }

    private sealed record FacebookDebugResponse(FacebookDebugData Data);
    private sealed record FacebookDebugData(
        [property: JsonPropertyName("is_valid")] bool IsValid,
        [property: JsonPropertyName("app_id")] string AppId,
        [property: JsonPropertyName("user_id")] string UserId,
        [property: JsonPropertyName("expires_at")] long ExpiresAt);
    private sealed record FacebookProfile(
        string Id,
        string? Email,
        [property: JsonPropertyName("first_name")] string? FirstName,
        [property: JsonPropertyName("last_name")] string? LastName,
        FacebookPicture? Picture);
    private sealed record FacebookPicture(FacebookPictureData? Data);
    private sealed record FacebookPictureData(string? Url);
}
