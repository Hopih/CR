using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CodeJam;
using static TcpChatServer.Package11207Helper;

namespace TcpChatServer;

public class TcpServer
{
    private readonly Socket _socket;
    private const int MaxTimeout = 5 * 60 * 1000;

    // Было: Dictionary<Socket,string>
    private readonly ConcurrentDictionary<Socket, string?> _clients = new();

    // Общий токен для graceful shutdown
    private readonly CancellationTokenSource _cts = new();

    public TcpServer(IPAddress address, int port)
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.Bind(new IPEndPoint(address, port));
    }

    public async Task StartAsync()
    {
        try
        {
            _socket.Listen(backlog: 100);

            // Бесконечный цикл приёма, пока не попросили остановить сервер
            while (!_cts.IsCancellationRequested)
            {
                Socket connectionSocket;
                try
                {
                    connectionSocket = await _socket.AcceptAsync(_cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Остановка сервера инициирована — выходим из цикла
                    break;
                }

                _clients[connectionSocket] = null;

                // Токен с линковкой к общему, чтобы корректно гасить сессии при StopAsync
                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);

                _ = Task.Run(
                    async () => await ProcessSocketConnect(connectionSocket, linkedCts),
                    linkedCts.Token);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.WriteLine(ex.ToDiagnosticString());
            await StopAsync();
        }
    }

    // Было: void StopAsync(). Делаем Task, чтобы можно было дождаться полной остановки.
    public async Task StopAsync()
    {
        try { _cts.Cancel(); } catch { /* ignore */ }

        try { _socket.Close(); } catch { /* ignore */ }

        // Аккуратно отключаем всех клиентов
        foreach (var client in _clients.Keys.ToList())
        {
            try { await client.DisconnectAsync(false); } catch { /* ignore */ }
            try { client.Dispose(); } catch { /* ignore */ }
            _clients.TryRemove(client, out _);
        }
    }

    private async Task ProcessSocketConnect(Socket socket, CancellationTokenSource ctxSource)
    {
        try
        {
            socket.ReceiveTimeout = MaxTimeout;
            socket.SendTimeout = MaxTimeout;

            var buffer = new byte[MaxPacketSize];
            var contentLength = await socket.ReceiveAsync(buffer, SocketFlags.None, ctxSource.Token);

            if (IsQueryValid(buffer, contentLength) &&
                IsHello(buffer[Command], buffer[Fullness], buffer[Query]))
            {
                var clientName = Encoding.UTF8.GetString(GetContent(buffer, contentLength));
                _clients[socket] = clientName;

                var broadcastMessage = $"К нам присоединился {clientName}";
                Console.WriteLine(broadcastMessage);
                var contentBytes = Encoding.UTF8.GetBytes(broadcastMessage);

                var package = CreatePackage(contentBytes,
                    SupportCommand.Say,
                    FullnessPackage.Full,
                    QueryType.Response);

                await BroadcastMessageAsync(socket, package, ctxSource.Token);
            }
            else
            {
                Console.WriteLine($"К нам пытался ворваться клиент {GetRemoteIpAddress(socket)}");
                _clients.TryRemove(socket, out _);
                await socket.DisconnectAsync(false);
                return;
            }

            while (socket.Connected && !ctxSource.IsCancellationRequested)
            {
                var content = new List<byte>();
                content.AddRange(Encoding.UTF8.GetBytes($"{_clients[socket]}:"));
                string message;
                contentLength = await socket.ReceiveAsync(buffer, SocketFlags.None, ctxSource.Token);
                if (!IsQueryValid(buffer, contentLength) ||
                    IsHello(buffer[Command], buffer[Fullness], buffer[Query]))
                {
                    continue;
                }
                if (IsBye(buffer[Command]))
                {
                    message = $"С нами прощается {_clients[socket]}.";
                    Console.WriteLine(message);

                    await BroadcastMessageAsync(socket,
                        CreatePackage(Encoding.UTF8.GetBytes(message),
                            SupportCommand.Say,
                            FullnessPackage.Full,
                            QueryType.Response), ctxSource.Token);

                    await socket.DisconnectAsync(false);
                    _clients.TryRemove(socket, out _);
                }
                else
                {
                    content.AddRange(Encoding.UTF8.GetBytes($"{_clients[socket]}:"));
                    content.AddRange(GetContent(buffer, contentLength));
                    if (IsPartial(buffer[Fullness]))
                    {
                        continue;
                    }

                    message = Encoding.UTF8.GetString(content.ToArray());
                    Console.WriteLine(message);

                    var resultContent = content.ToArray();
                    content.Clear();

                    var packages = GetPackagesByMessage(resultContent, SupportCommand.Say, QueryType.Response);

                    packages.ForEach(
                        async void (p) =>
                        {
                            try
                            {
                                await BroadcastMessageAsync(socket, p, ctxSource.Token);
                            }
                            catch (Exception e)
                            {
                                /* ignored */
                            }
                        });
                }
            }
        }
        catch
        {
            try { await socket.DisconnectAsync(false); } catch { /* ignore */ }
            _clients.TryRemove(socket, out _);
        }
        finally
        {
            try { socket.Dispose(); } catch { /* ignore */ }
        }
    }

    private string GetRemoteIpAddress(Socket socket)
    {
        return IPAddress.Parse(((IPEndPoint)socket.RemoteEndPoint!).Address.ToString())
               + ":" + ((IPEndPoint)socket.RemoteEndPoint!).Port;
    }

    private async Task BroadcastMessageAsync(Socket socket, byte[] message,
        CancellationToken ctx)
    {
        // Оставляем реализацию как есть — перепишем на Шаге 2
        var semaphore = new SemaphoreSlim(1);
        foreach (var client in _clients.Keys)
        {
            await semaphore.WaitAsync(ctx);
            await client.SendAsync(message, SocketFlags.None, ctx);
            semaphore.Release(1);
        }

        semaphore.Dispose();
    }
}