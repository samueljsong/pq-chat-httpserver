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
        Console.WriteLine("WS CONNECTED");

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Expected WebSocket request.");
            return;
        }

        string userId = "test-user"; // 👈 temporary test

        using var ws = await context.WebSockets.AcceptWebSocketAsync();

        Console.WriteLine("WebSocket accepted — staying open");

        // keep socket alive forever (test)
        while (ws.State == WebSocketState.Open)
        {
            await Task.Delay(1000);
        }

    }

}
