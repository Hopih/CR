using System.Net;
using System.Net.Sockets;

namespace TcpClientExample;

public class Program
{
    static async Task Main()
    {
        
        //var tcpListener = new TcpListener(new IPEndPoint(new IPAddress(new byte[] { 127, 0, 0, 1 }), 5000));
        //tcpListener.Start();
        var tcpConnector = new TcpConnector();
        await tcpConnector.Connect();

        await tcpConnector.Disconnect();
        //tcpListener.Stop();
    }
}