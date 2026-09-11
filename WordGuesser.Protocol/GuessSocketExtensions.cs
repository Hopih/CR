using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace WordGuesser.Protocol;

public static class GuessSocketExtensions
{
    // All handlers sharing a socket must also share the same send/receive gates.
    private static readonly ConditionalWeakTable<Socket, Lazy<GuessConnection>> Connections = new();

    private static GuessConnection Connection(Socket socket) =>
        Connections.GetValue(socket, static key => new Lazy<GuessConnection>(
            () => new GuessConnection(new NetworkStream(key, ownsSocket: true)))).Value;

    public static Task SendPacketAsync(this Socket socket, byte[] packet, CancellationToken ct = default) =>
        Connection(socket).SendAsync(packet, ct);

    public static Task<(Command Command, byte[] Payload)?> ReceivePacketAsync(this Socket socket,
        CancellationToken ct = default) => Connection(socket).ReceiveAsync(ct);
}
