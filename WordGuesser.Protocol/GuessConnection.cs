namespace WordGuesser.Protocol;

/// <summary>
/// Reads and writes complete frames. Owns the stream; only one instance may use a stream.
/// Independent gates allow reading and writing concurrently without interleaving frames.
/// </summary>
public sealed class GuessConnection(Stream stream) : IDisposable
{
    private readonly SemaphoreSlim _readGate = new(1, 1);
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private int _disposed;

    public async Task<(Command Command, byte[] Payload)?> ReceiveAsync(CancellationToken ct = default)
    {
        await _readGate.WaitAsync(ct);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed != 0, this);
            var header = new byte[GuessPackageMeta.PackagePayloadIndex];
            // EOF between frames is a normal disconnect. EOF within a frame is an error.
            if (await stream.ReadAsync(header.AsMemory(0, 1), ct) == 0)
            {
                Dispose();
                return null;
            }

            if (header[0] != GuessPackageMeta.Start)
                throw new InvalidDataException("Invalid packet start.");
            await stream.ReadExactlyAsync(header.AsMemory(1), ct);
            if (header[GuessPackageMeta.LengthByteIndex] > GuessPackageMeta.PayloadMaxByte)
                throw new InvalidDataException("Invalid packet header.");

            var packet = new byte[header[GuessPackageMeta.LengthByteIndex] + 4];
            header.CopyTo(packet, 0);
            await stream.ReadExactlyAsync(packet.AsMemory(header.Length), ct);
            var parsed = GuessPackageParser.TryParse(packet, out var response);
            if (parsed == null)
                throw new InvalidDataException($"Invalid packet: {response}.");
            return parsed;
        }
        catch
        {
            // After a partial read or cancellation, the next frame boundary is unknown.
            Dispose();
            throw;
        }
        finally
        {
            _readGate.Release();
        }
    }

    public async Task SendAsync(byte[] packet, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(packet);
        if (GuessPackageParser.TryParse(packet, out _) == null)
            throw new ArgumentException("Expected one complete packet.", nameof(packet));

        await _writeGate.WaitAsync(ct);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed != 0, this);
            // Stream.WriteAsync writes the entire buffer. NetworkStream handles partial socket sends.
            await stream.WriteAsync(packet.AsMemory(), ct);
        }
        catch
        {
            // A failed write may have sent only part of the frame: this connection cannot be reused.
            Dispose();
            throw;
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
            stream.Dispose();
    }
}
