using System.Buffers.Binary;
using System.Net.Sockets;

namespace pq_chat_httpserver.Realtime.Transport;

public static class TcpFramer
{
    public static async Task WriteFrame(NetworkStream stream, byte[] payload, CancellationToken ct)
    {
        var header = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(header, payload.Length);

        await stream.WriteAsync(header, ct);
        await stream.WriteAsync(payload, ct);
        await stream.FlushAsync(ct);
    }

    public static async Task<byte[]?> ReadFrame(NetworkStream stream, CancellationToken ct)
    {
        var header = new byte[4];
        var got = await ReadExactly(stream, header, 0, 4, ct);
        if (got == 0) return null;

        int len = BinaryPrimitives.ReadInt32BigEndian(header);
        if (len < 0 || len > 10_000_000) throw new Exception("Bad frame length.");

        var payload = new byte[len];
        await ReadExactly(stream, payload, 0, len, ct);
        return payload;
    }

    private static async Task<int> ReadExactly(NetworkStream stream, byte[] buf, int off, int count, CancellationToken ct)
    {
        int total = 0;
        while (total < count)
        {
            int n = await stream.ReadAsync(buf.AsMemory(off + total, count - total), ct);
            if (n == 0) return total;
            total += n;
        }
        return total;
    }
}
