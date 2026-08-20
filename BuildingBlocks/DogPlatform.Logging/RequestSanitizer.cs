using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace DogPlatform.Logging;

public sealed class RequestSanitizer : IRequestSanitizer
{
    private const string RedactedValue = "***";

    private static readonly Regex SensitiveAssignment = new(
        "(?<key>password|currentPassword|newPassword|confirmPassword|accessToken|refreshToken|authorization|apiKey|secret|verificationCode|code|token)\\s*[:=]\\s*(?<value>\\\"[^\\\"]*\\\"|'[^']*'|[^&,\\s}]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex BearerToken = new(
        "Bearer\\s+[^\\s,;]+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private static readonly HashSet<string> SensitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "currentPassword", "newPassword", "confirmPassword",
        "accessToken", "refreshToken", "authorization", "apiKey", "secret",
        "verificationCode", "code", "token", "cookie", "set-cookie"
    };

    public bool IsSensitiveName(string name) => SensitiveNames.Contains(name);

    public string SanitizeJson(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        try
        {
            var node = JsonNode.Parse(value);
            Redact(node);
            return node?.ToJsonString(new JsonSerializerOptions { WriteIndented = false }) ?? value;
        }
        catch (JsonException)
        {
            return SanitizeText(value);
        }
    }

    public string SanitizeQueryString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var parsed = QueryHelpers.ParseQuery(value);
        var sanitized = parsed.ToDictionary(
            pair => pair.Key,
            pair => IsSensitiveName(pair.Key) ? new[] { RedactedValue } : pair.Value.ToArray(),
            StringComparer.OrdinalIgnoreCase);

        return JsonSerializer.Serialize(sanitized);
    }

    public string SanitizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var sanitized = SensitiveAssignment.Replace(value, match => $"{match.Groups["key"].Value}=***");
        return BearerToken.Replace(sanitized, "Bearer ***");
    }

    private void Redact(JsonNode? node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var property in jsonObject.ToArray())
            {
                if (IsSensitiveName(property.Key))
                {
                    jsonObject[property.Key] = RedactedValue;
                }
                else
                {
                    Redact(property.Value);
                }
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                Redact(item);
            }
        }
    }
}
