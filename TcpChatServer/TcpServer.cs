using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TcpChatServer;

public class TcpServer
{
    private readonly Socket _socket;
    private readonly ConcurrentDictionary<Socket,string> _clients = new();

    public TcpServer(IPAddress address, int port)
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.Bind(new IPEndPoint(address, port));
    }

    public async Task Start()
    {
        try
        {
            _socket.Listen();
            while (_socket.Connected)
            {
                var connectionSocket = await _socket.AcceptAsync();
                var cancellationToken = new CancellationTokenSource();
                _ = Task.Run(
                    async () =>
                        await ProcessSocketConnect(connectionSocket, 
                            cancellationToken), cancellationToken.Token);
            }
        }
        catch
        {
            _socket.Close();
        }
    }

    private async Task ProcessSocketConnect(Socket socket, CancellationTokenSource ctxSource)
    {
        try
        {
            ctxSource.CancelAfter(5 * 60 * 1000);
            ctxSource.Token.Register(() => 
                throw new TimeoutException("Time is out"));
            var contentLength = 0;
            var buffer = new byte[256];
            contentLength = await socket.ReceiveAsync(buffer, SocketFlags.None, ctxSource.Token);
            
            if (IsResponseValid(buffer) && IsHello(buffer))
            {
                var clientName = Encoding.UTF8.GetString(GetContent(buffer, contentLength));
                _clients.TryAdd(socket, clientName);

                var broadcastMessage = $"К нам присоединился {clientName}";
                Console.WriteLine(broadcastMessage);
                //TODO: BroadCast

                await socket.SendAsync(Encoding.UTF8.GetBytes(broadcastMessage), SocketFlags.None, ctxSource.Token);
            }
            else
            {
                Console.WriteLine("К нам пытался ворваться клиент");
                await socket.DisconnectAsync(false);
            }

            while (socket.Connected)
            {
                var content = new List<byte>();
                var message = string.Empty;
                contentLength = await socket.ReceiveAsync(buffer, SocketFlags.None, ctxSource.Token);
                if (!IsResponseValid(buffer) || IsHello(buffer))
                {
                    continue;
                }
                if (IsBye(buffer))
                {
                    message = $"С нами прощается {_clients[socket]}.";
                    Console.WriteLine(message);
                    await BroadcastMessageAsync(socket, message, ctxSource.Token);
                    await socket.DisconnectAsync(false);
                }
                //Only Say Command
                else
                {
                    content.AddRange(GetContent(buffer, contentLength)); ;
                    if (IsPartial(buffer))
                    {
                        continue;
                    }

                    message = Encoding.UTF8.GetString(content.ToArray());
                    Console.WriteLine($"{_clients[socket]}: {message}");
                    await BroadcastMessageAsync(socket, message, ctxSource.Token);

                    content.Clear();
                }
            }
        }
        catch
        {
            await socket.DisconnectAsync(false);
        }
    }


    private async Task BroadcastMessageAsync(Socket socket, string message, 
        CancellationToken ctx)
    {
        var semaphore = new SemaphoreSlim(1);
        foreach (var client in _clients.Keys)
        {
            await semaphore.WaitAsync(ctx);
            await client.SendAsync(Encoding.UTF8.GetBytes(message), SocketFlags.None, ctx);
            semaphore.Release(1);
        }

        semaphore.Dispose();
    }

    private byte[] GetContent(byte[] buffer, int contentLength) =>
        buffer.Skip(10).Take(contentLength - 11).ToArray();

    private bool IsResponseValid(byte[] buffer) => 
        HasStart(buffer) && HasEnd(buffer) 
                         && IsCorrectProtocol(buffer)
                         && IsFullOrPartial(buffer);

    private bool HasStart(byte[] buffer) => 
        buffer[..3].Equals(Package11207Helper.BasePackage[..3]);
    
    private bool HasEnd(byte[] buffer) => 
        buffer[^1].Equals(Package11207Helper.LastByte);
    
    private bool IsCorrectProtocol(byte[] buffer) => 
        buffer[3..8].Equals(Package11207Helper.BasePackage[3..8]);
    private bool IsFullOrPartial(byte[] buffer) => 
        buffer[9] == (byte)FullnessPackage.Partial
        || buffer[9] == (byte)FullnessPackage.Full;

    private bool IsFull(byte[] buffer) =>
        buffer[9] == (byte)FullnessPackage.Full;

    private bool IsPartial(byte[] buffer) =>
        buffer[9] == (byte)FullnessPackage.Partial;

    private bool IsHello(byte[] buffer) =>
        buffer[8] == (byte)SupportCommand.Hello
        && buffer[9] == (byte)FullnessPackage.Full;
    private bool IsSay(byte[] buffer) => 
        buffer[8] == (byte)SupportCommand.Say;
    private bool IsBye(byte[] buffer) => 
        buffer[8] == (byte)SupportCommand.Bye;
}