using System.Net.WebSockets;

namespace pq_chat_httpserver.Realtime.Transport;

public static class WebSocketReader
{
    public static async Task<byte[]?> ReadWholeMessage(WebSocket ws, CancellationToken ct)
    {
        var buffer = new byte[8192];
        var result = await ws.ReceiveAsync(buffer, ct);

        if (result.MessageType == WebSocketMessageType.Close)
            return null;

        using var ms = new MemoryStream();
        ms.Write(buffer, 0, result.Count);

        while (!result.EndOfMessage)
        {
            result = await ws.ReceiveAsync(buffer, ct);
            ms.Write(buffer, 0, result.Count);
        }

        return ms.ToArray();
    }
}
