using System.Data;
using Microsoft.Data.SqlClient;

namespace DogPlatform.Logging;

public sealed class SqlErrorLogWriter(string connectionString) : IErrorLogWriter
{
    private const string InsertSql = """
        INSERT INTO [auth].[ErrorLogs]
        (
            [OccurredAtUtc], [ServiceName], [HttpMethod], [Path], [QueryString],
            [RequestBody], [StatusCode], [ExceptionType], [Message], [StackTrace],
            [UserId], [TraceId]
        )
        VALUES
        (
            @OccurredAtUtc, @ServiceName, @HttpMethod, @Path, @QueryString,
            @RequestBody, @StatusCode, @ExceptionType, @Message, @StackTrace,
            @UserId, @TraceId
        );
        SELECT CONVERT(bigint, SCOPE_IDENTITY());
        """;

    public async Task<long> WriteAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(InsertSql, connection) { CommandTimeout = 5 };

        Add(command, "@OccurredAtUtc", SqlDbType.DateTime2, entry.OccurredAtUtc);
        Add(command, "@ServiceName", SqlDbType.NVarChar, Limit(entry.ServiceName, 100), 100);
        Add(command, "@HttpMethod", SqlDbType.NVarChar, Limit(entry.HttpMethod, 10), 10);
        Add(command, "@Path", SqlDbType.NVarChar, Limit(entry.Path, 500), 500);
        Add(command, "@QueryString", SqlDbType.NVarChar, entry.QueryString, -1);
        Add(command, "@RequestBody", SqlDbType.NVarChar, entry.RequestBody, -1);
        Add(command, "@StatusCode", SqlDbType.Int, entry.StatusCode);
        Add(command, "@ExceptionType", SqlDbType.NVarChar, Limit(entry.ExceptionType, 500), 500);
        Add(command, "@Message", SqlDbType.NVarChar, entry.Message, -1);
        Add(command, "@StackTrace", SqlDbType.NVarChar, entry.StackTrace, -1);
        Add(command, "@UserId", SqlDbType.NVarChar, Limit(entry.UserId, 100), 100);
        Add(command, "@TraceId", SqlDbType.NVarChar, Limit(entry.TraceId, 100), 100);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    private static void Add(SqlCommand command, string name, SqlDbType type, object? value, int? size = null)
    {
        var parameter = size.HasValue
            ? command.Parameters.Add(name, type, size.Value)
            : command.Parameters.Add(name, type);
        parameter.Value = value ?? DBNull.Value;
    }

    private static string? Limit(string? value, int maximumLength) =>
        value?.Length > maximumLength ? value[..maximumLength] : value;
}
