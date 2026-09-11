using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using WordGuesserServer;

namespace WordGuesserTests;

public class CommandExchangeTests
{
    [Theory]
    [InlineData(CommandResponse.Ok)]
    [InlineData(CommandResponse.PlayerNotJoined)]
    [InlineData(CommandResponse.IncorrectSender)]
    [InlineData(CommandResponse.CommandIncorrect)]
    [InlineData(CommandResponse.PackageIncorrect)]
    [InlineData(CommandResponse.SessionNotFound)]
    public void Response_Decodes_After_Parsing(CommandResponse expected)
    {
        var parsed = GuessPackageParser.TryParse(GuessCommandHelper.CommandResponse(expected), out var status);

        Assert.Equal(CommandResponse.Ok, status);
        Assert.NotNull(parsed);
        Assert.Single(parsed.Value.Payload);
        Assert.Equal(expected, GuessCommandHelper.Decode<CommandResponse>(parsed.Value.Command, parsed.Value.Payload));
    }

    [Fact]
    public async Task Socket_Extensions_Send_Payload_Without_Nested_Packets()
    {
        using var pair = await SocketPair.Connect();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var id = Guid.NewGuid();

        await pair.Server.SendSessionId(id);
        var session = await ReceivePacket(pair.Client, timeout.Token);
        Assert.Equal(Command.SessionId, session.Command);
        Assert.Equal(id, GuessCommandHelper.Decode<Guid>(session.Command, session.Payload));

        await pair.Server.SendReadyToGuess("*****");
        var ready = await ReceivePacket(pair.Client, timeout.Token);
        Assert.Equal(Command.ReadyToGuess, ready.Command);
        Assert.Equal("*****", GuessCommandHelper.Decode<string>(ready.Command, ready.Payload));

        var responses = new (Func<Task> Send, CommandResponse Expected)[]
        {
            (pair.Server.SendOk, CommandResponse.Ok),
            (pair.Server.SendPlayerNotJoined, CommandResponse.PlayerNotJoined),
            (pair.Server.SendIncorrectSender, CommandResponse.IncorrectSender),
            (pair.Server.SendSessionNotFound, CommandResponse.SessionNotFound)
        };
        foreach (var (send, expected) in responses)
        {
            await send();
            var response = await ReceivePacket(pair.Client, timeout.Token);
            Assert.Equal(Command.CommandResponse, response.Command);
            Assert.Single(response.Payload);
            Assert.Equal(expected, GuessCommandHelper.Decode<CommandResponse>(response.Command, response.Payload));
        }
    }

    [Fact]
    public async Task Join_Sends_Decodable_Roles_To_Both_Players()
    {
        using var host = await SocketPair.Connect();
        using var guest = await SocketPair.Connect();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var id = Guid.NewGuid();
        var sessions = new ConcurrentDictionary<Guid, GameSession>();
        sessions[id] = new GameSession { Id = id, Hosted = new Player { Connector = host.Server } };
        var join = GuessPackageParser.TryParse(GuessCommandHelper.Join(id), out _)!.Value;

        await new JoinCommandHandler().Invoke(guest.Server, sessions, join.Payload, timeout.Token);

        var hostReady = await ReceivePacket(host.Client, timeout.Token);
        var guestReady = await ReceivePacket(guest.Client, timeout.Token);
        Assert.Equal(Command.Ready, hostReady.Command);
        Assert.Equal(Command.Ready, guestReady.Command);
        Assert.Equal(sizeof(int), hostReady.Payload.Length);
        Assert.Equal(sizeof(int), guestReady.Payload.Length);
        Assert.Equal(PlayerRole.Hosted, GuessCommandHelper.Decode<PlayerRole>(hostReady.Command, hostReady.Payload));
        Assert.Equal(PlayerRole.Guesser, GuessCommandHelper.Decode<PlayerRole>(guestReady.Command, guestReady.Payload));
    }

    private static async Task<(Command Command, byte[] Payload)> ReceivePacket(Socket socket, CancellationToken ct)
    {
        var header = new byte[3];
        await ReceiveExactly(socket, header, ct);
        var packet = new byte[header[GuessPackageMeta.LengthByteIndex] + 4];
        header.CopyTo(packet, 0);
        await ReceiveExactly(socket, packet.AsMemory(header.Length), ct);
        var parsed = GuessPackageParser.TryParse(packet, out var status);
        Assert.Equal(CommandResponse.Ok, status);
        Assert.NotNull(parsed);
        return parsed.Value;
    }

    private static async Task ReceiveExactly(Socket socket, Memory<byte> buffer, CancellationToken ct)
    {
        while (!buffer.IsEmpty)
        {
            var count = await socket.ReceiveAsync(buffer, SocketFlags.None, ct);
            Assert.True(count > 0, "Connection closed before a complete packet was received.");
            buffer = buffer[count..];
        }
    }

    private sealed class SocketPair(Socket client, Socket server) : IDisposable
    {
        public Socket Client { get; } = client;
        public Socket Server { get; } = server;

        public static async Task<SocketPair> Connect()
        {
            using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);
            var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                await client.ConnectAsync(listener.LocalEndPoint!, timeout.Token);
                return new SocketPair(client, await listener.AcceptAsync(timeout.Token));
            }
            catch
            {
                client.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            Client.Dispose();
            Server.Dispose();
        }
    }
}
