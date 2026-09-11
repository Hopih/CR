using System.Net;
using System.Net.Sockets;
using WordGuesser.Protocol;
using WordGuesserServer;

namespace WordGuesserTests;

public class ServerLifecycleTests
{
    [Theory]
    [InlineData("000")]
    [InlineData("111")]
    [InlineData("01010")]
    public async Task Full_Match_Swaps_Roles_Counts_Points_And_Ends_At_Three(string winners)
    {
        await using var game = new RunningServer();
        using var first = await game.Connect();
        using var second = await game.Connect();
        Socket[] players = [first, second];
        var id = await game.Create(first);
        await game.Join(first, second, id);
        int[] scores = [0, 0];

        for (var roundIndex = 0; roundIndex < winners.Length; roundIndex++)
        {
            var hostIndex = roundIndex % 2;
            var guesserIndex = 1 - hostIndex;
            var winnerIndex = winners[roundIndex] - '0';
            await players[hostIndex].SendPacketAsync(GuessCommandHelper.Word(id, "кот"), game.Token);
            var accepted = await game.Receive(players[hostIndex]);
            Assert.Equal(CommandResponse.Ok, GuessCommandHelper.Decode<CommandResponse>(accepted.Command, accepted.Payload));
            var ready = await game.Receive(players[guesserIndex]);
            Assert.Equal(Command.ReadyToGuess, ready.Command);
            Assert.Equal("***", GuessCommandHelper.Decode<string>(ready.Command, ready.Payload));

            // Exercise both a win on the last attempt and loss on exhaustion.
            for (var i = 0; i < 5; i++)
            {
                var guessed = i == 4 && winnerIndex == guesserIndex;
                await players[guesserIndex].SendPacketAsync(GuessCommandHelper.Guess(id, guessed ? "КоТ" : "ДОМ"), game.Token);
                foreach (var player in players)
                {
                    var packet = await game.Receive(player);
                    Assert.Equal(Command.AttemptResult, packet.Command);
                    var attempt = GuessCommandHelper.Decode<AttemptResult>(packet.Command, packet.Payload);
                    Assert.Equal(4 - i, attempt.AttemptsLeft);
                    Assert.Equal(guessed ? "КОТ" : "*О*", attempt.Mask);
                }
            }
            scores[winnerIndex]++;
            for (var i = 0; i < 2; i++)
            {
                var packet = await game.Receive(players[i]);
                Assert.Equal(Command.RoundResult, packet.Command);
                Assert.Equal(new RoundResult(i == winnerIndex, scores[i], scores[1 - i], 0, "КОТ"),
                    GuessCommandHelper.Decode<RoundResult>(packet.Command, packet.Payload));
            }

            if (scores[winnerIndex] == 3)
            {
                for (var i = 0; i < 2; i++)
                {
                    var packet = await game.Receive(players[i]);
                    Assert.Equal(Command.MatchResult, packet.Command);
                    Assert.Equal(new MatchResult(i == winnerIndex, scores[i], scores[1 - i]),
                        GuessCommandHelper.Decode<MatchResult>(packet.Command, packet.Payload));
                    Assert.Equal(Command.End, (await game.Receive(players[i])).Command);
                }
                Assert.Equal(0, game.Server.SessionCount);
                await game.RequestError(first, GuessCommandHelper.Guess(id, "КОТ"), CommandResponse.SessionNotFound);
            }
            else
            {
                for (var i = 0; i < 2; i++)
                {
                    var packet = await game.Receive(players[i]);
                    Assert.Equal(Command.Ready, packet.Command);
                    Assert.Equal(i == guesserIndex ? PlayerRole.Hosted : PlayerRole.Guesser,
                        GuessCommandHelper.Decode<PlayerRole>(packet.Command, packet.Payload));
                }
            }
        }
    }

