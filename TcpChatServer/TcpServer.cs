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
    private readonly Dictionary<Socket,string> _clients = new();

    public TcpServer(IPAddress address, int port)
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.Bind(new IPEndPoint(address, port));
    }

    public async Task StartAsync()
    {
        try
        {
            _socket.Listen();
            do
            {
                var cancellationToken = new CancellationTokenSource();

                cancellationToken.CancelAfter(MaxTimeout);
                cancellationToken.Token.Register(async () =>
                {
                    if (_clients.Count == 0)
                    {
                        StopAsync();
                    }
                });

                var connectionSocket = await _socket.AcceptAsync(cancellationToken.Token);
                _clients.Add(connectionSocket, null);
                var innerCancellationToken = new CancellationTokenSource();
                _ = Task.Run(
                    async () =>
                        await ProcessSocketConnect(connectionSocket,
                            innerCancellationToken), innerCancellationToken.Token);
            } while (_clients.Count != 0);
        }
        catch (TaskCanceledException tcex)
        {
            Console.WriteLine(tcex.ToDiagnosticString());
            //ignored
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.ToDiagnosticString());
            StopAsync();
        }
    }

    private void StopAsync()
    {
        _socket.Close();
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
                Console.WriteLine($"К нам пытался ворваться клиент {GetRemoteIpAdress(socket)}");
                _clients.Remove(socket);
                await socket.DisconnectAsync(false);
            }

            while (socket.Connected)
            {
                var content = new List<byte>();
                content.AddRange(Encoding.UTF8.GetBytes($"{_clients[socket]}:"));
                string message;
                contentLength = await socket.ReceiveAsync(buffer, SocketFlags.None, ctxSource.Token);
                if (!IsQueryValid(buffer, contentLength) || 
                    IsHello(buffer[Command], buffer[Fullness], buffer[Query] ))
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
                    _clients.Remove(socket);
                }
                //Only Say Command
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
                        async p => await BroadcastMessageAsync(socket, p, ctxSource.Token));

                }
            }
        }
        catch
        {
            await socket.DisconnectAsync(false);
        }
    }

    private string GetRemoteIpAdress(Socket socket)
    {
        return  IPAddress.Parse(((IPEndPoint)socket.RemoteEndPoint!).Address.ToString()) 
                     + ":" + ((IPEndPoint)socket.RemoteEndPoint).Port;        
    }
    
    private async Task BroadcastMessageAsync(Socket socket, byte[] message, 
        CancellationToken ctx)
    {
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