using Microsoft.Extensions.Options;
using pq_chat_httpserver.Realtime.Options;
using pq_chat_httpserver.Realtime.Transport;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.Json;

namespace pq_chat_httpserver.Realtime.Services;

public sealed class GatewayService
{
    private readonly TcpOptions _tcp;

    public GatewayService(IOptions<TcpOptions> tcpOptions)
    {
        _tcp = tcpOptions.Value;
    }

    public async Task BridgeAsync(WebSocket ws, string userId, CancellationToken ct)
    {
        using var tcp = new TcpClient();
        await tcp.ConnectAsync(_tcp.Host, _tcp.Port, ct);
        using var tcpStream = tcp.GetStream();

        // First message to TCP: AUTH
        var auth = JsonSerializer.SerializeToUtf8Bytes(new { type = "AUTH", userId });
        await TcpFramer.WriteFrame(tcpStream, auth, ct);

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var lct = linked.Token;

        var wsToTcp = PumpWebSocketToTcp(ws, tcpStream, lct);
        var tcpToWs = PumpTcpToWebSocket(tcpStream, ws, lct);

        await Task.WhenAny(wsToTcp, tcpToWs);
        linked.Cancel();
    }

    private static async Task PumpWebSocketToTcp(WebSocket ws, NetworkStream tcpStream, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
        {
            var payload = await WebSocketReader.ReadWholeMessage(ws, ct);
            if (payload is null) break;

            await TcpFramer.WriteFrame(tcpStream, payload, ct);
        }
    }

    private static async Task PumpTcpToWebSocket(NetworkStream tcpStream, WebSocket ws, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
        {
            var payload = await TcpFramer.ReadFrame(tcpStream, ct);
            if (payload is null) break;

            await ws.SendAsync(payload, WebSocketMessageType.Binary, true, ct);
        }
    }
}
