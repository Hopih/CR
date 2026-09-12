using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace TcpServerExample;

internal sealed class TcpServer : IDisposable
{
    private readonly IPEndPoint _serverSocketEp;
    private readonly Socket _tcpSocket;
    private readonly Dictionary<string, Socket> _clients;

    public TcpServer(IPEndPoint endPoint)
    {
        _tcpSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _serverSocketEp = endPoint;
        _clients = new Dictionary<string, Socket>();
    }

    public void Start()
    {
        _tcpSocket.Bind(_serverSocketEp);
        try
        {
            _tcpSocket.Listen();
        }
        catch
        {
            Console.WriteLine("Произошла ошибка прослушивания ");
        }
    }

    public async Task ProcessListen()
    {
        try
        {
            while (true)
            {
                // получаем входящее подключение
                var tcpClient = await _tcpSocket.AcceptAsync();
                
                _ = Task.Run(async () =>await ProcessMessage(tcpClient));
            }
        }
        catch(Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    private async Task ProcessMessage(Socket tcpClient)
    {
        while (true)
        {
            // буфер для считывания размера данных
            byte[] sizeBuffer = new byte[4];
            // сначала считываем размер данных
            await tcpClient.ReceiveAsync(sizeBuffer, SocketFlags.None);
            // узнаем размер и создаем соответствующий буфер
            int size = BitConverter.ToInt32(sizeBuffer, 0);
            // создаем соответствующий буфер
            byte[] data = new byte[size];
            // считываем собственно данные
            int bytes = await tcpClient.ReceiveAsync(data, SocketFlags.None);
            var response = Encoding.UTF8.GetString(data, 0, bytes);

            try
            {
                var message = JsonSerializer.Deserialize<Message>(response) ?? new Message();
                var responseMessage = $"";

                if (message.IsClosedMessage)
                {
                    _clients.Remove(message.Autor!);
                    return;
                }

                if (message.Data == null)
                {
                    return;
                }

                responseMessage += $"{message.Autor}: {message.Data}";
                Console.WriteLine(responseMessage);

                var messageData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(responseMessage));
                var messageSize = BitConverter.GetBytes(messageData.Length);

                await tcpClient.SendAsync(new ArraySegment<byte>(messageSize), SocketFlags.None);
                await tcpClient.SendAsync(new ArraySegment<byte>(messageData), SocketFlags.None);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }

    public async ValueTask Stop()
    {
        await _tcpSocket.DisconnectAsync(true);
    }

    public void Dispose()
    {
        _tcpSocket.Dispose();
    }
}