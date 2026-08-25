using System.Net.WebSockets;

namespace DogPlatform.Notification.API.WebSockets;

public sealed class NotificationWebSocketConnectionManager(
    ILogger<NotificationWebSocketConnectionManager> logger)
    : INotificationWebSocketConnectionManager
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, List<ManagedConnection>> _connections = [];

    public Task AddAsync(Guid userId, WebSocket socket,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(socket);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_connections.TryGetValue(userId, out var userConnections))
            {
                userConnections = [];
                _connections[userId] = userConnections;
            }

            if (userConnections.All(connection => !ReferenceEquals(connection.Socket, socket)))
                userConnections.Add(new ManagedConnection(socket));
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid userId, WebSocket socket,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_connections.TryGetValue(userId, out var userConnections))
                return Task.CompletedTask;

            var connection = userConnections.FirstOrDefault(item => ReferenceEquals(item.Socket, socket));
            if (connection is not null)
            {
                connection.Deactivate();
                userConnections.Remove(connection);
            }

            if (userConnections.Count == 0)
                _connections.Remove(userId);
        }

        return Task.CompletedTask;
    }

    public async Task SendToUserAsync(Guid userId, ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default)
    {
        ManagedConnection[] snapshot;
        lock (_gate)
        {
            snapshot = _connections.TryGetValue(userId, out var userConnections)
                ? [.. userConnections]
                : [];
        }

        await Task.WhenAll(snapshot.Select(connection =>
            SendAsync(userId, connection, payload, cancellationToken)));
    }

    public int GetConnectionCount(Guid userId)
    {
        lock (_gate)
            return _connections.TryGetValue(userId, out var userConnections)
                ? userConnections.Count
                : 0;
    }

    private async Task SendAsync(Guid userId, ManagedConnection connection,
        ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        if (!connection.IsActive || connection.Socket.State != WebSocketState.Open)
        {
            await RemoveBrokenConnectionAsync(userId, connection.Socket);
            return;
        }

        await connection.SendLock.WaitAsync(cancellationToken);
        try
        {
            if (!connection.IsActive || connection.Socket.State != WebSocketState.Open)
            {
                await RemoveBrokenConnectionAsync(userId, connection.Socket);
                return;
            }

            await connection.Socket.SendAsync(payload, WebSocketMessageType.Text,
                endOfMessage: true, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception,
                "WebSocket delivery failed for user {UserId}; removing the connection.", userId);
            await RemoveBrokenConnectionAsync(userId, connection.Socket);
        }
        finally
        {
            connection.SendLock.Release();
        }
    }

    private async Task RemoveBrokenConnectionAsync(Guid userId, WebSocket socket)
    {
        await RemoveAsync(userId, socket);
        try
        {
            socket.Abort();
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "WebSocket abort failed during cleanup for user {UserId}.", userId);
        }
    }

    private sealed class ManagedConnection(WebSocket socket)
    {
        private int _active = 1;
        public WebSocket Socket { get; } = socket;
        public SemaphoreSlim SendLock { get; } = new(1, 1);
        public bool IsActive => Volatile.Read(ref _active) == 1;
        public void Deactivate() => Interlocked.Exchange(ref _active, 0);
    }
}