    [Fact]
    public async Task Invalid_Actions_Do_Not_Change_The_Round_Or_Consume_Attempts()
    {
        await using var game = new RunningServer();
        using var host = await game.Connect();
        using var guest = await game.Connect();
        using var outsider = await game.Connect();
        var id = await game.Create(host);
        await game.RequestError(host, GuessCommandHelper.Word(id, "КОТ"), CommandResponse.PlayerNotJoined);
        await game.Join(host, guest, id);
        await game.RequestError(guest, GuessCommandHelper.Guess(id, "КОТ"), CommandResponse.InvalidState);
        await game.RequestError(guest, GuessCommandHelper.Word(id, "КОТ"), CommandResponse.IncorrectSender);
        await game.RequestError(outsider, GuessCommandHelper.Word(id, "КОТ"), CommandResponse.IncorrectSender);
        await game.RequestError(host, GuessCommandHelper.Word(id, "КОТ1"), CommandResponse.InvalidWord);
        await host.SendPacketAsync(GuessCommandHelper.Word(id, "КОТ"), game.Token);
        await game.Receive(host);
        await game.Receive(guest);
        await game.RequestError(host, GuessCommandHelper.Word(id, "ДОМ"), CommandResponse.InvalidState);
        await game.RequestError(host, GuessCommandHelper.Guess(id, "КОТ"), CommandResponse.IncorrectSender);
        await game.RequestError(outsider, GuessCommandHelper.Guess(id, "КОТ"), CommandResponse.IncorrectSender);
        await game.RequestError(guest, GuessCommandHelper.Guess(id, "КОТИК"), CommandResponse.InvalidGuess);
        await game.RequestError(guest, GuessCommandHelper.Guess(id, "К Т"), CommandResponse.InvalidGuess);
        await game.RequestError(host, GuessCommandHelper.Swap(), CommandResponse.InvalidState);
        await guest.SendPacketAsync(GuessCommandHelper.Guess(id, "кот"), game.Token);
        foreach (var socket in new[] { host, guest })
        {
            var attempt = await game.Receive(socket);
            Assert.Equal(4, GuessCommandHelper.Decode<AttemptResult>(attempt.Command, attempt.Payload).AttemptsLeft);
            var round = await game.Receive(socket);
            Assert.Equal(Command.RoundResult, round.Command);
            Assert.Equal(ReferenceEquals(socket, guest), GuessCommandHelper.Decode<RoundResult>(round.Command, round.Payload).Won);
            Assert.Equal(Command.Ready, (await game.Receive(socket)).Command);
        }
        await game.RequestError(guest, GuessCommandHelper.Guess(id, "КОТ"), CommandResponse.IncorrectSender);
        await game.RequestError(host, GuessCommandHelper.Guess(id, "КОТ"), CommandResponse.InvalidState);
    }

    [Fact]
    public async Task Create_Join_Rejects_Duplicate_Membership_And_Third_Player()
    {
        await using var game = new RunningServer();
        using var host = await game.Connect();
        using var guest = await game.Connect();
        using var third = await game.Connect();
        var id = await game.Create(host);
        await game.RequestError(host, GuessCommandHelper.Create(), CommandResponse.AlreadyInSession);
        await game.RequestError(host, GuessCommandHelper.Join(id), CommandResponse.AlreadyInSession);
        await game.Join(host, guest, id);
        Assert.Equal(1, game.Server.SessionCount);
        await game.RequestError(guest, GuessCommandHelper.Create(), CommandResponse.AlreadyInSession);
        await game.RequestError(guest, GuessCommandHelper.Join(id), CommandResponse.AlreadyInSession);
        await game.RequestError(third, GuessCommandHelper.Join(id), CommandResponse.SessionFull);
        await game.RequestError(third, GuessCommandHelper.Join(Guid.NewGuid()), CommandResponse.SessionNotFound);

        // The rejected third player remains free to create an independent session.
        Assert.NotEqual(id, await game.Create(third));
        Assert.Equal(2, game.Server.SessionCount);
    }

