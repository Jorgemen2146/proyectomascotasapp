using System.Net.WebSockets;

namespace DogPlatform.Notification.API.WebSockets;

public interface INotificationWebSocketConnectionManager
{
    Task AddAsync(Guid userId, WebSocket socket, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid userId, WebSocket socket, CancellationToken cancellationToken = default);
    Task SendToUserAsync(Guid userId, ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default);
    int GetConnectionCount(Guid userId);
}
