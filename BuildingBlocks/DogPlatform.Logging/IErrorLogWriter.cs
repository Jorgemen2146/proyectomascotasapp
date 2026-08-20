namespace DogPlatform.Logging;

public interface IErrorLogWriter
{
    Task<long> WriteAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default);
}
