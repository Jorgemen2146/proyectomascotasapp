using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DogPlatform.Logging;

public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IErrorLogWriter _writer;
    private readonly IRequestSanitizer _sanitizer;
    private readonly HttpLoggingOptions _options;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IErrorLogWriter writer,
        IRequestSanitizer sanitizer,
        IOptions<HttpLoggingOptions> options)
    {
        _next = next;
        _logger = logger;
        _writer = writer;
        _sanitizer = sanitizer;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            var errorId = await TryPersistAsync(context, exception);
            _logger.LogError(exception,
                "Unhandled exception in {ServiceName}. ErrorId={ErrorId} TraceId={TraceId}",
                _options.ServiceName, errorId, RequestLoggingMiddleware.GetTraceId(context));

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await JsonSerializer.SerializeAsync(context.Response.Body, new
            {
                error = "INTERNAL_SERVER_ERROR",
                message = "Ocurrió un error inesperado.",
                errorId
            }, JsonOptions);
        }
    }

    private async Task<long?> TryPersistAsync(HttpContext context, Exception exception)
    {
        try
        {
            var capturedBody = context.Items[RequestBodyCapture.HttpContextItemKey] as string;
            if (capturedBody is null && _options.CaptureRequestBody)
            {
                capturedBody = await RequestBodyCapture.CaptureAsync(
                    context.Request, _sanitizer, _options.MaxRequestBodyBytes, CancellationToken.None);
            }

            return await _writer.WriteAsync(new ErrorLogEntry(
                DateTime.UtcNow,
                _options.ServiceName,
                context.Request.Method,
                context.Request.Path.Value,
                _sanitizer.SanitizeQueryString(context.Request.QueryString.Value ?? string.Empty),
                capturedBody,
                StatusCodes.Status500InternalServerError,
                exception.GetType().FullName,
                _sanitizer.SanitizeText(exception.Message),
                exception.StackTrace is null ? null : _sanitizer.SanitizeText(exception.StackTrace),
                context.User.FindFirstValue("sub"),
                RequestLoggingMiddleware.GetTraceId(context)), CancellationToken.None);
        }
        catch (Exception loggingException)
        {
            _logger.LogError(loggingException,
                "Could not persist the unhandled exception. The original exception was {OriginalExceptionType}: {OriginalExceptionMessage}",
                exception.GetType().FullName, exception.Message);
            return null;
        }
    }
}
