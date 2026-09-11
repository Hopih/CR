using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using WordGuesser.Protocol;
using WordGuesserClient;
using WordGuesserServer;

namespace WordGuesserTests;

public class ClientIntegrationTests
{
    [Fact]
    public async Task Two_Actual_Clients_Complete_A_Match_And_Exit_Without_Extra_Input()
    {
        await using var server = new WgServer(new IPEndPoint(IPAddress.Loopback, 0));
        var serverTask = server.StartServer();
        await using var first = new ClientRun(server.EndPoint.Port);
        await using var second = new ClientRun(server.EndPoint.Port);
        await first.WaitState(ClientState.Lobby);
        await second.WaitState(ClientState.Lobby);
        first.Type("create");
        await first.WaitState(ClientState.WaitingForPlayer);
        Assert.NotNull(first.Client.SessionId);
        second.Type($"join {first.Client.SessionId}");
        await first.WaitState(ClientState.SettingWord);
        await second.WaitState(ClientState.WaitingForWord);
        Assert.Equal(first.Client.SessionId, second.Client.SessionId);

        // The first player wins: setting a word, then guessing, then setting again.
        for (var round = 0; round < 3; round++)
        {
            var host = round % 2 == 0 ? first : second;
            var guesser = round % 2 == 0 ? second : first;
            host.Type("word КОТ");
            await host.WaitState(ClientState.Watching);
            await guesser.WaitState(ClientState.Guessing);
            if (round == 1) guesser.Type("guess кот");
            else
            {
                for (var attempt = 0; attempt < 5; attempt++)
                {
                    guesser.ClearMessages();
                    guesser.Type("guess ДОМ");
                    var left = 4 - attempt;
                    await guesser.WaitMessage($"Осталось попыток: {left}.");
                }
            }
            await first.WaitMessage($"Счёт (вы : соперник): {round + 1}:0.");
            if (round < 2)
            {
                await guesser.WaitState(ClientState.SettingWord);
                await host.WaitState(ClientState.WaitingForWord);
            }
        }
        await first.Run.WaitAsync(TimeSpan.FromSeconds(5));
        await second.Run.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(first.Client.HasError);
        Assert.False(second.Client.HasError);
        Assert.Equal(3, first.Client.YourScore);
        Assert.Equal(3, second.Client.OpponentScore);
        Assert.Equal(ClientState.Finished, first.Client.State);
        Assert.Equal(0, server.SessionCount);
        await server.StopAsync();
        await serverTask;
    }

    [Theory]
    [InlineData("exit")]
    [InlineData("cancel")]
    [InlineData(null)]
    public async Task Exit_Or_Eof_Ends_The_Opponents_Session(string? input)
    {
        await using var server = new WgServer(new IPEndPoint(IPAddress.Loopback, 0));
        _ = server.StartServer();
        await using var first = new ClientRun(server.EndPoint.Port);
        await using var second = new ClientRun(server.EndPoint.Port);
        await first.WaitState(ClientState.Lobby);
        await second.WaitState(ClientState.Lobby);
        first.Type("create");
        await first.WaitState(ClientState.WaitingForPlayer);
        second.Type($"join {first.Client.SessionId}");
        await second.WaitState(ClientState.WaitingForWord);
        if (input == "cancel") first.Cancel();
        else first.Type(input);
        await first.Run.WaitAsync(TimeSpan.FromSeconds(5));
        await second.Run.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(0, server.SessionCount);
        Assert.False(first.Client.HasError);
        Assert.False(second.Client.HasError);
    }

    [Fact]
    public async Task Server_Disconnect_And_User_Cancellation_Interrupt_Pending_Input()
    {
        await using var server = new WgServer(new IPEndPoint(IPAddress.Loopback, 0));
        _ = server.StartServer();
        await using var first = new ClientRun(server.EndPoint.Port);
        await first.WaitState(ClientState.Lobby);
        first.Cancel();
        await first.Run.WaitAsync(TimeSpan.FromSeconds(5));
        await using var second = new ClientRun(server.EndPoint.Port);
        await second.WaitState(ClientState.Lobby);
        await server.StopAsync();
        await second.Run.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(ClientState.Finished, second.Client.State);
    }

