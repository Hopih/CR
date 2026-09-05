using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace WordGuesserServer;

public class WgServer
{
    private readonly Socket _server;
    private ConcurrentDictionary<Guid, GameSession> _gameSessions = new();

    public WgServer(IPEndPoint  endPoint)
    {
        _server = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _server.Bind(endPoint);
        _server.Listen(8);
    }

    public async Task StartServer()
    {
        var cts = new CancellationTokenSource();
        while (!cts.IsCancellationRequested)
        {
            try
            {
                var connection = await _server.AcceptAsync(cts.Token);

                _ = Task.Run(async () => await HandleConnection(connection,  cts), cts.Token);
            }
            catch
            {
                await _server.DisconnectAsync(true, cts.Token);
            }
        }
    }

    private async Task HandleConnection(Socket connection, CancellationTokenSource cts)
    {
        await connection.SendAsync(new GuessPackageBuilder(
                    GuessCommandHelper.CommandResponse(CommandResponse.Ok), Command.CommandResponse)
                .Build(),
            SocketFlags.None);
        
        ArraySegment<byte> buffer = new byte[256];

        do
        {
            var messageByteCount = await connection.ReceiveAsync(buffer, SocketFlags.None);

            var message = buffer[..messageByteCount];

            var response = GuessPackageParser.TryParse(message, out var commandResponse);

            if (response == null || commandResponse != CommandResponse.Ok)
            {
                await connection.SendAsync(new GuessPackageBuilder(
                            GuessCommandHelper.CommandResponse(commandResponse), Command.CommandResponse)
                        .Build(),
                    SocketFlags.None);
                continue;
            }

            var commandHandler = CommandHandlerFactory.GetHandler(response.Value.Command);

            await commandHandler.Invoke(connection, _gameSessions, response.Value.Payload, cts.Token);
        }
        while(!cts.IsCancellationRequested || connection.Connected);
    }
}