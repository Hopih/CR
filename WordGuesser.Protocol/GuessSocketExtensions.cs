using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace WordGuesser.Protocol;

public static class GuessSocketExtensions
{
    // Only synchronization data is attached to a socket. No stream or socket wrapper is created.
    private sealed class Gates
    {
        public SemaphoreSlim Read { get; } = new(1, 1);
        public SemaphoreSlim Write { get; } = new(1, 1);
    }

    private static readonly ConditionalWeakTable<Socket, Gates> Synchronization = new();

    public static async Task SendPacketAsync(this Socket socket, byte[] packet, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(socket);
        ArgumentNullException.ThrowIfNull(packet);
        if (GuessPackageParser.TryParse(packet, out _) == null)
            throw new ArgumentException("Expected one complete packet.", nameof(packet));

        var gate = Synchronization.GetValue(socket, static _ => new Gates()).Write;
        // Cancellation before acquiring the gate has not changed the byte stream.
        await gate.WaitAsync(ct);
        try
        {
            ReadOnlyMemory<byte> remaining = packet;
            while (!remaining.IsEmpty)
            {
                var sent = await socket.SendAsync(remaining, SocketFlags.None, ct);
                if (sent == 0) throw new IOException("Socket stopped sending before the packet was complete.");
                remaining = remaining[sent..];
            }
        }
        catch
        {
            // A partial packet may already be on the wire. Do not reuse this socket.
            socket.Dispose();
            throw;
        }
        finally { gate.Release(); }
    }

    public static async Task<(Command Command, byte[] Payload)?> ReceivePacketAsync(this Socket socket,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(socket);
        var gate = Synchronization.GetValue(socket, static _ => new Gates()).Read;
        await gate.WaitAsync(ct);
        try
        {
            var header = new byte[GuessPackageMeta.PackagePayloadIndex];
            if (await socket.ReceiveAsync(header.AsMemory(0, 1), SocketFlags.None, ct) == 0)
            {
                socket.Dispose();
                return null;
            }
            if (header[0] != GuessPackageMeta.Start)
                throw new InvalidDataException("Invalid packet start.");
            await ReceiveExactlyAsync(socket, header.AsMemory(1), ct);
            var length = header[GuessPackageMeta.LengthByteIndex];
            if (length > GuessPackageMeta.PayloadMaxByte)
                throw new InvalidDataException("Invalid packet length.");

            var packet = new byte[length + 4];
            header.CopyTo(packet, 0);
            await ReceiveExactlyAsync(socket, packet.AsMemory(header.Length), ct);
            var parsed = GuessPackageParser.TryParse(packet, out var response);
            if (parsed == null) throw new InvalidDataException($"Invalid packet: {response}.");
            return parsed;
        }
        catch
        {
            socket.Dispose();
            throw;
        }
        finally { gate.Release(); }
    }

    private static async Task ReceiveExactlyAsync(Socket socket, Memory<byte> remaining, CancellationToken ct)
    {
        while (!remaining.IsEmpty)
        {
            var received = await socket.ReceiveAsync(remaining, SocketFlags.None, ct);
            if (received == 0) throw new EndOfStreamException("Connection closed inside a packet.");
            remaining = remaining[received..];
        }
    }
}
