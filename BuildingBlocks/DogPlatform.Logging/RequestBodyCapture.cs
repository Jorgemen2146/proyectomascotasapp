using System.Buffers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace DogPlatform.Logging;

public static class RequestBodyCapture
{
    public const string TruncatedMarker = "[TRUNCATED]";
    public static readonly object HttpContextItemKey = new();

    public static async Task<string?> CaptureAsync(
        HttpRequest request,
        IRequestSanitizer sanitizer,
        int maxBytes,
        CancellationToken cancellationToken = default)
    {
        if (request.ContentLength == 0 || maxBytes <= 0)
        {
            return null;
        }

        if (request.HasFormContentType &&
            request.ContentType?.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) == true)
        {
            return await CaptureMultipartAsync(request, sanitizer, maxBytes, cancellationToken);
        }

        if (!IsTextContentType(request.ContentType))
        {
            return request.ContentLength is null ? null : $"[CONTENT NOT CAPTURED: {request.ContentType ?? "unknown"}]";
        }

        if (request.ContentLength > maxBytes)
        {
            return TruncatedMarker;
        }

        request.EnableBuffering();
        request.Body.Position = 0;
        var rented = ArrayPool<byte>.Shared.Rent(maxBytes + 1);

        try
        {
            var totalRead = 0;
            while (totalRead <= maxBytes)
            {
                var read = await request.Body.ReadAsync(
                    rented.AsMemory(totalRead, maxBytes + 1 - totalRead), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                totalRead += read;
            }

            if (totalRead > maxBytes)
            {
                return TruncatedMarker;
            }

            var body = Encoding.UTF8.GetString(rented, 0, totalRead);
            if (IsJsonContentType(request.ContentType))
            {
                return sanitizer.SanitizeJson(body);
            }

            return request.ContentType?.StartsWith(
                "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) == true
                ? sanitizer.SanitizeQueryString(body)
                : sanitizer.SanitizeText(body);
        }
        finally
        {
            request.Body.Position = 0;
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static async Task<string> CaptureMultipartAsync(
        HttpRequest request,
        IRequestSanitizer sanitizer,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        try
        {
            var form = await request.ReadFormAsync(cancellationToken);
            var fields = form.ToDictionary(
                pair => pair.Key,
                pair => sanitizer.IsSensitiveName(pair.Key)
                    ? "***"
                    : sanitizer.SanitizeJson(pair.Value.ToString()));
            var files = form.Files.Select(file => new
            {
                fileName = Path.GetFileName(file.FileName),
                contentType = file.ContentType,
                fileSize = file.Length
            });
            var metadata = JsonSerializer.Serialize(new { fields, files });
            return Encoding.UTF8.GetByteCount(metadata) > maxBytes ? TruncatedMarker : metadata;
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException)
        {
            return "[MULTIPART METADATA UNAVAILABLE]";
        }
    }

    private static bool IsJsonContentType(string? contentType) =>
        contentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsTextContentType(string? contentType) =>
        contentType is null ||
        IsJsonContentType(contentType) ||
        contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
        contentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
}
