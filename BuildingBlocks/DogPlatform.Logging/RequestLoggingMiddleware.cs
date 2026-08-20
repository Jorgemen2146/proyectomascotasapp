using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DogPlatform.Logging;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly IRequestSanitizer _sanitizer;
    private readonly HttpLoggingOptions _options;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger,
        IRequestSanitizer sanitizer,
        IOptions<HttpLoggingOptions> options)
    {
        _next = next;
        _logger = logger;
        _sanitizer = sanitizer;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestBody = _options.CaptureRequestBody
            ? await RequestBodyCapture.CaptureAsync(
                context.Request, _sanitizer, _options.MaxRequestBodyBytes, context.RequestAborted)
            : null;

        context.Items[RequestBodyCapture.HttpContextItemKey] = requestBody;
        var statusCode = StatusCodes.Status200OK;

        try
        {
            await _next(context);
            statusCode = context.Response.StatusCode;
        }
        catch
        {
            statusCode = StatusCodes.Status500InternalServerError;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "HTTP request {ServiceName} {HttpMethod} {Path} Query={QueryString} Body={RequestBody} UserId={UserId} StatusCode={StatusCode} DurationMs={DurationMs} TraceId={TraceId}",
                _options.ServiceName,
                context.Request.Method,
                context.Request.Path.Value,
                _sanitizer.SanitizeQueryString(context.Request.QueryString.Value ?? string.Empty),
                requestBody,
                context.User.FindFirstValue("sub"),
                statusCode,
                stopwatch.Elapsed.TotalMilliseconds,
                GetTraceId(context));
        }
    }

    internal static string GetTraceId(HttpContext context) =>
        Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
}
