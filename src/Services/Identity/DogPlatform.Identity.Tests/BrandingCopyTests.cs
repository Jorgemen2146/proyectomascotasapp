using System.Net;
using DogPlatform.Identity.Application.Features.Authentication.External;
using DogPlatform.Identity.Infrastructure.Messaging;
using Microsoft.Extensions.Options;

namespace DogPlatform.Identity.Tests;

public sealed class BrandingCopyTests
{
    [Fact]
    public async Task VerificationEmail_UsesPetLifeVisibleBranding()
    {
        var handler = new CapturingHandler();
        var sender = CreateSender(handler);

        await sender.SendVerificationCodeAsync(
            "user@example.com", "123456", CancellationToken.None);

        var payload = Assert.Single(handler.Payloads);
        Assert.Contains("PetLife", payload, StringComparison.Ordinal);
        Assert.DoesNotContain("DogPlatform", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PasswordResetEmail_UsesPetLifeVisibleBranding()
    {
        var handler = new CapturingHandler();
        var sender = CreateSender(handler);

        await sender.SendPasswordResetCodeAsync(
            "user@example.com", "123456", 10, CancellationToken.None);

        var payload = Assert.Single(handler.Payloads);
        Assert.Contains("PetLife", payload, StringComparison.Ordinal);
        Assert.DoesNotContain("DogPlatform", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExternalAuth_KeepsErrorCodeAndUsesPetLifeInVisibleDescription()
    {
        Assert.Equal("EXTERNAL_ACCOUNT_LINK_REQUIRED", ExternalAuthErrors.AccountLinkRequired.Code);
        Assert.Contains("PetLife", ExternalAuthErrors.AccountLinkRequired.Description);
        Assert.DoesNotContain(
            "DogPlatform", ExternalAuthErrors.AccountLinkRequired.Description,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LegalDocumentSeeds_UsePetLifeInVisibleContent()
    {
        var root = FindRepositoryRoot();
        AssertLegalContent(
            Path.Combine(root, "scripts", "database", "Identity", "AddLegalConsents.sql"),
            "IF NOT EXISTS (SELECT 1 FROM auth.LegalDocuments WHERE Type=N'TermsAndConditions'");
        AssertLegalContent(
            Path.Combine(root, "scripts", "database", "Identity", "UpdateLegalDocumentsV1.sql"),
            "DECLARE @TermsContent");
    }

    private static ResendEmailSender CreateSender(CapturingHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.resend.test/") };
        var options = Options.Create(new EmailOptions
        {
            Provider = "Resend",
            FromEmail = "noreply@example.com",
            FromName = "PetLife",
            Resend = new ResendOptions { ApiKey = "test-api-key" }
        });
        return new ResendEmailSender(client, options);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "DogPlatform.slnx")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    private static void AssertLegalContent(string path, string marker)
    {
        var script = File.ReadAllText(path);
        var markerIndex = script.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, $"Legal content marker not found in {path}.");
        var visibleContent = script[markerIndex..];
        Assert.Contains("PetLife", visibleContent, StringComparison.Ordinal);
        Assert.DoesNotContain("DogPlatform", visibleContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Dog Platform", visibleContent, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public List<string> Payloads { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Payloads.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
