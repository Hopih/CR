using System.Net;
using System.Net.Sockets;

namespace TcpClientExample;

public class TcpConnector
{
    private readonly IPEndPoint _iEndpoint;
    public Socket TcpSocket { get; init; }

    public TcpConnector()
    {
        TcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    }

    public async Task Connect()
    {
         
        await TcpSocket.ConnectAsync("www.google.com", 80);
        if (TcpSocket.Connected)
        {
            Console.WriteLine("Соединение установлено");
        }
    }

    public async Task Disconnect()
    {
        await TcpSocket.DisconnectAsync(false);
    }
}