    [Fact]
    public async Task Concurrent_Joins_Admit_Exactly_One_Player()
    {
        await using var game = new RunningServer();
        using var host = await game.Connect();
        using var first = await game.Connect();
        using var second = await game.Connect();
        var id = await game.Create(host);
        await Task.WhenAll(first.SendPacketAsync(GuessCommandHelper.Join(id), game.Token),
            second.SendPacketAsync(GuessCommandHelper.Join(id), game.Token));
        var a = await game.Receive(first);
        var b = await game.Receive(second);
        Assert.True((a.Command == Command.SessionId) != (b.Command == Command.SessionId));
        var rejected = a.Command == Command.CommandResponse ? a : b;
        Assert.Equal(Command.CommandResponse, rejected.Command);
        Assert.Equal(CommandResponse.SessionFull, GuessCommandHelper.Decode<CommandResponse>(rejected.Command, rejected.Payload));
        var winner = a.Command == Command.SessionId ? first : second;
        Assert.Equal(Command.Ready, (await game.Receive(winner)).Command);
        Assert.Equal(Command.Ready, (await game.Receive(host)).Command);
        Assert.Equal(1, game.Server.SessionCount);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task Disconnect_Removes_Session_And_Notifies_Opponent(bool hostLeaves, bool resetConnection)
    {
        await using var game = new RunningServer();
        using var host = await game.Connect();
        using var guest = await game.Connect();
        var id = await game.Create(host);
        await game.Join(host, guest, id);
        var leaving = hostLeaves ? host : guest;
        var remaining = hostLeaves ? guest : host;
        if (resetConnection) leaving.LingerState = new LingerOption(true, 0);
        leaving.Dispose();
        Assert.Equal(Command.End, (await game.Receive(remaining)).Command);
        Assert.Equal(0, game.Server.SessionCount);
        using var newcomer = await game.Connect();
        await game.RequestError(newcomer, GuessCommandHelper.Join(id), CommandResponse.SessionNotFound);
        Assert.NotEqual(id, await game.Create(remaining));
    }

    [Fact]
    public async Task Waiting_Host_Disconnect_Removes_Empty_Session()
    {
        await using var game = new RunningServer();
        using var host = await game.Connect();
        var id = await game.Create(host);
        host.Dispose();
        await game.Until(() => game.Server.ConnectionCount == 0);
        Assert.Equal(0, game.Server.SessionCount);
        using var newcomer = await game.Connect();
        await game.RequestError(newcomer, GuessCommandHelper.Join(id), CommandResponse.SessionNotFound);
    }

    [Fact]
    public async Task End_Releases_Both_Members_And_Does_Not_Affect_Other_Sessions()
    {
        await using var game = new RunningServer();
        using var host = await game.Connect();
        using var guest = await game.Connect();
        using var otherHost = await game.Connect();
        var id = await game.Create(host);
        await game.Join(host, guest, id);
        await game.Create(otherHost);
        await guest.SendPacketAsync(GuessCommandHelper.End(), game.Token);
        Assert.Equal(Command.End, (await game.Receive(host)).Command);
        Assert.Equal(Command.End, (await game.Receive(guest)).Command);
        Assert.Equal(1, game.Server.SessionCount);
        await game.Create(host);
        await game.Create(guest);
        Assert.Equal(3, game.Server.SessionCount);
        await game.RequestError(otherHost, GuessCommandHelper.Create(), CommandResponse.AlreadyInSession);
    }

    [Fact]
    public async Task Invalid_Payload_Does_Not_End_Connection_But_Invalid_Frame_Does()
    {
        await using var game = new RunningServer();
        using var client = await game.Connect();
        await game.RequestError(client, new GuessPackageBuilder([1], Command.Join).Build(), CommandResponse.PackageIncorrect);
        await game.RequestError(client, GuessCommandHelper.Ready(PlayerRole.Guesser), CommandResponse.CommandIncorrect);
        await game.Create(client);
        await RawSocketTestSupport.SendAll(client, new byte[] { 0 }, game.Token);
        Assert.Null(await client.ReceivePacketAsync(game.Token));
        await game.Until(() => game.Server.SessionCount == 0);
        using var other = await game.Connect();
        await game.Create(other);
    }

    [Fact]
    public async Task Stop_Cancels_Pending_Reads_And_Cleans_All_Sessions()
    {
        await using var game = new RunningServer();
        using var first = await game.Connect();
        using var second = await game.Connect();
        await game.Create(first);
        await game.Create(second);
        await RawSocketTestSupport.SendAll(second, new byte[] { 2 }, game.Token); // An unfinished header.
        await game.Server.StopAsync().WaitAsync(game.Token);
        await game.Run.WaitAsync(game.Token);
        Assert.Equal(0, game.Server.SessionCount);
        Assert.Equal(0, game.Server.ConnectionCount);
        Assert.Null(await first.ReceivePacketAsync(game.Token));
        await game.Server.StopAsync();
    }

    [Fact]
    public async Task External_Cancellation_Stops_Server_And_Releases_Listener()
    {
        using var cancellation = new CancellationTokenSource();
        await using var server = new WgServer(new IPEndPoint(IPAddress.Loopback, 0));
        var endpoint = server.EndPoint;
        var run = server.StartServer(cancellation.Token);
        cancellation.Cancel();
        await run.WaitAsync(TimeSpan.FromSeconds(5));
        await using var replacement = new WgServer(endpoint);
        Assert.Equal(endpoint, replacement.EndPoint);
    }

    private sealed class RunningServer : IAsyncDisposable
    {
        private readonly CancellationTokenSource _timeout = new(TimeSpan.FromSeconds(10));
        public CancellationToken Token => _timeout.Token;
        public WgServer Server { get; } = new(new IPEndPoint(IPAddress.Loopback, 0));
        public Task Run { get; }
        public RunningServer() => Run = Server.StartServer();

        public async Task<Socket> Connect()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(Server.EndPoint, Token);
                var response = await Receive(socket);
                Assert.Equal(Command.CommandResponse, response.Command);
                Assert.Equal(CommandResponse.Ok, GuessCommandHelper.Decode<CommandResponse>(response.Command, response.Payload));
                return socket;
            }
            catch { socket.Dispose(); throw; }
        }

