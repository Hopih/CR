using System.Net;
using System.Net.Sockets;

namespace WordGuesserTests;

internal static class RawSocketTestSupport
{
    public static async Task<(Socket Sender, Socket Receiver)> Connect(CancellationToken ct)
    {
        using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        var sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await sender.ConnectAsync(listener.LocalEndPoint!, ct);
            return (sender, await listener.AcceptAsync(ct));
        }
        catch { sender.Dispose(); throw; }
    }

    // Deliberately bypasses packet validation to inject malformed and combined data in tests.
    public static async Task SendAll(Socket socket, ReadOnlyMemory<byte> bytes, CancellationToken ct)
    {
        while (!bytes.IsEmpty)
        {
            var sent = await socket.SendAsync(bytes, SocketFlags.None, ct);
            Assert.True(sent > 0);
            bytes = bytes[sent..];
        }
    }
}
