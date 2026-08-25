using System.Net.WebSockets;
using System.Security.Claims;

namespace DogPlatform.Notification.API.WebSockets;

public static class NotificationWebSocketEndpoint
{
    public static async Task HandleAsync(
        HttpContext context,
        INotificationWebSocketConnectionManager connectionManager)
    {
        var claim = context.User.FindFirst("sub") ??
                    context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (context.User.Identity?.IsAuthenticated != true ||
            !Guid.TryParse(claim?.Value, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        await connectionManager.AddAsync(userId, socket, context.RequestAborted);

        try
        {
            var buffer = new byte[4 * 1024];
            while (!context.RequestAborted.IsCancellationRequested &&
                   socket.State is WebSocketState.Open or WebSocketState.CloseSent)
            {
                var result = await socket.ReceiveAsync(buffer, context.RequestAborted);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;
            }
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client disconnected or the host is shutting down.
        }
        catch (WebSocketException)
        {
            // Connection lifecycle errors are handled by removal in finally.
        }
        finally
        {
            await connectionManager.RemoveAsync(userId, socket, CancellationToken.None);
            await CloseGracefullyAsync(socket);
        }
    }

    private static async Task CloseGracefullyAsync(WebSocket socket)
    {
        try
        {
            if (socket.State == WebSocketState.CloseReceived)
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure,
                    "Client closed the connection.", CancellationToken.None);
            else if (socket.State == WebSocketState.Open)
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                    "Connection closed.", CancellationToken.None);
        }
        catch (WebSocketException)
        {
            socket.Abort();
        }
    }
}
