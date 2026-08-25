using System.Text;
using System.Text.Json;
using DogPlatform.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace DogPlatform.Logging.Tests;

public sealed class HttpLoggingTests
{
    private static readonly IRequestSanitizer Sanitizer = new RequestSanitizer();

    [Fact]
    public async Task Request_body_remains_available_to_downstream_handler()
    {
        var context = JsonContext("{\"name\":\"Firulais\"}");
        string? downstreamBody = null;
        var middleware = CreateRequestMiddleware(async httpContext =>
        {
            using var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true);
            downstreamBody = await reader.ReadToEndAsync();
        });

        await middleware.InvokeAsync(context);

        Assert.Equal("{\"name\":\"Firulais\"}", downstreamBody);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("accessToken")]
    [InlineData("refreshToken")]
    [InlineData("verificationCode")]
    public void Sensitive_json_values_are_redacted_recursively(string propertyName)
    {
        var input = $"{{\"nested\":{{\"{propertyName}\":\"real-secret\"}}}}";

        var result = Sanitizer.SanitizeJson(input);

        Assert.DoesNotContain("real-secret", result);
        Assert.Contains("***", result);
    }

    [Theory]
    [InlineData("imageBase64")]
    [InlineData("base64")]
    [InlineData("imageData")]
    [InlineData("fileContent")]
    public void Base64_image_fields_are_removed_from_json_logs(string propertyName)
    {
        var result = Sanitizer.SanitizeJson($"{{\"{propertyName}\":\"large-image-payload\"}}");

        Assert.DoesNotContain("large-image-payload", result);
        Assert.Contains("[BASE64_IMAGE_REMOVED]", result);
    }

    [Fact]
    public async Task Authorization_header_is_not_logged()
    {
        var logger = new CapturingLogger<RequestLoggingMiddleware>();
        var context = JsonContext("{}");
        context.Request.Headers.Authorization = "Bearer do-not-log-this";
        var middleware = CreateRequestMiddleware(_ => Task.CompletedTask, logger);

        await middleware.InvokeAsync(context);

        Assert.DoesNotContain("do-not-log-this", string.Join(Environment.NewLine, logger.Messages));
    }

    [Fact]
    public void WebSocket_access_token_query_value_is_redacted()
    {
        var result = Sanitizer.SanitizeQueryString("?access_token=do-not-log-this&client=mobile");

        Assert.DoesNotContain("do-not-log-this", result);
        Assert.Contains("***", result);
        Assert.Contains("mobile", result);
    }

    [Fact]
    public async Task Unhandled_exception_creates_error_log_and_returns_generated_error_id()
    {
        var writer = new CapturingWriter(153);
        var context = JsonContext("{\"password\":\"real-secret\"}");
        context.TraceIdentifier = "trace-123";
        var pipeline = CreateExceptionPipeline(writer, _ => throw new InvalidOperationException("database exploded"));

        await pipeline(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Single(writer.Entries);
        Assert.Equal("trace-123", writer.Entries[0].TraceId);
        Assert.DoesNotContain("real-secret", writer.Entries[0].RequestBody);
        var response = await ReadResponseAsync(context);
        Assert.Equal(153, response.GetProperty("errorId").GetInt64());
        Assert.Equal("INTERNAL_SERVER_ERROR", response.GetProperty("error").GetString());
        Assert.DoesNotContain("database exploded", response.ToString());
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    public async Task Expected_http_status_does_not_create_error_log(int statusCode)
    {
        var writer = new CapturingWriter(1);
        var context = CreateContext();
        var pipeline = CreateExceptionPipeline(writer, httpContext =>
        {
            httpContext.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        });

        await pipeline(context);

        Assert.Empty(writer.Entries);
        Assert.Equal(statusCode, context.Response.StatusCode);
    }

    [Fact]
    public async Task Sql_writer_failure_does_not_replace_generic_500_response()
    {
        var writer = new ThrowingWriter();
        var context = CreateContext();
        var pipeline = CreateExceptionPipeline(writer, _ => throw new InvalidOperationException("original failure"));

        await pipeline(context);

        var response = await ReadResponseAsync(context);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal(JsonValueKind.Null, response.GetProperty("errorId").ValueKind);
        Assert.DoesNotContain("original failure", response.ToString());
    }

    [Fact]
    public async Task Multipart_capture_contains_metadata_but_not_file_content()
    {
        var context = CreateContext();
        context.Request.ContentType = "multipart/form-data; boundary=test";
        var bytes = Encoding.UTF8.GetBytes("binary-secret-content");
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "photo", "dog.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };
        context.Request.Form = new FormCollection(
            new Dictionary<string, StringValues> { ["verificationCode"] = "987654" },
            new FormFileCollection { file });

        var result = await RequestBodyCapture.CaptureAsync(context.Request, Sanitizer, 32 * 1024);

        Assert.Contains("dog.jpg", result);
        Assert.Contains("image/jpeg", result);
        Assert.Contains(bytes.Length.ToString(), result);
        Assert.DoesNotContain("binary-secret-content", result);
        Assert.DoesNotContain("987654", result);
        Assert.Contains("***", result);
    }

    [Fact]
    public async Task Successful_request_does_not_create_error_log()
    {
        var writer = new CapturingWriter(1);
        var context = CreateContext();
        var pipeline = CreateExceptionPipeline(writer, httpContext =>
        {
            httpContext.Response.StatusCode = StatusCodes.Status201Created;
            return Task.CompletedTask;
        });

        await pipeline(context);

        Assert.Empty(writer.Entries);
    }

    private static RequestLoggingMiddleware CreateRequestMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware>? logger = null) =>
        new(next, logger ?? new CapturingLogger<RequestLoggingMiddleware>(), Sanitizer, Options());

    private static RequestDelegate CreateExceptionPipeline(IErrorLogWriter writer, RequestDelegate terminal)
    {
        var requestLogging = CreateRequestMiddleware(terminal);
        var exceptionHandling = new ExceptionHandlingMiddleware(
            requestLogging.InvokeAsync,
            new CapturingLogger<ExceptionHandlingMiddleware>(),
            writer,
            Sanitizer,
            Options());
        return exceptionHandling.InvokeAsync;
    }

    private static IOptions<HttpLoggingOptions> Options() =>
        Microsoft.Extensions.Options.Options.Create(new HttpLoggingOptions
        {
            CaptureRequestBody = true,
            MaxRequestBodyBytes = 32 * 1024,
            ServiceName = "DogPlatform.Tests.API"
        });

    private static DefaultHttpContext JsonContext(string body)
    {
        var context = CreateContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentLength = context.Request.Body.Length;
        return context;
    }

    private static DefaultHttpContext CreateContext() => new()
    {
        Response = { Body = new MemoryStream() }
    };

    private static async Task<JsonElement> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
    }

    private sealed class CapturingWriter(long generatedId) : IErrorLogWriter
    {
        public List<ErrorLogEntry> Entries { get; } = [];

        public Task<long> WriteAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            return Task.FromResult(generatedId);
        }
    }

    private sealed class ThrowingWriter : IErrorLogWriter
    {
        public Task<long> WriteAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("SQL unavailable");
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }
}
