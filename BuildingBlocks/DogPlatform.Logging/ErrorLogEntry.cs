namespace DogPlatform.Logging;

public sealed record ErrorLogEntry(
    DateTime OccurredAtUtc,
    string ServiceName,
    string? HttpMethod,
    string? Path,
    string? QueryString,
    string? RequestBody,
    int? StatusCode,
    string? ExceptionType,
    string? Message,
    string? StackTrace,
    string? UserId,
    string? TraceId);
