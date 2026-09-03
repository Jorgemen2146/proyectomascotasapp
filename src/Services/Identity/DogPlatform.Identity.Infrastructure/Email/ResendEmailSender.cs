using System.Net.Http.Headers;
using System.Net.Http.Json;
using DogPlatform.Identity.Application.Communication;
using Microsoft.Extensions.Options;

namespace DogPlatform.Identity.Infrastructure.Messaging;

internal sealed class ResendEmailSender : IEmailSender
{
    private const string Subject = "Verifica tu cuenta de PetLife";
    private const string PasswordResetSubject = "Tu código para restablecer tu contraseña";

    private readonly HttpClient _httpClient;
    private readonly EmailOptions _options;

    public ResendEmailSender(HttpClient httpClient, IOptions<EmailOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task SendVerificationCodeAsync(
        string email,
        string code,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _options.Resend.ApiKey);
        request.Content = JsonContent.Create(new
        {
            from = $"{_options.FromName} <{_options.FromEmail}>",
            to = new[] { email },
            subject = Subject,
            html = BuildHtml(code)
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task SendPasswordResetCodeAsync(
        string email,
        string code,
        int expirationMinutes,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", _options.Resend.ApiKey);
        request.Content = JsonContent.Create(new
        {
            from = $"{_options.FromName} <{_options.FromEmail}>",
            to = new[] { email },
            subject = PasswordResetSubject,
            html = BuildPasswordResetHtml(code, expirationMinutes)
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private void EnsureConfigured()
    {
        if (!string.Equals(_options.Provider, "Resend", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Email:Provider must be configured as Resend.");

        if (string.IsNullOrWhiteSpace(_options.FromEmail))
            throw new InvalidOperationException("Email:FromEmail is required.");

        if (string.IsNullOrWhiteSpace(_options.Resend.ApiKey))
            throw new InvalidOperationException("Email:Resend:ApiKey is required.");
    }

    private static string BuildHtml(string code) => $$"""
        <!doctype html>
        <html lang="es">
          <body style="font-family:Arial,sans-serif;color:#1f2937;line-height:1.5">
            <p>Hola,</p>
            <p>Se solicitó crear una cuenta en PetLife.</p>
            <p>Tu código de verificación es:</p>
            <p style="font-size:32px;font-weight:700;letter-spacing:8px;margin:24px 0">{{code}}</p>
            <p>El código expira en 10 minutos.</p>
            <p>Si no solicitaste esta cuenta, puedes ignorar este correo.</p>
          </body>
        </html>
        """;

    private static string BuildPasswordResetHtml(string code, int expirationMinutes) => $$"""
        <!doctype html>
        <html lang="es">
          <body style="font-family:Arial,sans-serif;color:#1f2937;line-height:1.5">
            <p>Hola,</p>
            <p>Tu código de recuperación de PetLife es:</p>
            <p style="font-size:32px;font-weight:700;letter-spacing:8px;margin:24px 0">{{code}}</p>
            <p>Este código vence en {{expirationMinutes}} minutos.</p>
            <p>Si no solicitaste este cambio, puedes ignorar este correo.</p>
          </body>
        </html>
        """;
}
