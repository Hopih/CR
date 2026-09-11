using WordGuesser.Protocol;

namespace WordGuesserTests;

public class GuessConnectionTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(256)]
    public async Task Reads_Fragmented_And_Combined_Frames(int chunkSize)
    {
        var bytes = GuessCommandHelper.Create().Concat(GuessCommandHelper.Win("ЁЖИК"))
            .Concat(GuessCommandHelper.End()).ToArray();
        using var connection = new GuessConnection(new FragmentedStream(bytes, chunkSize));

        Assert.Equal(Command.Create, (await connection.ReceiveAsync())!.Value.Command);
        var win = (await connection.ReceiveAsync())!.Value;
        Assert.Equal(Command.Win, win.Command);
        Assert.Equal("ЁЖИК", GuessCommandHelper.Decode<string>(win.Command, win.Payload));
        Assert.Equal(Command.End, (await connection.ReceiveAsync())!.Value.Command);
        Assert.Null(await connection.ReceiveAsync());
    }

    [Fact]
    public async Task Truncated_Frame_Is_An_Error_At_Every_Byte_Position()
    {
        var bytes = GuessCommandHelper.Win("КОТ");
        for (var length = 1; length < bytes.Length; length++)
        {
            using var connection = new GuessConnection(new FragmentedStream(bytes[..length], 1));
            await Assert.ThrowsAsync<EndOfStreamException>(() => connection.ReceiveAsync());
            await Assert.ThrowsAsync<ObjectDisposedException>(() => connection.ReceiveAsync());
        }
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 255)]
    [InlineData(2, 97)]
    [InlineData(3, 0)]
    public async Task Invalid_Frame_Closes_Connection(int index, byte value)
    {
        var bytes = GuessCommandHelper.Create();
        bytes[index] = value;
        using var connection = new GuessConnection(new MemoryStream(bytes));
        await Assert.ThrowsAsync<InvalidDataException>(() => connection.ReceiveAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => connection.ReceiveAsync());
    }

    [Fact]
    public async Task Marker_Bytes_Inside_Payload_Are_Not_Frame_Boundaries()
    {
        byte[] payload = [2, 3, 2, 3];
        var packet = new GuessPackageBuilder(payload, Command.GuessResult).Build();
        using var connection = new GuessConnection(new FragmentedStream(packet, 1));
        Assert.Equal(payload, (await connection.ReceiveAsync())!.Value.Payload);
    }

    [Fact]
    public async Task Concurrent_Writes_Do_Not_Interleave_Even_If_Stream_Writes_Byte_By_Byte()
    {
        using var stream = new SlowWriteStream();
        using var connection = new GuessConnection(stream);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var expected = Enumerable.Range(0, 20).Select(i => $"СЛОВО{i}").ToArray();
        await Task.WhenAll(expected.Select(word => connection.SendAsync(GuessCommandHelper.Win(word), timeout.Token)));

        using var reader = new GuessConnection(new MemoryStream(stream.ToArray()));
        var actual = new List<string>();
        while (await reader.ReceiveAsync(timeout.Token) is { } packet)
            actual.Add(GuessCommandHelper.Decode<string>(packet.Command, packet.Payload));
        Assert.Equal(expected.Order(), actual.Order());
    }

    [Fact]
    public async Task Cancellation_During_Frame_Read_Closes_Connection()
    {
        using var stream = new BlockingReadStream();
        using var connection = new GuessConnection(stream);
        using var cancellation = new CancellationTokenSource();
        var reading = connection.ReceiveAsync(cancellation.Token);
        await stream.Waiting.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reading);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => connection.ReceiveAsync());
    }

    [Fact]
    public async Task Failed_Write_Closes_Connection()
    {
        using var connection = new GuessConnection(new FailingWriteStream());
        await Assert.ThrowsAsync<IOException>(() => connection.SendAsync(GuessCommandHelper.Create()));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => connection.SendAsync(GuessCommandHelper.Create()));
    }

    [Fact]
    public async Task Cancelling_A_Queued_Write_Does_Not_Close_Connection()
    {
        using var stream = new BlockingWriteStream();
        using var connection = new GuessConnection(stream);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var first = connection.SendAsync(GuessCommandHelper.Create(), timeout.Token);
        await stream.Waiting.Task.WaitAsync(timeout.Token);
        using var cancellation = new CancellationTokenSource();
        var second = connection.SendAsync(GuessCommandHelper.End(), cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
        stream.Continue.TrySetResult();
        await first;
        await connection.SendAsync(GuessCommandHelper.End(), timeout.Token);
        Assert.Equal(GuessCommandHelper.Create().Concat(GuessCommandHelper.End()), stream.ToArray());
    }

    [Fact]
    public async Task Cancelling_An_Active_Write_Closes_Connection()
    {
        using var stream = new BlockingWriteStream();
        using var connection = new GuessConnection(stream);
        using var cancellation = new CancellationTokenSource();
        var sending = connection.SendAsync(GuessCommandHelper.Create(), cancellation.Token);
        await stream.Waiting.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sending);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => connection.SendAsync(GuessCommandHelper.End()));
    }

    [Fact]
    public async Task Invalid_Outgoing_Frame_Does_Not_Write_Or_Close_Connection()
    {
        using var stream = new MemoryStream();
        using var connection = new GuessConnection(stream);
        await Assert.ThrowsAsync<ArgumentException>(() => connection.SendAsync([0, 0, 0, 0]));
        Assert.Equal(0, stream.Length);
        await connection.SendAsync(GuessCommandHelper.Create());
        Assert.Equal(GuessCommandHelper.Create(), stream.ToArray());
    }

    private sealed class FragmentedStream(byte[] bytes, int chunkSize) : MemoryStream(bytes)
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) =>
            base.ReadAsync(buffer[..Math.Min(buffer.Length, chunkSize)], ct);
    }

    private sealed class SlowWriteStream : MemoryStream
    {
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                ct.ThrowIfCancellationRequested();
                WriteByte(buffer.Span[i]);
                await Task.Yield();
            }
        }
    }

    private sealed class BlockingReadStream : MemoryStream
    {
        public TaskCompletionSource Waiting { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _sentHeader;

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            if (!_sentHeader)
            {
                _sentHeader = true;
                buffer.Span[0] = GuessPackageMeta.Start;
                return 1;
            }
            Waiting.SetResult();
            await Task.Delay(Timeout.Infinite, ct);
            return 0;
        }
    }

    private sealed class FailingWriteStream : MemoryStream
    {
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
        {
            WriteByte(buffer.Span[0]);
            return ValueTask.FromException(new IOException("Simulated failure after the first byte."));
        }
    }

    private sealed class BlockingWriteStream : MemoryStream
    {
        public TaskCompletionSource Waiting { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
        {
            WriteByte(buffer.Span[0]);
            Waiting.TrySetResult();
            await Continue.Task.WaitAsync(ct);
            await base.WriteAsync(buffer[1..], ct);
        }
    }
}
