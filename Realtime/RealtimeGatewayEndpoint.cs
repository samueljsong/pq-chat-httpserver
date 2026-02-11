using pq_chat_httpserver.Realtime.Services;
using System.Net.WebSockets;

namespace pq_chat_httpserver.Realtime;

public static class RealtimeGatewayEndpoint
{
    public static void Map(WebApplication app)
    {
        app.Map("/ws", HandleAsync);
    }

    private static async Task HandleAsync(HttpContext context, GatewayService gateway, TokenValidator validator)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Expected WebSocket request.");
            return;
        }

        var token = context.Request.Query["token"].ToString();
        if (string.IsNullOrWhiteSpace(token))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Missing token.");
            return;
        }

        string userId;
        try
        {
            userId = validator.ValidateAndGetUserId(token);
        }
        catch
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid token.");
            return;
        }

        using var ws = await context.WebSockets.AcceptWebSocketAsync();
        await gateway.BridgeAsync(ws, userId, context.RequestAborted);

        try
        {
            if (ws.State is WebSocketState.Open or WebSocketState.CloseReceived)
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
        catch { }
    }
}
