using System.Text.Json;
using DogPlatform.Notification.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DogPlatform.Notification.API.WebSockets;

public sealed class WebSocketNotificationRealtimePublisher(
    INotificationWebSocketConnectionManager connectionManager,
    IOptions<JsonOptions> jsonOptions) : INotificationRealtimePublisher
{
    public Task PublishAsync(NotificationResponse notification, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var envelope = new NotificationWebSocketEnvelope("notificationReceived", notification);
        var payload = JsonSerializer.SerializeToUtf8Bytes(
            envelope, jsonOptions.Value.JsonSerializerOptions);
        return connectionManager.SendToUserAsync(userId, payload, cancellationToken);
    }

    private sealed record NotificationWebSocketEnvelope(string Event, NotificationResponse Data);
}
