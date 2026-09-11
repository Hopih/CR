using WordGuesser.Protocol;
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
        var ct = cts.Token;
        try
        {
            await connection.SendOk(ct);
            while (!ct.IsCancellationRequested)
            {
                var packet = await connection.ReceivePacketAsync(ct);
                if (packet == null)
                    break;

                ICommandHandler commandHandler;
                try
                {
                    commandHandler = CommandHandlerFactory.GetHandler(packet.Value.Command);
                }
                catch (NotSupportedException)
                {
                    await connection.SendResponseCommand(CommandResponse.CommandIncorrect, ct);
                    continue;
                }

                try
                {
                    GuessCommandHelper.Decode(packet.Value.Command, packet.Value.Payload);
                }
                catch (FormatException)
                {
                    await connection.SendResponseCommand(CommandResponse.PackageIncorrect, ct);
                    continue;
                }

                await commandHandler.Invoke(connection, _gameSessions, packet.Value.Payload, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or SocketException or ObjectDisposedException)
        {
            Console.Error.WriteLine($"Connection closed: {ex.Message}");
        }
        finally
        {
            connection.Dispose();
        }
    }
}
