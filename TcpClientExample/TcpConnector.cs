using System.Net.Sockets;
using System.Text;

namespace TcpClientExample;

public sealed class TcpConnector : IDisposable
{
    private readonly Socket _tcpSocket;

    public TcpConnector()
    {
        _tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    }

    public async Task Connect(string host, int port)
    {
        await _tcpSocket.ConnectAsync(host, port);
    }

    public async ValueTask<int> SendMessage(ArraySegment<byte> message)
    {
        var bytesSent = await _tcpSocket.SendAsync(message, SocketFlags.None);
        if (bytesSent > 0)
        {
            return bytesSent;
        }

        throw new SocketException();
    }

    public bool IsConnected()
    {
        return _tcpSocket.Connected;
    }

    public async ValueTask<string> GetResult()
    {
        while (true)
        {
            try
            {
                var sizeBuffer = new byte[4];
                // сначала считываем размер данных
                await _tcpSocket.ReceiveAsync(sizeBuffer, SocketFlags.None);
                // узнаем размер и создаем соответствующий буфер
                var size = BitConverter.ToInt32(sizeBuffer, 0);
                // создаем соответствующий буфер
                var data = new byte[size];
                // считываем собственно данные
                var bytes = await _tcpSocket.ReceiveAsync(data, SocketFlags.None);
                return Encoding.UTF8.GetString(data, 0, bytes);
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }

    public async Task Disconnect()
    {
        await _tcpSocket.DisconnectAsync(true);
    }

    public void Dispose()
    {
        _tcpSocket.Dispose();
    }
}