    [Fact]
    public async Task Rejected_Join_Can_Be_Retried_And_Invalid_Word_Can_Be_Corrected()
    {
        await using var server = new WgServer(new IPEndPoint(IPAddress.Loopback, 0));
        _ = server.StartServer();
        await using var host = new ClientRun(server.EndPoint.Port);
        await using var guest = new ClientRun(server.EndPoint.Port);
        await host.WaitState(ClientState.Lobby);
        await guest.WaitState(ClientState.Lobby);
        guest.Type($"join {Guid.NewGuid()}");
        await guest.WaitMessage("Сессия не найдена");
        Assert.Null(guest.Client.SessionId);
        host.Type("create");
        await host.WaitState(ClientState.WaitingForPlayer);
        guest.Type($"join {host.Client.SessionId}");
        await host.WaitState(ClientState.SettingWord);
        await guest.WaitState(ClientState.WaitingForWord);
        host.Type("word КОТ1");
        await host.WaitMessage("В слове допустимы");
        await host.WaitState(ClientState.SettingWord);
        host.Type("word");
        await host.WaitMessage("Введите слово:");
        host.Type("КОТ");
        await guest.WaitState(ClientState.Guessing);
        guest.Type("guess КОТИК");
        await guest.WaitMessage("Попытка должна");
        Assert.Equal(5, guest.Client.AttemptsLeft);
        guest.Type("guess кот");
        await guest.WaitMessage("Раунд выигран");
    }

    [Fact]
    public async Task Pending_Create_Is_Not_Sent_Twice_And_Exit_Sends_End()
    {
        using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        await using var client = new ClientRun(((IPEndPoint)listener.LocalEndPoint!).Port);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var peer = await listener.AcceptAsync(timeout.Token);
        await peer.SendPacketAsync(GuessCommandHelper.CommandResponse(CommandResponse.Ok), timeout.Token);
        await client.WaitState(ClientState.Lobby);
        client.Type("create");
        Assert.Equal(Command.Create, (await peer.ReceivePacketAsync(timeout.Token))!.Value.Command);
        Assert.Null(client.Client.SessionId);
        client.Type("create");
        await client.WaitMessage("Ожидаем подтверждение предыдущей команды.");
        client.Type("exit");
        Assert.Equal(Command.End, (await peer.ReceivePacketAsync(timeout.Token))!.Value.Command);
        await client.Run.WaitAsync(timeout.Token);
    }

    [Fact]
    public async Task Connection_Failure_Is_Reported_Without_Unhandled_Exception()
    {
        using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)listener.LocalEndPoint!).Port; // Bound but not listening.
        await using var client = new ClientRun(port);
        await client.Run.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(client.Client.HasError);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Invalid_Server_Message_Stops_Client_While_Waiting_For_Input(bool wrongDirection)
    {
        using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        await using var client = new ClientRun(((IPEndPoint)listener.LocalEndPoint!).Port);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var peer = await listener.AcceptAsync(timeout.Token);
        await peer.SendPacketAsync(GuessCommandHelper.CommandResponse(CommandResponse.Ok), timeout.Token);
        await client.WaitState(ClientState.Lobby);
        await peer.SendPacketAsync(wrongDirection ? GuessCommandHelper.Create()
            : new GuessPackageBuilder([1], Command.Ready).Build(), timeout.Token);
        await client.Run.WaitAsync(timeout.Token);
        Assert.True(client.Client.HasError);
        Assert.Equal(ClientState.Finished, client.Client.State);
    }

    private sealed class ClientRun : IAsyncDisposable
    {
        private readonly ScriptUi _ui = new();
        private readonly CancellationTokenSource _stop = new();
        public GameClient Client { get; }
        public Task Run { get; }
        public ClientRun(int port)
        {
            Client = new GameClient("127.0.0.1", port, _ui);
            Run = Client.Start(_stop.Token);
        }
        public void Type(string? line) => _ui.Input.Writer.TryWrite(line);
        public void Cancel() => _stop.Cancel();
        public async Task WaitState(ClientState state)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (Client.State != state || Client.Pending != null) await Task.Delay(5, timeout.Token);
        }
        public void ClearMessages() { while (_ui.Output.Reader.TryRead(out _)) { } }
        public async Task WaitMessage(string text)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (!(await _ui.Output.Reader.ReadAsync(timeout.Token)).Contains(text, StringComparison.Ordinal)) { }
        }
        public async ValueTask DisposeAsync()
        {
            _stop.Cancel();
            await Run.WaitAsync(TimeSpan.FromSeconds(5));
            _stop.Dispose();
        }
    }

    private sealed class ScriptUi : IClientUi
    {
        public Channel<string?> Input { get; } = Channel.CreateUnbounded<string?>();
        public Channel<string> Output { get; } = Channel.CreateUnbounded<string>();
        public ValueTask<string?> ReadLineAsync(CancellationToken ct) => Input.Reader.ReadAsync(ct);
        public void WriteLine(string message) => Output.Writer.TryWrite(message);
    }
}
