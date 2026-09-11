using System.Net.Sockets;
using WordGuesser.Protocol;

namespace WordGuesserTests;

public class GuessSocketTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(256)]
    public async Task Reads_Fragmented_And_Combined_Frames(int chunkSize)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var pair = await RawSocketTestSupport.Connect(timeout.Token);
        using var sender = pair.Sender;
        using var receiver = pair.Receiver;
        var bytes = GuessCommandHelper.Create().Concat(GuessCommandHelper.Win("ЁЖИК"))
            .Concat(GuessCommandHelper.End()).ToArray();
        var first = receiver.ReceivePacketAsync(timeout.Token);
        for (var offset = 0; offset < bytes.Length; offset += chunkSize)
        {
            await RawSocketTestSupport.SendAll(sender, bytes.AsMemory(offset, Math.Min(chunkSize, bytes.Length - offset)), timeout.Token);
            await Task.Yield();
        }
        sender.Shutdown(SocketShutdown.Send);
        Assert.Equal(Command.Create, (await first)!.Value.Command);
        var win = (await receiver.ReceivePacketAsync(timeout.Token))!.Value;
        Assert.Equal("ЁЖИК", GuessCommandHelper.Decode<string>(win.Command, win.Payload));
        Assert.Equal(Command.End, (await receiver.ReceivePacketAsync(timeout.Token))!.Value.Command);
        Assert.Null(await receiver.ReceivePacketAsync(timeout.Token));
    }

    [Fact]
    public async Task Incomplete_Packet_Waits_For_Remaining_Bytes()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var pair = await RawSocketTestSupport.Connect(timeout.Token);
        using var sender = pair.Sender;
        using var receiver = pair.Receiver;
        var packet = GuessCommandHelper.Win("КОТ");
        var reading = receiver.ReceivePacketAsync(timeout.Token);
        await RawSocketTestSupport.SendAll(sender, packet.AsMemory(0, packet.Length - 1), timeout.Token);
        Assert.False(reading.IsCompleted);
        await RawSocketTestSupport.SendAll(sender, packet.AsMemory(packet.Length - 1), timeout.Token);
        Assert.Equal("КОТ", GuessCommandHelper.Decode<string>(Command.Win, (await reading)!.Value.Payload));
    }

    [Fact]
    public async Task Truncated_Frame_Fails_At_Every_Byte_Position()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var bytes = GuessCommandHelper.Win("КОТ");
        for (var length = 1; length < bytes.Length; length++)
        {
            var pair = await RawSocketTestSupport.Connect(timeout.Token);
            using var sender = pair.Sender;
            using var receiver = pair.Receiver;
            await RawSocketTestSupport.SendAll(sender, bytes.AsMemory(0, length), timeout.Token);
            sender.Shutdown(SocketShutdown.Send);
            await Assert.ThrowsAsync<EndOfStreamException>(() => receiver.ReceivePacketAsync(timeout.Token));
            Assert.True(receiver.SafeHandle.IsClosed);
        }
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 255)]
    [InlineData(2, 97)]
    [InlineData(3, 0)]
    public async Task Invalid_Frame_Closes_Socket(int index, byte value)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var pair = await RawSocketTestSupport.Connect(timeout.Token);
        using var sender = pair.Sender;
        using var receiver = pair.Receiver;
        var bytes = GuessCommandHelper.Create();
        bytes[index] = value;
        await RawSocketTestSupport.SendAll(sender, bytes, timeout.Token);
        await Assert.ThrowsAsync<InvalidDataException>(() => receiver.ReceivePacketAsync(timeout.Token));
        Assert.True(receiver.SafeHandle.IsClosed);
    }

    [Fact]
    public async Task Maximum_Payload_And_Markers_Are_Transmitted_Intact()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var pair = await RawSocketTestSupport.Connect(timeout.Token);
        using var sender = pair.Sender;
        using var receiver = pair.Receiver;
        var payload = Enumerable.Range(0, 96).Select(i => (byte)(i % 2 + 2)).ToArray();
        await sender.SendPacketAsync(new GuessPackageBuilder(payload, Command.GuessResult).Build(), timeout.Token);
        Assert.Equal(payload, (await receiver.ReceivePacketAsync(timeout.Token))!.Value.Payload);
    }

    [Fact]
    public async Task Cancelling_Active_Read_Closes_Socket()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var pair = await RawSocketTestSupport.Connect(timeout.Token);
        using var sender = pair.Sender;
        using var receiver = pair.Receiver;
        using var cancellation = new CancellationTokenSource();
        await RawSocketTestSupport.SendAll(sender, new byte[] { 2 }, timeout.Token);
        var read = receiver.ReceivePacketAsync(cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read);
        Assert.True(receiver.SafeHandle.IsClosed);
    }

    [Fact]
    public async Task Cancelling_Queued_Read_Does_Not_Close_Socket()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var pair = await RawSocketTestSupport.Connect(timeout.Token);
        using var sender = pair.Sender;
        using var receiver = pair.Receiver;
        var first = receiver.ReceivePacketAsync(timeout.Token);
        using var cancellation = new CancellationTokenSource();
        var queued = receiver.ReceivePacketAsync(cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued);
        Assert.False(receiver.SafeHandle.IsClosed);
        await sender.SendPacketAsync(GuessCommandHelper.Create(), timeout.Token);
        Assert.Equal(Command.Create, (await first)!.Value.Command);
        await sender.SendPacketAsync(GuessCommandHelper.End(), timeout.Token);
        Assert.Equal(Command.End, (await receiver.ReceivePacketAsync(timeout.Token))!.Value.Command);
    }

    [Fact]
    public async Task Invalid_Outgoing_Frame_Does_Not_Write_Or_Close_Socket()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var pair = await RawSocketTestSupport.Connect(timeout.Token);
        using var sender = pair.Sender;
        using var receiver = pair.Receiver;
        await Assert.ThrowsAsync<ArgumentException>(() => sender.SendPacketAsync([0, 0, 0, 0], timeout.Token));
        await sender.SendPacketAsync(GuessCommandHelper.Create(), timeout.Token);
        Assert.Equal(Command.Create, (await receiver.ReceivePacketAsync(timeout.Token))!.Value.Command);
    }

    [Fact]
    public async Task Failed_Send_Closes_Socket()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var pair = await RawSocketTestSupport.Connect(timeout.Token);
        using var sender = pair.Sender;
        using var receiver = pair.Receiver;
        sender.Shutdown(SocketShutdown.Send);
        await Assert.ThrowsAsync<SocketException>(() => sender.SendPacketAsync(GuessCommandHelper.Create(), timeout.Token));
        Assert.True(sender.SafeHandle.IsClosed);
    }
}
