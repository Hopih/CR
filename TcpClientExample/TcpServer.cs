using System.Net;
using System.Net.Sockets;

namespace TcpClientExample;

public class TcpServer
{
    private readonly IPEndPoint _serverSocketEP;
    public Socket TcpSocket { get;}

    public TcpServer(IPEndPoint endPoint, int port)
    {
        TcpSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _serverSocketEP = endPoint;
        
    }

    public async Task Start()
    {
        TcpSocket.Bind(_serverSocketEP);
        try
        {
            TcpSocket.Listen();
        }
        catch
        {
            Console.WriteLine("Произошла ошибка прослушивания ");
        }
    }
}