        public async Task<(Command Command, byte[] Payload)> Receive(Socket socket)
        {
            var packet = await socket.ReceivePacketAsync(Token);
            Assert.NotNull(packet);
            return packet.Value;
        }

        public async Task<Guid> Create(Socket socket)
        {
            await socket.SendPacketAsync(GuessCommandHelper.Create(), Token);
            var packet = await Receive(socket);
            Assert.Equal(Command.SessionId, packet.Command);
            return GuessCommandHelper.Decode<Guid>(packet.Command, packet.Payload);
        }

        public async Task Join(Socket host, Socket guest, Guid id)
        {
            await guest.SendPacketAsync(GuessCommandHelper.Join(id), Token);
            var packet = await Receive(guest);
            Assert.Equal(Command.SessionId, packet.Command);
            Assert.Equal(id, GuessCommandHelper.Decode<Guid>(packet.Command, packet.Payload));
            var guestReady = await Receive(guest);
            var hostReady = await Receive(host);
            Assert.Equal(PlayerRole.Guesser, GuessCommandHelper.Decode<PlayerRole>(guestReady.Command, guestReady.Payload));
            Assert.Equal(PlayerRole.Hosted, GuessCommandHelper.Decode<PlayerRole>(hostReady.Command, hostReady.Payload));
        }

        public async Task RequestError(Socket socket, byte[] packet, CommandResponse expected)
        {
            await socket.SendPacketAsync(packet, Token);
            var response = await Receive(socket);
            Assert.Equal(Command.CommandResponse, response.Command);
            Assert.Equal(expected, GuessCommandHelper.Decode<CommandResponse>(response.Command, response.Payload));
        }

        public async Task Until(Func<bool> condition)
        {
            while (!condition()) await Task.Delay(10, Token);
        }

        public async ValueTask DisposeAsync()
        {
            await Server.DisposeAsync();
            _timeout.Dispose();
        }
    }
}
